using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace Pip
{
    internal enum Sprite { Stand, StepA, StepB, Sit, Held, Fly, Sleep, Eat, Land, Link, Number, Code, LongText }
    internal enum Speech { Hello, Link, Number, Code, LongText, Feed }
    internal enum Symbol { Paw, Link, Number, Code, Paper, Biscuit, Moon, Sun, Power }

    internal sealed class Art : IDisposable
    {
        internal const int MenuWidth = 231, MenuHeight = 202;
        internal const int BubbleWidth = 216, BubbleHeight = 57;
        private static readonly float[] Widths = { 114, 122, 125, 104, 89, 132, 108, 105, 127, 113, 105, 121, 122 };
        private static readonly Color Paper = Color.FromArgb(252, 243, 230);
        private static readonly Color Muted = Color.FromArgb(190, 200, 199);
        private static readonly Color Gold = Color.FromArgb(255, 202, 132);
        private static readonly Color Teal = Color.FromArgb(135, 226, 202);
        private static readonly Color Fern = Color.FromArgb(181, 228, 158);
        private static readonly Color Lilac = Color.FromArgb(212, 196, 245);
        private static readonly Color Rose = Color.FromArgb(251, 160, 149);
        private static readonly Color Panel = Color.FromArgb(249, 27, 35, 40);
        private static readonly Color Edge = Color.FromArgb(123, 108, 131, 134);
        private readonly Bitmap[] sprites = new Bitmap[Widths.Length];
        private readonly Font title = new Font("Segoe UI", 21f, FontStyle.Bold, GraphicsUnit.Pixel);
        private readonly Font heading = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Pixel);
        private readonly Font label = new Font("Segoe UI", 13.5f, FontStyle.Regular, GraphicsUnit.Pixel);
        private readonly Font detail = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel);

        internal Art()
        {
            foreach (Sprite sprite in Enum.GetValues(typeof(Sprite)))
            {
                using (Stream stream = Resource(sprite.ToString().ToLowerInvariant() + ".png"))
                using (var source = new Bitmap(stream))
                {
                    var frame = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppPArgb);
                    using (Graphics graphics = Graphics.FromImage(frame))
                    {
                        graphics.CompositingMode = CompositingMode.SourceCopy;
                        graphics.DrawImageUnscaled(source, 0, 0);
                    }
                    sprites[(int)sprite] = frame;
                }
            }
        }

        private static Stream Resource(string name)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Pip.Art." + name);
            if (stream == null) throw new FileNotFoundException("Missing embedded artwork", name);
            return stream;
        }

        internal Icon LoadIcon()
        {
            using (Stream stream = Resource("pip.ico"))
            using (var icon = new Icon(stream, 32, 32))
                return (Icon)icon.Clone();
        }

        internal float SpriteHeight(Sprite sprite)
        {
            Bitmap image = sprites[(int)sprite];
            return Widths[(int)sprite] * image.Height / image.Width;
        }

        internal void DrawSprite(Graphics graphics, Sprite sprite, float x, float foot, int direction,
            float angle, float widthFactor, float heightFactor)
        {
            Bitmap image = sprites[(int)sprite];
            float width = Widths[(int)sprite] * widthFactor;
            float height = SpriteHeight(sprite) * heightFactor;
            GraphicsState state = graphics.Save();
            graphics.TranslateTransform(x, foot - height * .48f);
            graphics.RotateTransform(angle);
            graphics.ScaleTransform(direction, 1);
            graphics.DrawImage(image, new RectangleF(-width / 2, -height * .52f, width, height),
                new RectangleF(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
            graphics.Restore(state);
        }

        internal void DrawBubble(Graphics graphics, Speech speech, float x, float y, float tailX, float visibility)
        {
            if (visibility <= 0) return;
            string headline, subline;
            Color color;
            Symbol symbol;
            switch (speech)
            {
                case Speech.Link:
                    headline = "A shortcut!"; subline = "Does it lead to snacks?"; color = Teal; symbol = Symbol.Link;
                    break;
                case Speech.Number:
                    headline = "I counted it."; subline = "Still zero snacks."; color = Gold; symbol = Symbol.Number;
                    break;
                case Speech.Code:
                    headline = "I found the bug."; subline = "It ran under the taskbar."; color = Fern; symbol = Symbol.Code;
                    break;
                case Speech.LongText:
                    headline = "So many words."; subline = "I'll use them as a blanket."; color = Lilac; symbol = Symbol.Paper;
                    break;
                case Speech.Feed:
                    headline = "Crunch."; subline = "Excellent biscuit."; color = Gold; symbol = Symbol.Biscuit;
                    break;
                default:
                    headline = "Hi, I'm Pip."; subline = "Grab and flick to toss."; color = Gold; symbol = Symbol.Paw;
                    break;
            }

            RectangleF card = new RectangleF(x, y, BubbleWidth, BubbleHeight);
            float tail = Math.Max(x + 18, Math.Min(x + BubbleWidth - 18, tailX));
            using (var shadow = new SolidBrush(Faded(Color.FromArgb(45, 20, 25, 30), visibility)))
            using (var surface = new SolidBrush(Faded(Panel, visibility)))
            using (var edge = new Pen(Faded(Edge, visibility), 1f))
            using (var tile = new SolidBrush(Faded(Color.FromArgb(33, color), visibility)))
            using (GraphicsPath path = Round(card, 12))
            using (GraphicsPath softShadow = Round(new RectangleF(x + .5f, y + 3.5f, BubbleWidth, BubbleHeight + .5f), 13))
            using (GraphicsPath badge = Round(new RectangleF(x + 11, y + 11, 35, 35), 10))
            {
                graphics.FillPath(shadow, softShadow);
                graphics.FillPolygon(surface, new[] { new PointF(tail - 7, card.Bottom - 1),
                    new PointF(tail + 7, card.Bottom - 1), new PointF(tail + 1, card.Bottom + 7) });
                graphics.FillPath(surface, path);
                graphics.DrawPath(edge, path);
                graphics.FillPath(tile, badge);
            }
            DrawSymbol(graphics, symbol, x + 18, y + 18, 21, Faded(color, visibility));
            Write(graphics, headline, heading, Faded(Paper, visibility), x + 57, y + 10);
            Write(graphics, subline, detail, Faded(Muted, visibility), x + 57, y + 30);
        }

        internal static int MenuRow(float x, float y)
        {
            if (x < 8 || x > MenuWidth - 8) return -1;
            for (int index = 0; index < 3; index++)
            {
                float top = 72 + index * 40;
                if (y >= top && y < top + 38) return index;
            }
            return -1;
        }

        internal void DrawMenu(Graphics graphics, int highlighted, bool asleep)
        {
            var frame = new RectangleF(1, 1, MenuWidth - 2, MenuHeight - 2);
            using (GraphicsPath background = Round(frame, 17))
            using (var fill = new LinearGradientBrush(frame, Color.FromArgb(33, 44, 48), Color.FromArgb(24, 31, 36), 90f))
            using (var edge = new Pen(Color.FromArgb(79, 98, 103)))
            using (var divider = new Pen(Color.FromArgb(60, 76, 81)))
            using (var portraitBack = new SolidBrush(Color.FromArgb(61, 55, 50)))
            using (GraphicsPath portrait = Round(new RectangleF(13, 12, 45, 45), 13))
            {
                graphics.FillPath(fill, background);
                graphics.DrawPath(edge, background);
                graphics.FillPath(portraitBack, portrait);
                DrawSprite(graphics, Sprite.Sit, 35.5f, 53, 1, 0, .36f, .36f);
                Write(graphics, "Pip", title, Paper, 70, 11);
                Write(graphics, "Taskbar fox", detail, Muted, 71, 37);
                graphics.DrawLine(divider, 12, 64, MenuWidth - 12, 64);
            }

            DrawCommand(graphics, 0, highlighted, "Feed", Symbol.Biscuit, Gold);
            DrawCommand(graphics, 1, highlighted, asleep ? "Wake Pip" : "Put to sleep", asleep ? Symbol.Sun : Symbol.Moon, Teal);
            DrawCommand(graphics, 2, highlighted, "Exit", Symbol.Power, Rose);
        }

        private void DrawCommand(Graphics graphics, int index, int highlighted, string text, Symbol symbol, Color color)
        {
            float top = 72 + index * 40;
            if (highlighted == index)
            {
                using (var brush = new SolidBrush(Color.FromArgb(46, color)))
                using (GraphicsPath path = Round(new RectangleF(8, top, MenuWidth - 16, 38), 10))
                    graphics.FillPath(brush, path);
            }
            DrawSymbol(graphics, symbol, 22, top + 9, 20, color);
            Write(graphics, text, label, Paper, 53, top + 9);
        }

        internal static void DrawShadow(Graphics graphics, float x, float floor, float width, float visibility)
        {
            using (var outer = new SolidBrush(Faded(Color.FromArgb(46, 20, 24, 32), visibility)))
            using (var inner = new SolidBrush(Faded(Color.FromArgb(63, 20, 24, 32), visibility)))
            {
                graphics.FillEllipse(outer, x - width / 2, floor - 3.8f, width, 5.8f);
                graphics.FillEllipse(inner, x - width * .34f, floor - 2.8f, width * .68f, 3.7f);
            }
        }

        internal static void DrawZ(Graphics graphics, float x, float y, float size, float opacity)
        {
            var points = new[] { new PointF(x, y), new PointF(x + size, y),
                new PointF(x, y + size), new PointF(x + size, y + size) };
            using (var rim = RoundedPen(Faded(Color.FromArgb(200, 33, 35, 47), opacity), 4))
            using (var pen = RoundedPen(Faded(Lilac, opacity), 2.1f))
            {
                graphics.DrawLines(rim, points);
                graphics.DrawLines(pen, points);
            }
        }

        internal static void DrawSpark(Graphics graphics, float x, float y, float size, Color color, float opacity)
        {
            using (var brush = new SolidBrush(Faded(color, opacity)))
                graphics.FillPolygon(brush, new[] { new PointF(x, y - size), new PointF(x + size * .29f, y - size * .29f),
                    new PointF(x + size, y), new PointF(x + size * .29f, y + size * .29f), new PointF(x, y + size),
                    new PointF(x - size * .29f, y + size * .29f), new PointF(x - size, y), new PointF(x - size * .29f, y - size * .29f) });
        }

        internal static void DrawHeart(Graphics graphics, float x, float y, float size, float opacity)
        {
            using (var path = new GraphicsPath())
            using (var brush = new SolidBrush(Faded(Rose, opacity)))
            using (var outline = new Pen(Faded(Color.FromArgb(218, 73, 42, 52), opacity), 1.6f))
            {
                path.StartFigure();
                path.AddBezier(x, y + size, x - size * 1.9f, y - size * .16f, x - size * .75f, y - size * 1.5f, x, y - size * .48f);
                path.AddBezier(x, y - size * .48f, x + size * .75f, y - size * 1.5f, x + size * 1.9f, y - size * .16f, x, y + size);
                path.CloseFigure();
                graphics.FillPath(brush, path);
                graphics.DrawPath(outline, path);
            }
        }

        private static void DrawSymbol(Graphics graphics, Symbol symbol, float x, float y, float size, Color color)
        {
            GraphicsState state = graphics.Save();
            graphics.TranslateTransform(x, y);
            graphics.ScaleTransform(size / 20, size / 20);
            using (var pen = RoundedPen(color, 1.75f))
            using (var brush = new SolidBrush(color))
            {
                switch (symbol)
                {
                    case Symbol.Paw:
                        graphics.FillEllipse(brush, 5.1f, 9.5f, 9.8f, 7.2f);
                        graphics.FillEllipse(brush, 2, 6, 3.8f, 4.8f);
                        graphics.FillEllipse(brush, 6.4f, 2.5f, 3.3f, 4.8f);
                        graphics.FillEllipse(brush, 10.5f, 2.5f, 3.3f, 4.8f);
                        graphics.FillEllipse(brush, 14.7f, 6, 3.8f, 4.8f);
                        break;
                    case Symbol.Link:
                        graphics.DrawArc(pen, new RectangleF(2, 8, 9, 9), 62, 266);
                        graphics.DrawArc(pen, new RectangleF(9, 3, 9, 9), 244, 265);
                        graphics.DrawLine(pen, 7, 12, 13, 8);
                        break;
                    case Symbol.Number:
                        graphics.DrawLine(pen, 8, 3, 5.5f, 17);
                        graphics.DrawLine(pen, 14.5f, 3, 12, 17);
                        graphics.DrawLine(pen, 3, 7.3f, 17.4f, 7.3f);
                        graphics.DrawLine(pen, 2.6f, 12.5f, 17, 12.5f);
                        break;
                    case Symbol.Code:
                        graphics.DrawLines(pen, new[] { new PointF(5.8f, 6.5f), new PointF(2, 10), new PointF(5.8f, 13.5f) });
                        graphics.DrawLine(pen, 11.7f, 4.5f, 8.3f, 15.5f);
                        graphics.DrawLines(pen, new[] { new PointF(14.2f, 6.5f), new PointF(18, 10), new PointF(14.2f, 13.5f) });
                        break;
                    case Symbol.Paper:
                        using (GraphicsPath page = Round(new RectangleF(4, 2.5f, 12, 15), 2))
                            graphics.DrawPath(pen, page);
                        graphics.DrawLine(pen, 7, 7.4f, 13, 7.4f);
                        graphics.DrawLine(pen, 7, 11.3f, 12, 11.3f);
                        break;
                    case Symbol.Biscuit:
                        graphics.DrawArc(pen, 2.4f, 2.4f, 15.2f, 15.2f, -7, 283);
                        graphics.DrawArc(pen, 11.2f, 1.1f, 7.5f, 8.2f, 177, -97);
                        graphics.FillEllipse(brush, 6, 6.7f, 2.2f, 2.2f);
                        graphics.FillEllipse(brush, 6.8f, 12.3f, 2, 2);
                        graphics.FillEllipse(brush, 11.9f, 10.2f, 2.4f, 2.4f);
                        break;
                    case Symbol.Moon:
                        using (var moon = new GraphicsPath())
                        {
                            moon.AddBezier(10.5f, 2.4f, 2.2f, 2.2f, .2f, 11.9f, 5.9f, 16.1f);
                            moon.AddBezier(5.9f, 16.1f, 10.6f, 19.4f, 16.7f, 16.8f, 17.6f, 12.1f);
                            moon.AddBezier(17.6f, 12.1f, 10.4f, 14.2f, 7.8f, 8.5f, 10.5f, 2.4f);
                            graphics.DrawPath(pen, moon);
                        }
                        break;
                    case Symbol.Sun:
                        graphics.DrawEllipse(pen, 6, 6, 8, 8);
                        for (int index = 0; index < 8; index++)
                        {
                            double angle = index * Math.PI / 4;
                            float dx = (float)Math.Cos(angle), dy = (float)Math.Sin(angle);
                            graphics.DrawLine(pen, 10 + dx * 6.6f, 10 + dy * 6.6f, 10 + dx * 8.6f, 10 + dy * 8.6f);
                        }
                        break;
                    case Symbol.Power:
                        graphics.DrawArc(pen, 3, 3.8f, 14, 13.8f, -47, 276);
                        graphics.DrawLine(pen, 10, 2.2f, 10, 10);
                        break;
                }
            }
            graphics.Restore(state);
        }

        private static void Write(Graphics graphics, string text, Font font, Color color, float x, float y)
        {
            using (var brush = new SolidBrush(color))
                graphics.DrawString(text, font, brush, new PointF(x, y), StringFormat.GenericTypographic);
        }

        private static GraphicsPath Round(RectangleF rect, float radius)
        {
            float diameter = 2 * radius;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Pen RoundedPen(Color color, float width)
        {
            return new Pen(color, width) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        }

        private static Color Faded(Color color, float alpha)
        {
            return Color.FromArgb((int)(color.A * Math.Max(0, Math.Min(1, alpha))), color.R, color.G, color.B);
        }

        public void Dispose()
        {
            foreach (Bitmap image in sprites) image.Dispose();
            title.Dispose();
            heading.Dispose();
            label.Dispose();
            detail.Dispose();
        }
    }
}
