using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Nimvio;

internal static class DesktopAwareness
{
    internal static Func<Screen, List<WindowSnapshot>>? VisibleWindowSnapshotsOverride { get; set; }
    internal static Func<WindowSnapshot?>? ActiveWindowOverride { get; set; }
    internal static Func<IntPtr, (bool Ok, Rectangle Bounds)>? TryGetWindowRectangleOverride { get; set; }
    internal static Func<HashSet<IntPtr>>? VisibleWindowHandlesOverride { get; set; }
    internal static Func<Point?>? ActiveCaretPositionOverride { get; set; }
    internal static Func<HashSet<Keys>>? PressedTypingKeysOverride { get; set; }
    internal static Func<TimeSpan>? UserIdleTimeOverride { get; set; }
    internal static Func<bool>? IsForeignFullscreenOverride { get; set; }
    internal static Func<Point?>? ActiveWindowCenterOverride { get; set; }

    internal static void ClearTestOverrides()
    {
        VisibleWindowSnapshotsOverride = null;
        ActiveWindowOverride = null;
        TryGetWindowRectangleOverride = null;
        VisibleWindowHandlesOverride = null;
        ActiveCaretPositionOverride = null;
        PressedTypingKeysOverride = null;
        UserIdleTimeOverride = null;
        IsForeignFullscreenOverride = null;
        ActiveWindowCenterOverride = null;
    }

    public static List<Rectangle> VisibleWindows(Screen screen)
        => VisibleWindowSnapshots(screen).Select(window => window.Bounds).ToList();

    public static List<WindowSnapshot> VisibleWindowSnapshots(Screen screen)
    {
        if (VisibleWindowSnapshotsOverride is { } snapshotsOverride)
        {
            return snapshotsOverride(screen);
        }

        var result = new List<WindowSnapshot>();
        var ownProcess = (uint)Environment.ProcessId;
        DesktopAwarenessNativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!DesktopAwarenessNativeMethods.IsWindowVisible(hwnd) || DesktopAwarenessNativeMethods.IsIconic(hwnd))
            {
                return true;
            }

            DesktopAwarenessNativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == ownProcess || !DesktopAwarenessNativeMethods.GetWindowRect(hwnd, out var r))
            {
                return true;
            }

            var rectangle = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
            if (!DesktopAwarenessRules.MeetsVisibleWindowSizeThreshold(rectangle))
            {
                return true;
            }

            if (DesktopAwarenessRules.ShouldIncludeForeignWindowOnScreen(rectangle, screen.Bounds, processId, ownProcess))
            {
                var titleLength = DesktopAwarenessNativeMethods.GetWindowTextLength(hwnd);
                var title = new StringBuilder(Math.Max(1, titleLength + 1));
                DesktopAwarenessNativeMethods.GetWindowText(hwnd, title, title.Capacity);
                try
                {
                    result.Add(new WindowSnapshot(hwnd, rectangle,
                        Process.GetProcessById((int)processId).ProcessName, title.ToString()));
                }
                catch
                {
                    result.Add(new WindowSnapshot(hwnd, rectangle, string.Empty, title.ToString()));
                }
            }

            return true;
        }, IntPtr.Zero);
        return result;
    }

    public static Point? ActiveWindowCenter()
    {
        if (ActiveWindowCenterOverride is { } centerOverride)
        {
            return centerOverride();
        }

        var hwnd = DesktopAwarenessNativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero || !DesktopAwarenessNativeMethods.GetWindowRect(hwnd, out var r))
        {
            return null;
        }

        return DesktopAwarenessRules.WindowCenter(Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom));
    }

    public static Rectangle? ActiveWindowRectangle()
    {
        var hwnd = DesktopAwarenessNativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero || !DesktopAwarenessNativeMethods.GetWindowRect(hwnd, out var r))
        {
            return null;
        }

        DesktopAwarenessNativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == (uint)Environment.ProcessId)
        {
            return null;
        }

        var rectangle = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
        return DesktopAwarenessRules.QualifyingActiveForeignWindow(rectangle, processId, (uint)Environment.ProcessId);
    }

    public static WindowSnapshot? ActiveWindow()
    {
        if (ActiveWindowOverride is { } activeOverride)
        {
            return activeOverride();
        }

        var hwnd = DesktopAwarenessNativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero || !TryGetWindowRectangle(hwnd, out var rectangle))
        {
            return null;
        }

        DesktopAwarenessNativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (!DesktopAwarenessRules.IsForeignProcess(processId, (uint)Environment.ProcessId)
            || !DesktopAwarenessRules.MeetsVisibleWindowSizeThreshold(rectangle))
        {
            return null;
        }

        var titleLength = DesktopAwarenessNativeMethods.GetWindowTextLength(hwnd);
        var title = new StringBuilder(Math.Max(1, titleLength + 1));
        DesktopAwarenessNativeMethods.GetWindowText(hwnd, title, title.Capacity);
        try { return new WindowSnapshot(hwnd, rectangle, Process.GetProcessById((int)processId).ProcessName, title.ToString()); }
        catch { return new WindowSnapshot(hwnd, rectangle, string.Empty, title.ToString()); }
    }

    public static bool TryGetWindowRectangle(IntPtr hwnd, out Rectangle rectangle)
    {
        if (TryGetWindowRectangleOverride is { } rectOverride)
        {
            var result = rectOverride(hwnd);
            rectangle = result.Bounds;
            return result.Ok;
        }

        rectangle = Rectangle.Empty;
        if (hwnd == IntPtr.Zero || !DesktopAwarenessNativeMethods.IsWindowVisible(hwnd) || DesktopAwarenessNativeMethods.IsIconic(hwnd)
            || !DesktopAwarenessNativeMethods.GetWindowRect(hwnd, out var r))
        {
            return false;
        }

        rectangle = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
        return DesktopAwarenessRules.HasPositiveSize(rectangle);
    }

    public static HashSet<IntPtr> VisibleWindowHandles()
    {
        if (VisibleWindowHandlesOverride is { } handlesOverride)
        {
            return handlesOverride();
        }

        var result = new HashSet<IntPtr>();
        var ownProcess = (uint)Environment.ProcessId;
        DesktopAwarenessNativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!TryGetWindowRectangle(hwnd, out var rectangle)
                || !DesktopAwarenessRules.MeetsVisibleWindowSizeThreshold(rectangle))
            {
                return true;
            }

            DesktopAwarenessNativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
            if (DesktopAwarenessRules.IsForeignProcess(processId, ownProcess))
            {
                result.Add(hwnd);
            }

            return true;
        }, IntPtr.Zero);
        return result;
    }

    public static Point? ActiveCaretPosition()
    {
        if (ActiveCaretPositionOverride is { } caretOverride)
        {
            return caretOverride();
        }

        var foreground = DesktopAwarenessNativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            return null;
        }

        var threadId = DesktopAwarenessNativeMethods.GetWindowThreadProcessId(foreground, out var processId);
        if (processId == (uint)Environment.ProcessId)
        {
            return null;
        }

        var info = new GuiThreadInfo { Size = Marshal.SizeOf<GuiThreadInfo>() };
        if (!DesktopAwarenessNativeMethods.GetGUIThreadInfo(threadId, ref info) || info.Caret == IntPtr.Zero)
        {
            return null;
        }

        var point = new Point((info.CaretRect.Left + info.CaretRect.Right) / 2, info.CaretRect.Bottom);
        return DesktopAwarenessNativeMethods.ClientToScreen(info.Caret, ref point) ? point : null;
    }

    public static HashSet<Keys> PressedTypingKeys()
    {
        if (PressedTypingKeysOverride is { } keysOverride)
        {
            return keysOverride();
        }

        var pressed = new HashSet<Keys>();
        for (var key = (int)Keys.Back; key <= (int)Keys.OemClear; key++)
        {
            var candidate = (Keys)key;
            if (DesktopAwarenessRules.IsIgnoredTypingKey(candidate))
            {
                continue;
            }

            if ((DesktopAwarenessNativeMethods.GetAsyncKeyState(key) & 0x8000) != 0)
            {
                pressed.Add(candidate);
            }
        }
        return pressed;
    }

    public static TimeSpan UserIdleTime()
    {
        if (UserIdleTimeOverride is { } idleOverride)
        {
            return idleOverride();
        }

        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!DesktopAwarenessNativeMethods.GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        var elapsedMilliseconds = unchecked((uint)Environment.TickCount - info.TickCount);
        return TimeSpan.FromMilliseconds(elapsedMilliseconds);
    }

    public static bool IsForeignFullscreen()
    {
        if (IsForeignFullscreenOverride is { } fullscreenOverride)
        {
            return fullscreenOverride();
        }

        var hwnd = DesktopAwarenessNativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero || !DesktopAwarenessNativeMethods.GetWindowRect(hwnd, out var r))
        {
            return false;
        }

        DesktopAwarenessNativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (!DesktopAwarenessRules.IsForeignProcess(processId, (uint)Environment.ProcessId))
        {
            return false;
        }

        var rectangle = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
        var screen = Screen.FromRectangle(rectangle).Bounds;
        return DesktopAwarenessRules.CoversScreenWithinTolerance(rectangle, screen);
    }
}
