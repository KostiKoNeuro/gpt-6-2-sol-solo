using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Pip
{
    internal sealed class Pet : ApplicationContext
    {
        private enum Mode { Rest, Patrol, Held, Airborne, Landing, Dozing, Sleep, Feeding, Reacting }
        private const double SleepAfter = 60;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Random random = new Random();
        private readonly Art art = new Art();
        private readonly ShellWindow host = new ShellWindow();
        private readonly LayeredWindow body = new LayeredWindow("Pip - Pet", false, true);
        private readonly LayeredWindow effects = new LayeredWindow("Pip - Effects", true, false);
        private readonly Timer timer = new Timer { Interval = 16 };
        private readonly ClipboardListener clipboard;
        private readonly TrayIcon tray;
        private readonly Queue<PointerSample> samples = new Queue<PointerSample>();
        private Perch perch;
        private Mode mode;
        private double started, deadline, lastTick, nextDisplay, nextTopmost, nextDraw, nextIdleCheck;
        private double activity, greetingUntil = 4.3, heartAt = -10, impactAt = -10;
        private float x, y, vx, vy, tilt, impactX, impactY, impactSize;
        private int facing = 1, bounces;
        private bool stayAsleep, pendingFeed, disposed;
        private CopyKind copy, pendingCopy;
        private Point grabStart;
        private float grabOffsetX, grabOffsetY, grabDistance;

        internal Pet()
        {
            perch = Perch.At(Native.CursorPosition());
            x = Clamp(perch.Work.Left + perch.Work.Width * .69f, perch.Left, perch.Right);
            y = perch.Floor;
            activity = Now;
            Enter(Mode.Rest, Now, .9);
            body.LeftDown += Grab;
            body.LeftUp += ReleasedInside;
            clipboard = new ClipboardListener(host.Handle);
            host.ClipboardChanged += clipboard.Changed;
            host.DisplayChanged += () => nextDisplay = 0;
            tray = new TrayIcon(host, art, () => stayAsleep || mode == Mode.Sleep || mode == Mode.Dozing);
            tray.Command += TrayCommand;
            lastTick = Now;
            Render(lastTick);
            timer.Tick += Tick;
            timer.Start();
        }

        private double Now { get { return clock.Elapsed.TotalSeconds; } }
        private float Scale { get { return perch.Scale; } }
        private bool InMotion { get { return mode == Mode.Held || mode == Mode.Airborne || mode == Mode.Landing; } }

        private void Enter(Mode next, double now, double duration)
        {
            mode = next;
            started = now;
            deadline = now + duration;
        }

        private void Tick(object sender, EventArgs args)
        {
            double now = Now;
            float dt = (float)Math.Min(.045, now - lastTick);
            lastTick = now;
            if (now >= nextDisplay)
            {
                nextDisplay = now + .9;
                Point point = mode == Mode.Held ? Native.CursorPosition() : Point.Round(new PointF(x, y - 22 * Scale));
                UpdatePerch(point);
            }

            CopyKind newCopy = clipboard.Check(now);
            if (newCopy != CopyKind.None)
            {
                activity = now;
                if (InMotion) pendingCopy = newCopy;
                else React(newCopy, now);
            }

            if (now >= nextIdleCheck)
            {
                nextIdleCheck = now + .15;
                CheckIdle(now);
            }

            switch (mode)
            {
                case Mode.Rest:
                    if (now >= deadline)
                    {
                        if (x >= perch.Right - 2) facing = -1;
                        if (x <= perch.Left + 2) facing = 1;
                        Enter(Mode.Patrol, now, 3.6 + random.NextDouble() * 2.8);
                    }
                    break;
                case Mode.Patrol:
                    x = Clamp(x + facing * 73 * Scale * dt, perch.Left, perch.Right);
                    if (now >= deadline || x <= perch.Left || x >= perch.Right)
                    {
                        if (x <= perch.Left || x >= perch.Right || random.Next(3) == 0) facing = -facing;
                        Enter(Mode.Rest, now, 1.3 + random.NextDouble() * 1.9);
                    }
                    break;
                case Mode.Held:
                    Point cursor = Native.CursorPosition();
                    Follow(cursor, now);
                    if (!Native.KeyDown(Native.VkLeftButton)) Release(now);
                    break;
                case Mode.Airborne:
                    Fall(dt, now);
                    break;
                case Mode.Landing:
                    if (now >= deadline) Settle(now, .65);
                    break;
                case Mode.Dozing:
                    if (now >= deadline) Enter(Mode.Sleep, now, Double.PositiveInfinity);
                    break;
                case Mode.Feeding:
                case Mode.Reacting:
                    if (now >= deadline) Settle(now, 1.25);
                    break;
            }

            tray.Tick(now);
            if (now >= nextDraw)
            {
                nextDraw = now + (mode == Mode.Sleep || (mode == Mode.Rest && now > greetingUntil) ? .026 : .013);
                Render(now);
            }
            if (now >= nextTopmost)
            {
                nextTopmost = now + 1.25;
                effects.Raise();
                body.Raise();
                tray.Raise();
            }
        }

        private void CheckIdle(double now)
        {
            if (stayAsleep) return;
            bool idle = now - activity >= SleepAfter && Native.IdleMilliseconds() >= SleepAfter * 1000;
            if (mode == Mode.Sleep || mode == Mode.Dozing)
            {
                if (!idle) Enter(Mode.Rest, now, 1.15);
            }
            else if (idle && (mode == Mode.Patrol || mode == Mode.Rest))
            {
                Doze(now);
            }
        }

        private void Settle(double now, double restTime)
        {
            y = perch.Floor;
            if (pendingFeed)
            {
                pendingFeed = false;
                Feed(now);
            }
            else if (pendingCopy != CopyKind.None)
            {
                CopyKind next = pendingCopy;
                pendingCopy = CopyKind.None;
                React(next, now);
            }
            else if (stayAsleep) Doze(now);
            else Enter(Mode.Rest, now, restTime);
        }

        private void Doze(double now)
        {
            greetingUntil = 0;
            Enter(Mode.Dozing, now, .29);
        }

        private void Feed(double now)
        {
            stayAsleep = false;
            greetingUntil = 0;
            Enter(Mode.Feeding, now, 3.25);
        }

        private void React(CopyKind kind, double now)
        {
            pendingCopy = CopyKind.None;
            copy = kind;
            greetingUntil = 0;
            Enter(Mode.Reacting, now, kind == CopyKind.LongText ? 4.5 : 3.85);
        }

        private void TrayCommand(TrayAction command)
        {
            double now = Now;
            activity = now;
            switch (command)
            {
                case TrayAction.Feed:
                    stayAsleep = false;
                    if (InMotion) pendingFeed = true;
                    else Feed(now);
                    break;
                case TrayAction.Sleep:
                    if (stayAsleep || mode == Mode.Sleep || mode == Mode.Dozing)
                    {
                        stayAsleep = false;
                        if (mode == Mode.Sleep || mode == Mode.Dozing) Enter(Mode.Rest, now, 1.15);
                    }
                    else
                    {
                        stayAsleep = true;
                        pendingFeed = false;
                        if (!InMotion) Doze(now);
                    }
                    break;
                case TrayAction.Exit:
                    timer.Stop();
                    ExitThread();
                    break;
            }
        }

        private void Grab(Point screen)
        {
            if (mode == Mode.Held) return;
            double now = Now;
            stayAsleep = false;
            pendingFeed = false;
            activity = now;
            greetingUntil = 0;
            grabStart = screen;
            grabOffsetX = screen.X - x;
            grabOffsetY = screen.Y - y;
            grabDistance = 0;
            tilt = vx = vy = 0;
            samples.Clear();
            samples.Enqueue(new PointerSample(screen, now));
            Enter(Mode.Held, now, Double.PositiveInfinity);
        }

        private void ReleasedInside(Point point)
        {
            if (mode != Mode.Held) return;
            double now = Now;
            Follow(point, now);
            Release(now);
        }

        private void Follow(Point point, double now)
        {
            Point next = new Point(point.X - (int)grabOffsetX, point.Y - (int)grabOffsetY);
            if (Native.MonitorAt(point) != perch.Monitor) UpdatePerch(point);
            float previous = x;
            x = Clamp(next.X, perch.Left, perch.Right);
            y = Clamp(next.Y, perch.Ceiling, perch.Floor);
            float side = Clamp((x - previous) * .88f, -17, 17);
            tilt += (side - tilt) * .24f;
            grabDistance = Math.Max(grabDistance, Math.Abs(point.X - grabStart.X) + Math.Abs(point.Y - grabStart.Y));
            samples.Enqueue(new PointerSample(point, now));
            while (samples.Count > 2 && now - samples.Peek().Time > .115) samples.Dequeue();
        }

        private void Release(double now)
        {
            activity = now;
            if (grabDistance < 8 * Scale)
            {
                heartAt = now;
                if (perch.Floor - y < 5 * Scale)
                {
                    Settle(now, 1.3);
                    return;
                }
            }
            PointerSample first = samples.Peek();
            Point current = Native.CursorPosition();
            float interval = (float)Math.Max(.02, now - first.Time);
            vx = Clamp((current.X - first.Point.X) / interval * .57f, -1040 * Scale, 1040 * Scale);
            vy = Clamp((current.Y - first.Point.Y) / interval * .56f, -990 * Scale, 870 * Scale);
            if (Math.Abs(vx) >= 140 * Scale) facing = vx > 0 ? 1 : -1;
            if (y >= perch.Floor - 5 * Scale && vy > -95 * Scale)
            {
                if (Math.Abs(vx) < 135 * Scale)
                {
                    Settle(now, 1.1);
                    return;
                }
                vy = -Math.Min(430 * Scale, 155 * Scale + Math.Abs(vx) * .27f);
            }
            y = Math.Min(y, perch.Floor - Scale);
            bounces = 2;
            Enter(Mode.Airborne, now, Double.PositiveInfinity);
        }

        private void Fall(float dt, double now)
        {
            float gravity = 1480 * Scale;
            x += vx * dt;
            y += vy * dt + .5f * gravity * dt * dt;
            vy += gravity * dt;
            if (x < perch.Left || x > perch.Right)
            {
                x = Clamp(x, perch.Left, perch.Right);
                vx *= -.54f;
                if (Math.Abs(vx) >= 120 * Scale) facing = vx > 0 ? 1 : -1;
            }
            if (y < perch.Ceiling)
            {
                y = perch.Ceiling;
                vy = Math.Abs(vy) * .27f;
            }
            if (y < perch.Floor || vy <= 0) return;
            y = perch.Floor;
            impactSize = Clamp(vy / (720 * Scale), .18f, 1);
            impactX = x;
            impactY = y;
            impactAt = now;
            if (bounces > 0 && vy >= 205 * Scale)
            {
                bounces--;
                vy *= -.35f;
                vx *= .64f;
            }
            else Enter(Mode.Landing, now, .29);
        }

        private void UpdatePerch(Point point)
        {
            Perch next = Perch.At(point);
            float ratio = next.Scale / Scale;
            vx *= ratio;
            vy *= ratio;
            perch = next;
            x = Clamp(x, next.Left, next.Right);
            y = (mode == Mode.Held || mode == Mode.Airborne) ? Clamp(y, next.Ceiling, next.Floor) : next.Floor;
        }

        private void Render(double now)
        {
            Look look = Pose(now);
            float scale = Scale;
            const int stageW = 180, stageH = 185, below = 16;
            var petRect = new Rectangle((int)Math.Round(x - stageW * scale / 2), (int)Math.Round(y - (stageH - below) * scale),
                (int)Math.Ceiling(stageW * scale), (int)Math.Ceiling(stageH * scale));
            var fxRect = new Rectangle((int)Math.Round(x - 167 * scale), (int)Math.Round(y - 226 * scale),
                (int)Math.Ceiling(334 * scale), (int)Math.Ceiling(246 * scale));
            effects.Render(fxRect, graphics =>
            {
                graphics.ScaleTransform(scale, scale);
                DrawEffects(graphics, fxRect, look, now);
            });
            body.Render(petRect, graphics =>
            {
                graphics.ScaleTransform(scale, scale);
                art.DrawSprite(graphics, look.Sprite, (x - petRect.X) / scale, (y - petRect.Y) / scale - look.Lift,
                    facing, look.Angle, look.Wide, look.Tall);
            });
        }

        private Look Pose(double now)
        {
            double age = now - started;
            var look = new Look { Sprite = Sprite.Sit, Wide = 1, Tall = 1 };
            switch (mode)
            {
                case Mode.Patrol:
                    double gait = age * 8.8;
                    int contact = (int)gait % 4;
                    look.Sprite = contact == 0 ? Sprite.StepA : contact == 2 ? Sprite.StepB : Sprite.Stand;
                    look.Lift = 1.8f * (float)(.5 + .5 * Math.Cos(gait * Math.PI));
                    break;
                case Mode.Held:
                    look.Sprite = Sprite.Held;
                    look.Angle = tilt + (float)Math.Sin(age * 8) * 1.7f;
                    break;
                case Mode.Airborne:
                    look.Sprite = Sprite.Fly;
                    look.Angle = facing * Clamp(vy / (Scale * 33), -23, 25);
                    break;
                case Mode.Landing:
                    look.Sprite = Sprite.Land;
                    look.Wide += .08f * (1 - (float)(age / .29));
                    break;
                case Mode.Dozing:
                    look.Sprite = Sprite.Land;
                    look.Tall -= .08f * (float)Math.Min(1, age / .29);
                    break;
                case Mode.Sleep:
                    look.Sprite = Sprite.Sleep;
                    look.Tall += .036f * (float)Math.Sin(age * 2.5);
                    look.Wide -= .013f * (float)Math.Sin(age * 2.5);
                    break;
                case Mode.Feeding:
                    look.Sprite = Sprite.Eat;
                    look.Tall += .022f * (float)Math.Sin(age * 15);
                    look.Angle = facing * 1.8f * (float)Math.Sin(age * 12);
                    break;
                case Mode.Reacting:
                    look.Sprite = ReactionSprite(copy);
                    float bump = (float)Math.Max(0, 1 - age * 1.5);
                    look.Lift = 9 * bump * (float)Math.Abs(Math.Sin(age * 13));
                    if (copy == CopyKind.Number) look.Angle = facing * (float)Math.Sin(age * 8) * 3.3f * bump;
                    if (copy == CopyKind.Code) look.Angle = facing * (float)Math.Sin(age * 3.6) * 1.5f;
                    if (copy == CopyKind.LongText) look.Tall -= .045f * (float)Math.Min(1, age / 2);
                    break;
                default:
                    look.Tall += .009f * (float)Math.Sin(age * 3.5);
                    break;
            }
            return look;
        }

        private void DrawEffects(Graphics graphics, Rectangle bounds, Look look, double now)
        {
            float scale = Scale;
            float cx = (x - bounds.X) / scale, foot = (y - bounds.Y) / scale;
            double age = now - started;
            float height = art.SpriteHeight(look.Sprite) * look.Tall;
            float clearance = (perch.Floor - y) / scale;
            if (clearance < 39)
                Art.DrawShadow(graphics, cx + facing * 7, foot + clearance, mode == Mode.Sleep ? 85 : 76, 1 - clearance / 39);

            if (mode == Mode.Sleep)
            {
                for (int index = 0; index < 3; index++)
                {
                    float rise = (float)((age + 2.15 - index * .54) % 2.15 / 2.15);
                    Art.DrawZ(graphics, cx + facing * (28 + 22 * rise), foot - height * .54f - rise * 52,
                        6.8f + 4.3f * rise, (float)Math.Sin(rise * Math.PI));
                }
            }
            if (mode == Mode.Airborne && Math.Abs(vx) > 180 * scale)
            {
                float nose = cx - facing * 64;
                using (var stream = new Pen(Color.FromArgb(130, 219, 243, 236), 1.6f))
                {
                    stream.StartCap = stream.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    graphics.DrawLine(stream, nose, foot - 43, nose - facing * 12, foot - 39);
                    graphics.DrawLine(stream, nose + facing * 3, foot - 32, nose - facing * 5, foot - 28);
                }
            }

            float landing = (float)((now - impactAt) / .45);
            if (landing >= 0 && landing < 1)
            {
                for (int index = -3; index <= 3; index++)
                {
                    if (index == 0) continue;
                    float sparkX = (impactX - bounds.X) / scale + index * (7 + landing * 9);
                    float sparkY = (impactY - bounds.Y) / scale - 4 - (float)Math.Sin(landing * Math.PI) * Math.Abs(index) * 4;
                    Art.DrawSpark(graphics, sparkX, sparkY, (2.5f + impactSize * 2) * (1 - landing),
                        Color.FromArgb(255, 218, 159), .86f * (1 - landing));
                }
            }
            float heart = (float)((now - heartAt) / .95);
            if (heart >= 0 && heart < 1)
                Art.DrawHeart(graphics, cx + facing * 26, foot - height + 6 - heart * 35, 6, (1 - heart) * .98f);

            if (mode == Mode.Feeding && age < 1.65)
            {
                float crumb = (float)((age * 2.8) % 1);
                for (int index = 0; index < 3; index++)
                    Art.DrawSpark(graphics, cx + facing * (28 + (6 + index * 10) * crumb), foot - 64 - index * 6 + crumb * crumb * 22,
                        1.7f + index * .52f, Color.FromArgb(255, 207, 116), 1 - crumb);
            }

            Speech speech;
            double length, shown;
            if (mode == Mode.Reacting)
            {
                speech = ReactionSpeech(copy); length = deadline - started; shown = age;
            }
            else if (mode == Mode.Feeding)
            {
                speech = Speech.Feed; length = deadline - started; shown = age;
            }
            else if (now < greetingUntil)
            {
                speech = Speech.Hello; length = greetingUntil; shown = now;
            }
            else return;

            float visibility = Clamp((float)Math.Min(shown / .16, (length - shown) / .29), 0, 1);
            float leftEdge = (perch.Screen.Left - bounds.X) / scale + 9;
            float rightEdge = (perch.Screen.Right - bounds.X) / scale - Art.BubbleWidth - 9;
            float left = Clamp(cx - Art.BubbleWidth / 2f + facing * 22, leftEdge, rightEdge);
            float top = foot - height - look.Lift - Art.BubbleHeight - 9 + (1 - visibility) * 6;
            art.DrawBubble(graphics, speech, left, top, cx + facing * 19, visibility);
        }

        private static Sprite ReactionSprite(CopyKind kind)
        {
            switch (kind)
            {
                case CopyKind.Link: return Sprite.Link;
                case CopyKind.Number: return Sprite.Number;
                case CopyKind.Code: return Sprite.Code;
                default: return Sprite.LongText;
            }
        }

        private static Speech ReactionSpeech(CopyKind kind)
        {
            switch (kind)
            {
                case CopyKind.Link: return Speech.Link;
                case CopyKind.Number: return Speech.Number;
                case CopyKind.Code: return Speech.Code;
                default: return Speech.LongText;
            }
        }

        private static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(max, value)); }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                // WinForms also disposes the context when the message loop exits.
                disposed = true;
                timer.Dispose();
                tray.Dispose();
                clipboard.Dispose();
                host.Dispose();
                effects.Dispose();
                body.Dispose();
                art.Dispose();
            }
            base.Dispose(disposing);
        }

        private struct PointerSample
        {
            internal readonly Point Point;
            internal readonly double Time;
            internal PointerSample(Point point, double time) { Point = point; Time = time; }
        }

        private struct Look
        {
            internal Sprite Sprite;
            internal float Wide, Tall, Angle, Lift;
        }
    }
}
