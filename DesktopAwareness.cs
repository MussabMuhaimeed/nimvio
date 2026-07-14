using System.Runtime.InteropServices;

namespace Nimvio;

internal static class DesktopAwareness
{
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }

    public static List<Rectangle> VisibleWindows(Screen screen)
    {
        var result = new List<Rectangle>();
        var ownProcess = (uint)Environment.ProcessId;
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) || IsIconic(hwnd)) return true;
            GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == ownProcess || !GetWindowRect(hwnd, out var r)) return true;
            var rectangle = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
            if (rectangle.Width < 180 || rectangle.Height < 100) return true;
            if (screen.Bounds.IntersectsWith(rectangle)) result.Add(rectangle);
            return true;
        }, IntPtr.Zero);
        return result;
    }

    public static Point? ActiveWindowCenter()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var r)) return null;
        return new Point((r.Left + r.Right) / 2, (r.Top + r.Bottom) / 2);
    }

    public static bool IsForeignFullscreen()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var r)) return false;
        GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == (uint)Environment.ProcessId) return false;
        var rectangle = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
        var screen = Screen.FromRectangle(rectangle).Bounds;
        return Math.Abs(rectangle.Left - screen.Left) <= 2 && Math.Abs(rectangle.Top - screen.Top) <= 2
            && Math.Abs(rectangle.Right - screen.Right) <= 2 && Math.Abs(rectangle.Bottom - screen.Bottom) <= 2;
    }
}
