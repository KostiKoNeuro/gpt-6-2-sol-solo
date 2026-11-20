using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Pip
{
    internal enum TrayAction { Feed, Sleep, Exit }

    internal sealed class ShellWindow : NativeWindow, IDisposable
    {
        internal const int TrayCallback = 0x8001;
        private readonly int taskbarCreated;
        internal event Action ClipboardChanged;
        internal event Action DisplayChanged;
        internal event Action TaskbarCreated;
        internal event Action<int, Point> TrayMessage;

        internal ShellWindow()
        {
            taskbarCreated = Native.RegisterWindowMessage("TaskbarCreated");
            CreateHandle(new CreateParams
            {
                Caption = "Pip - Shell",
                Style = Native.Popup,
                ExStyle = Native.NoActivate | Native.ToolWindow
            });
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == TrayCallback)
            {
                if (TrayMessage != null) TrayMessage(unchecked((ushort)message.LParam.ToInt64()), Native.MessagePoint(message.WParam));
                return;
            }
            if (message.Msg == Native.WmClipboardUpdate && ClipboardChanged != null) ClipboardChanged();
            if (message.Msg == taskbarCreated && TaskbarCreated != null) TaskbarCreated();
            if ((message.Msg == Native.WmDisplayChange || message.Msg == Native.WmSettingChange) &&
                DisplayChanged != null) DisplayChanged();
            base.WndProc(ref message);
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero) DestroyHandle();
        }
    }

    internal sealed class TrayIcon : IDisposable
    {
        private const uint Id = 1;
        private const int Select = 0x0400;
        private const int KeySelect = 0x0401;
        private readonly ShellWindow host;
        private readonly Icon icon;
        private readonly LayeredWindow menu;
        private readonly Art art;
        private readonly Func<bool> asleep;
        private Rectangle bounds;
        private float scale;
        private int highlighted = -1;
        private int pressed = -1;
        private bool added, versionFour, leftWasDown, rightWasDown, lastAsleep;
        private double nextRegister;

        internal event Action<TrayAction> Command;

        internal TrayIcon(ShellWindow host, Art art, Func<bool> asleep)
        {
            this.host = host;
            this.art = art;
            this.asleep = asleep;
            icon = art.LoadIcon();
            menu = new LayeredWindow("Pip - Tray menu", false, false);
            menu.LeftDown += point => pressed = RowAt(point);
            menu.LeftUp += AcceptClick;
            host.TrayMessage += Receive;
            host.TaskbarCreated += Register;
            Register();
        }

        private IconData Data()
        {
            return new IconData
            {
                Size = (uint)Marshal.SizeOf(typeof(IconData)),
                Window = host.Handle, Id = Id,
                Message = ShellWindow.TrayCallback,
                Icon = icon.Handle, Tip = "Pip - Taskbar fox",
                Info = "", InfoTitle = ""
            };
        }

        private void Register()
        {
            IconData data = Data();
            // Message, icon, tooltip and standard tooltip even with version 4.
            data.Flags = 0x87;
            added = Shell_NotifyIcon(0, ref data);
            if (!added) return;
            data.Version = 4;
            versionFour = Shell_NotifyIcon(4, ref data);
        }

        private void Receive(int message, Point anchor)
        {
            if (message != Native.WmContextMenu && message != Select && message != KeySelect &&
                (versionFour || (message != 0x0205 && message != Native.WmLeftUp))) return;
            if (menu.Visible)
            {
                Hide();
                return;
            }
            if (message == KeySelect || anchor == new Point(-1, -1))
            {
                var id = new IconId { Size = (uint)Marshal.SizeOf(typeof(IconId)), Window = host.Handle, Id = Id };
                Native.PixelRect rect;
                anchor = Shell_NotifyIconGetRect(ref id, out rect) == 0
                    ? new Point(rect.Left + (rect.Right - rect.Left) / 2, rect.Top)
                    : Native.CursorPosition();
            }
            else if (!versionFour || anchor == Point.Empty) anchor = Native.CursorPosition();
            Show(anchor);
        }

        private void Show(Point anchor)
        {
            Perch perch = Perch.At(anchor);
            Rectangle work = perch.Work;
            scale = perch.Scale;
            int width = (int)Math.Ceiling(Art.MenuWidth * scale);
            int height = (int)Math.Ceiling(Art.MenuHeight * scale);
            int gap = (int)Math.Round(8 * scale);
            int x = Math.Max(work.Left + gap, Math.Min(work.Right - width - gap, anchor.X - width / 2));
            int y = anchor.Y - height - gap;
            if (anchor.Y >= work.Bottom - gap) y = work.Bottom - height - gap;
            if (y < work.Top + gap) y = anchor.Y + gap;
            y = Math.Max(work.Top + gap, Math.Min(work.Bottom - height - gap, y));
            bounds = new Rectangle(x, y, width, height);
            leftWasDown = Native.KeyDown(Native.VkLeftButton);
            rightWasDown = Native.KeyDown(Native.VkRightButton);
            pressed = -1;
            highlighted = RowAt(Native.CursorPosition());
            Redraw();
            menu.Raise();
        }

        private int RowAt(Point point)
        {
            if (!bounds.Contains(point)) return -1;
            return Art.MenuRow((point.X - bounds.X) / scale, (point.Y - bounds.Y) / scale);
        }

        private void AcceptClick(Point point)
        {
            int row = RowAt(point);
            if (pressed < 0 || row != pressed)
            {
                pressed = -1;
                return;
            }
            Hide();
            if (Command != null) Command((TrayAction)row);
        }

        private void Redraw()
        {
            lastAsleep = asleep();
            menu.Render(bounds, graphics =>
            {
                graphics.ScaleTransform(scale, scale);
                art.DrawMenu(graphics, highlighted, lastAsleep);
            });
        }

        internal void Tick(double now)
        {
            if (!added && now >= nextRegister)
            {
                nextRegister = now + 3;
                Register();
            }
            if (!menu.Visible) return;
            Point pointer = Native.CursorPosition();
            bool left = Native.KeyDown(Native.VkLeftButton);
            bool right = Native.KeyDown(Native.VkRightButton);
            if (Native.KeyDown(Native.VkEscape) ||
                (!bounds.Contains(pointer) && ((left && !leftWasDown) || (right && !rightWasDown))))
            {
                Hide();
                return;
            }
            leftWasDown = left;
            rightWasDown = right;
            int row = RowAt(pointer);
            if (row != highlighted || lastAsleep != asleep())
            {
                highlighted = row;
                Redraw();
            }
        }

        internal void Raise() { menu.Raise(); }

        private void Hide()
        {
            menu.Hide();
            pressed = -1;
        }

        public void Dispose()
        {
            host.TrayMessage -= Receive;
            host.TaskbarCreated -= Register;
            IconData data = Data();
            if (added) Shell_NotifyIcon(2, ref data);
            menu.Dispose();
            icon.Dispose();
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct IconData
        {
            internal uint Size;
            internal IntPtr Window;
            internal uint Id, Flags;
            internal int Message;
            internal IntPtr Icon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] internal string Tip;
            internal uint State, StateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] internal string Info;
            internal uint Version;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] internal string InfoTitle;
            internal uint InfoFlags;
            internal Guid Guid;
            internal IntPtr BalloonIcon;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IconId
        {
            internal uint Size;
            internal IntPtr Window;
            internal uint Id;
            internal Guid Guid;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(uint message, ref IconData icon);

        [DllImport("shell32.dll")]
        private static extern int Shell_NotifyIconGetRect(ref IconId id, out Native.PixelRect rect);
    }
}
