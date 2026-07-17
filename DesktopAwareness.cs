using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;

namespace Nimvio;

internal static class DesktopAwareness
{
    internal readonly record struct WindowSnapshot(IntPtr Handle, Rectangle Bounds, string ProcessName, string Title);
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll")] private static extern bool GetGUIThreadInfo(uint idThread, ref GuiThreadInfo info);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hwnd, ref Point point);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int virtualKey);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maximumCount);
    [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr hwnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public int Size, Flags;
        public IntPtr Active, Focus, Capture, MenuOwner, MoveSize, Caret;
        public NativeRect CaretRect;
    }

    public static List<Rectangle> VisibleWindows(Screen screen)
    {
        var result = new List<Rectangle>();
        var ownProcess = (uint)Environment.ProcessId;
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) || IsIconic(hwnd))
            {
                return true;
            }

            GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == ownProcess || !GetWindowRect(hwnd, out var r))
            {
                return true;
            }

            var rectangle = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
            if (rectangle.Width < 180 || rectangle.Height < 100)
            {
                return true;
            }

            if (screen.Bounds.IntersectsWith(rectangle))
            {
                result.Add(rectangle);
            }

            return true;
        }, IntPtr.Zero);
        return result;
    }

    public static Point? ActiveWindowCenter()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var r))
        {
            return null;
        }

        return new Point((r.Left + r.Right) / 2, (r.Top + r.Bottom) / 2);
    }

    public static Rectangle? ActiveWindowRectangle()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var r))
        {
            return null;
        }

        GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == (uint)Environment.ProcessId)
        {
            return null;
        }

        var rectangle = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
        return rectangle.Width >= 180 && rectangle.Height >= 100 ? rectangle : null;
    }

    public static WindowSnapshot? ActiveWindow()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero || !TryGetWindowRectangle(hwnd, out var rectangle))
        {
            return null;
        }

        GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == (uint)Environment.ProcessId || rectangle.Width < 180 || rectangle.Height < 100)
        {
            return null;
        }

        var titleLength = GetWindowTextLength(hwnd);
        var title = new StringBuilder(Math.Max(1, titleLength + 1));
        GetWindowText(hwnd, title, title.Capacity);
        try { return new WindowSnapshot(hwnd, rectangle, Process.GetProcessById((int)processId).ProcessName, title.ToString()); }
        catch { return new WindowSnapshot(hwnd, rectangle, string.Empty, title.ToString()); }
    }

    public static bool TryGetWindowRectangle(IntPtr hwnd, out Rectangle rectangle)
    {
        rectangle = Rectangle.Empty;
        if (hwnd == IntPtr.Zero || !IsWindowVisible(hwnd) || IsIconic(hwnd) || !GetWindowRect(hwnd, out var r))
        {
            return false;
        }

        rectangle = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
        return rectangle.Width > 0 && rectangle.Height > 0;
    }

    public static HashSet<IntPtr> VisibleWindowHandles()
    {
        var result = new HashSet<IntPtr>();
        var ownProcess = (uint)Environment.ProcessId;
        EnumWindows((hwnd, _) =>
        {
            if (!TryGetWindowRectangle(hwnd, out var rectangle) || rectangle.Width < 180 || rectangle.Height < 100)
            {
                return true;
            }

            GetWindowThreadProcessId(hwnd, out var processId);
            if (processId != ownProcess)
            {
                result.Add(hwnd);
            }

            return true;
        }, IntPtr.Zero);
        return result;
    }

    public static Point? ActiveCaretPosition()
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            return null;
        }

        var threadId = GetWindowThreadProcessId(foreground, out var processId);
        if (processId == (uint)Environment.ProcessId)
        {
            return null;
        }

        var info = new GuiThreadInfo { Size = Marshal.SizeOf<GuiThreadInfo>() };
        if (!GetGUIThreadInfo(threadId, ref info) || info.Caret == IntPtr.Zero)
        {
            return null;
        }

        var point = new Point((info.CaretRect.Left + info.CaretRect.Right) / 2, info.CaretRect.Bottom);
        return ClientToScreen(info.Caret, ref point) ? point : null;
    }

    public static HashSet<Keys> PressedTypingKeys()
    {
        var pressed = new HashSet<Keys>();
        for (var key = (int)Keys.Back; key <= (int)Keys.OemClear; key++)
        {
            var candidate = (Keys)key;
            if (candidate is Keys.ShiftKey or Keys.ControlKey or Keys.Menu or Keys.Capital
                or Keys.LWin or Keys.RWin or Keys.Escape or Keys.Tab)
            {
                continue;
            }

            if ((GetAsyncKeyState(key) & 0x8000) != 0)
            {
                pressed.Add(candidate);
            }
        }
        return pressed;
    }

    public static bool IsForeignFullscreen()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var r))
        {
            return false;
        }

        GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == (uint)Environment.ProcessId)
        {
            return false;
        }

        var rectangle = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
        var screen = Screen.FromRectangle(rectangle).Bounds;
        return Math.Abs(rectangle.Left - screen.Left) <= 2 && Math.Abs(rectangle.Top - screen.Top) <= 2
            && Math.Abs(rectangle.Right - screen.Right) <= 2 && Math.Abs(rectangle.Bottom - screen.Bottom) <= 2;
    }
}
