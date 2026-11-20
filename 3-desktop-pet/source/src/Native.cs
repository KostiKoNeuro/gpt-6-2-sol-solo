using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Pip
{
    internal static class Native
    {
        internal const int WmMouseActivate = 0x0021;
        internal const int WmSetCursor = 0x0020;
        internal const int WmLeftDown = 0x0201;
        internal const int WmLeftUp = 0x0202;
        internal const int WmContextMenu = 0x007B;
        internal const int WmClipboardUpdate = 0x031D;
        internal const int WmDisplayChange = 0x007E;
        internal const int WmSettingChange = 0x001A;
        internal const int NoActivate = 0x08000000;
        internal const int ToolWindow = 0x00000080;
        internal const int Popup = unchecked((int)0x80000000);
        internal const int VkLeftButton = 1;
        internal const int VkRightButton = 2;
        internal const int VkEscape = 0x1B;

        internal static bool KeyDown(int key)
        {
            return (GetAsyncKeyState(key) & 0x8000) != 0;
        }

        internal static Point CursorPosition()
        {
            PixelPoint point;
            GetCursorPos(out point);
            return new Point(point.X, point.Y);
        }

        internal static Point MessagePoint(IntPtr value)
        {
            long packed = value.ToInt64();
            return new Point(unchecked((short)packed), unchecked((short)(packed >> 16)));
        }

        internal static uint IdleMilliseconds()
        {
            var input = new LastInputInfo { Size = (uint)Marshal.SizeOf(typeof(LastInputInfo)) };
            if (!GetLastInputInfo(ref input)) return 0;
            // A queued input timestamp may briefly be ahead of GetTickCount.
            int age = unchecked((int)(GetTickCount() - input.Tick));
            return (uint)Math.Max(0, age);
        }

        internal static IntPtr MonitorAt(Point point)
        {
            return MonitorFromPoint(new PixelPoint(point.X, point.Y), 2);
        }

        internal static float MonitorScale(IntPtr monitor)
        {
            uint x, y;
            return GetDpiForMonitor(monitor, 0, out x, out y) == 0 ? x / 96f : 1f;
        }

        internal static void Raise(IntPtr window, bool show)
        {
            // TOPMOST, NOMOVE, NOSIZE, NOACTIVATE, NOOWNERZORDER.
            SetWindowPos(window, new IntPtr(-1), 0, 0, 0, 0, show ? 0x0253u : 0x0213u);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PixelPoint
        {
            internal int X, Y;
            internal PixelPoint(int x, int y) { X = x; Y = y; }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PixelSize
        {
            internal int Width, Height;
            internal PixelSize(int width, int height) { Width = width; Height = height; }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PixelRect
        {
            internal int Left, Top, Right, Bottom;
            internal Rectangle Rectangle
            {
                get { return Rectangle.FromLTRB(Left, Top, Right, Bottom); }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MonitorInfo
        {
            internal int Size;
            internal PixelRect Monitor, Work;
            internal uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LastInputInfo
        {
            internal uint Size, Tick;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BitmapInfo
        {
            internal int HeaderSize, Width, Height;
            internal short Planes, BitsPerPixel;
            internal uint Compression, ImageSize;
            internal int XResolution, YResolution;
            internal uint ColorsUsed, ImportantColors;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Blend
        {
            internal byte Operation, Flags, Opacity, AlphaFormat;
        }

        [DllImport("user32.dll")] internal static extern bool GetCursorPos(out PixelPoint point);
        [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LastInputInfo info);
        [DllImport("kernel32.dll")] private static extern uint GetTickCount();
        [DllImport("user32.dll")] internal static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(PixelPoint point, uint flags);
        [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint x, out uint y);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindow(string className, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string title);
        [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr window, out PixelRect rect);
        [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr window, int command);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr LoadCursor(IntPtr instance, IntPtr name);
        [DllImport("user32.dll")] internal static extern IntPtr SetCursor(IntPtr cursor);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UpdateLayeredWindow(IntPtr window, IntPtr screen, ref PixelPoint destination,
            ref PixelSize size, IntPtr source, ref PixelPoint origin, uint key, ref Blend blend, uint flags);
        [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr source);
        [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern IntPtr CreateDIBSection(IntPtr dc, ref BitmapInfo info, uint usage, out IntPtr pixels, IntPtr section, uint offset);
        [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr dc, IntPtr bitmap);
        [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr bitmap);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int RegisterWindowMessage(string name);
        [DllImport("user32.dll")] internal static extern bool AddClipboardFormatListener(IntPtr window);
        [DllImport("user32.dll")] internal static extern bool RemoveClipboardFormatListener(IntPtr window);
        [DllImport("user32.dll")] internal static extern uint GetClipboardSequenceNumber();
        [DllImport("user32.dll")] internal static extern bool IsClipboardFormatAvailable(uint format);
        [DllImport("user32.dll")] internal static extern bool OpenClipboard(IntPtr window);
        [DllImport("user32.dll")] internal static extern bool CloseClipboard();
        [DllImport("user32.dll")] internal static extern IntPtr GetClipboardData(uint format);
        [DllImport("kernel32.dll")] internal static extern IntPtr GlobalLock(IntPtr memory);
        [DllImport("kernel32.dll")] internal static extern bool GlobalUnlock(IntPtr memory);
        [DllImport("kernel32.dll")] internal static extern UIntPtr GlobalSize(IntPtr memory);
    }

    internal sealed class Perch
    {
        internal readonly IntPtr Monitor;
        internal readonly Rectangle Screen, Work;
        internal readonly float Scale, Floor, Left, Right, Ceiling;

        private Perch(IntPtr monitor, Rectangle screen, Rectangle work, float scale, float floor)
        {
            Monitor = monitor;
            Screen = screen;
            Work = work;
            Scale = scale;
            Floor = floor;
            Left = work.Left + 66 * scale;
            Right = Math.Max(Left, work.Right - 66 * scale);
            Ceiling = Math.Min(floor, screen.Top + 151 * scale);
        }

        internal static Perch At(Point point)
        {
            IntPtr monitor = Native.MonitorAt(point);
            var info = new Native.MonitorInfo { Size = Marshal.SizeOf(typeof(Native.MonitorInfo)) };
            if (!Native.GetMonitorInfo(monitor, ref info))
            {
                Rectangle fallback = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                return new Perch(monitor, fallback, fallback, 1, fallback.Bottom);
            }

            Rectangle screen = info.Monitor.Rectangle;
            Rectangle work = info.Work.Rectangle;
            float scale = Native.MonitorScale(monitor);
            float floor = Math.Min(work.Bottom, screen.Bottom - 2 * scale);
            floor = TaskbarFloor(Native.FindWindow("Shell_TrayWnd", null), screen, scale, floor);
            IntPtr taskbar = IntPtr.Zero;
            while ((taskbar = Native.FindWindowEx(IntPtr.Zero, taskbar, "Shell_SecondaryTrayWnd", null)) != IntPtr.Zero)
                floor = TaskbarFloor(taskbar, screen, scale, floor);
            return new Perch(monitor, screen, work, scale, floor);
        }

        private static float TaskbarFloor(IntPtr taskbar, Rectangle screen, float scale, float floor)
        {
            Native.PixelRect rect;
            if (taskbar == IntPtr.Zero || !Native.GetWindowRect(taskbar, out rect)) return floor;
            Rectangle visible = Rectangle.Intersect(rect.Rectangle, screen);
            if (visible.Width >= screen.Width / 2 && visible.Height >= 14 * scale &&
                visible.Top > screen.Top + screen.Height / 2 && visible.Bottom >= screen.Bottom - 2 * scale)
                return Math.Min(floor, visible.Top);
            return floor;
        }
    }

    internal sealed class LayeredWindow : NativeWindow, IDisposable
    {
        private readonly IntPtr cursor;
        private Dib surface;

        internal bool Visible { get; private set; }
        internal Rectangle Bounds { get; private set; }
        internal event Action<Point> LeftDown;
        internal event Action<Point> LeftUp;

        internal LayeredWindow(string caption, bool passThrough, bool handCursor)
        {
            // WS_EX_LAYERED handles input at the same alpha boundary that it draws.
            // The additional WS_EX_TRANSPARENT flag makes effects wholly pass through.
            CreateHandle(new CreateParams
            {
                Caption = caption,
                Style = Native.Popup,
                ExStyle = Native.NoActivate | Native.ToolWindow | 0x00080008 | (passThrough ? 0x20 : 0)
            });
            cursor = Native.LoadCursor(IntPtr.Zero, new IntPtr(handCursor ? 32649 : 32512));
        }

        internal void Render(Rectangle bounds, Action<Graphics> paint)
        {
            if (surface == null || surface.Size != bounds.Size)
            {
                if (surface != null) surface.Dispose();
                surface = new Dib(bounds.Size);
            }
            using (Graphics graphics = Graphics.FromImage(surface.Bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                paint(graphics);
            }
            var destination = new Native.PixelPoint(bounds.X, bounds.Y);
            var size = new Native.PixelSize(bounds.Width, bounds.Height);
            var origin = new Native.PixelPoint(0, 0);
            var blend = new Native.Blend { Opacity = 255, AlphaFormat = 1 };
            if (!Native.UpdateLayeredWindow(Handle, IntPtr.Zero, ref destination, ref size,
                surface.Dc, ref origin, 0, ref blend, 2)) return;
            Bounds = bounds;
            if (!Visible)
            {
                Native.Raise(Handle, true);
                Visible = true;
            }
        }

        internal void Raise()
        {
            if (Visible) Native.Raise(Handle, false);
        }

        internal void Hide()
        {
            if (!Visible) return;
            Native.ShowWindow(Handle, 0);
            Visible = false;
        }

        protected override void WndProc(ref Message message)
        {
            switch (message.Msg)
            {
                case Native.WmMouseActivate:
                    message.Result = new IntPtr(3); // MA_NOACTIVATE, still deliver the click.
                    return;
                case Native.WmSetCursor:
                    Native.SetCursor(cursor);
                    message.Result = new IntPtr(1);
                    return;
                case Native.WmLeftDown:
                case Native.WmLeftUp:
                    Action<Point> handler = message.Msg == Native.WmLeftDown ? LeftDown : LeftUp;
                    if (handler != null)
                    {
                        Point local = Native.MessagePoint(message.LParam);
                        handler(new Point(Bounds.X + local.X, Bounds.Y + local.Y));
                    }
                    return;
            }
            base.WndProc(ref message);
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero) DestroyHandle();
            if (surface != null) surface.Dispose();
        }

        private sealed class Dib : IDisposable
        {
            internal readonly IntPtr Dc;
            internal readonly Size Size;
            internal readonly Bitmap Bitmap;
            private readonly IntPtr dib, previous;

            internal Dib(Size size)
            {
                Size = size;
                Dc = Native.CreateCompatibleDC(IntPtr.Zero);
                var info = new Native.BitmapInfo
                {
                    HeaderSize = 40, Width = size.Width, Height = -size.Height, Planes = 1, BitsPerPixel = 32
                };
                IntPtr pixels;
                dib = Native.CreateDIBSection(Dc, ref info, 0, out pixels, IntPtr.Zero, 0);
                if (dib == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    Native.DeleteDC(Dc);
                    throw new Win32Exception(error);
                }
                previous = Native.SelectObject(Dc, dib);
                Bitmap = new Bitmap(size.Width, size.Height, size.Width * 4, PixelFormat.Format32bppPArgb, pixels);
            }

            public void Dispose()
            {
                Bitmap.Dispose();
                Native.SelectObject(Dc, previous);
                Native.DeleteObject(dib);
                Native.DeleteDC(Dc);
            }
        }
    }
}
