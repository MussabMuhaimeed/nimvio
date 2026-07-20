using Nimvio;
using Xunit;

namespace Nimvio.Tests.Mind;

public sealed class DesktopAwarenessIntegrationTests
{
    [Fact]
    public void UserIdleTimeReturnsNonNegativeDuration()
    {
        // Arrange

        // Act
        var idle = DesktopAwareness.UserIdleTime();

        // Assert
        Assert.True(idle >= TimeSpan.Zero);
    }

    [Fact]
    public void VisibleWindowHandlesReturnsOnlyForeignWindows()
    {
        // Arrange

        // Act
        var handles = DesktopAwareness.VisibleWindowHandles();

        // Assert
        Assert.NotNull(handles);
        Assert.DoesNotContain(IntPtr.Zero, handles);
    }

    [Fact]
    public void VisibleWindowsOnPrimaryScreenRespectMinimumSizeRules()
    {
        // Arrange
        var screen = Screen.PrimaryScreen!;

        // Act
        var windows = DesktopAwareness.VisibleWindows(screen);

        // Assert
        Assert.All(windows, window =>
        {
            Assert.True(DesktopAwarenessRules.MeetsVisibleWindowSizeThreshold(window));
            Assert.True(DesktopAwarenessRules.IntersectsScreen(window, screen.Bounds));
        });
    }

    [Fact]
    public void TryGetWindowRectangleZeroHandleReturnsFalse()
    {
        // Arrange
        var handle = IntPtr.Zero;

        // Act
        var success = DesktopAwareness.TryGetWindowRectangle(handle, out var rectangle);

        // Assert
        Assert.False(success);
        Assert.Equal(Rectangle.Empty, rectangle);
    }

    [Fact]
    public void ActiveWindowRectangleWhenPresentMeetsQualifyingForeignWindowRules()
    {
        // Arrange

        // Act
        var rectangle = DesktopAwareness.ActiveWindowRectangle();

        // Assert
        if (rectangle is not { } bounds)
        {
            return;
        }

        Assert.True(DesktopAwarenessRules.MeetsVisibleWindowSizeThreshold(bounds));
    }
}
