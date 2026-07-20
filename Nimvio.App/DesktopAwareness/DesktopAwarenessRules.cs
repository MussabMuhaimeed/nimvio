namespace Nimvio;

internal static class DesktopAwarenessRules
{
    internal const int MinWindowWidth = 180;
    internal const int MinWindowHeight = 100;
    internal const int FullscreenEdgeTolerance = 2;

    internal static bool MeetsVisibleWindowSizeThreshold(Rectangle rectangle)
        => rectangle.Width >= MinWindowWidth && rectangle.Height >= MinWindowHeight;

    internal static bool HasPositiveSize(Rectangle rectangle)
        => rectangle.Width > 0 && rectangle.Height > 0;

    internal static bool IsForeignProcess(uint processId, uint ownProcessId)
        => processId != ownProcessId;

    internal static bool IntersectsScreen(Rectangle window, Rectangle screenBounds)
        => screenBounds.IntersectsWith(window);

    internal static bool ShouldIncludeForeignWindowOnScreen(
        Rectangle window,
        Rectangle screenBounds,
        uint processId,
        uint ownProcessId)
        => IsForeignProcess(processId, ownProcessId)
            && MeetsVisibleWindowSizeThreshold(window)
            && IntersectsScreen(window, screenBounds);

    internal static Rectangle? QualifyingActiveForeignWindow(Rectangle rectangle, uint processId, uint ownProcessId)
        => !IsForeignProcess(processId, ownProcessId) || !MeetsVisibleWindowSizeThreshold(rectangle)
            ? null
            : rectangle;

    internal static Point WindowCenter(Rectangle rectangle)
        => new((rectangle.Left + rectangle.Right) / 2, (rectangle.Top + rectangle.Bottom) / 2);

    internal static bool CoversScreenWithinTolerance(
        Rectangle window,
        Rectangle screen,
        int tolerance = FullscreenEdgeTolerance)
        => Math.Abs(window.Left - screen.Left) <= tolerance
            && Math.Abs(window.Top - screen.Top) <= tolerance
            && Math.Abs(window.Right - screen.Right) <= tolerance
            && Math.Abs(window.Bottom - screen.Bottom) <= tolerance;

    internal static bool IsIgnoredTypingKey(Keys key)
        => key is Keys.ShiftKey or Keys.ControlKey or Keys.Menu or Keys.Capital
            or Keys.LWin or Keys.RWin or Keys.Escape or Keys.Tab;
}
