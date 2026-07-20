using Nimvio;
using Xunit;

namespace Nimvio.Tests.Mind;

public sealed class DesktopAwarenessRulesTests
{
    private const uint OwnProcess = 100;
    private const uint ForeignProcess = 200;

    [Theory]
    [InlineData(180, 100, true)]
    [InlineData(179, 100, false)]
    [InlineData(180, 99, false)]
    [InlineData(500, 400, true)]
    public void MeetsVisibleWindowSizeThresholdUsesMinimumWidthAndHeight(int width, int height, bool expected)
    {
        // Arrange
        var rectangle = new Rectangle(0, 0, width, height);

        // Act
        var result = DesktopAwarenessRules.MeetsVisibleWindowSizeThreshold(rectangle);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(0, 10, false)]
    [InlineData(10, 0, false)]
    public void HasPositiveSizeRequiresNonZeroDimensions(int width, int height, bool expected)
    {
        // Arrange
        var rectangle = new Rectangle(0, 0, width, height);

        // Act
        var result = DesktopAwarenessRules.HasPositiveSize(rectangle);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsForeignProcessComparesAgainstOwnProcessId()
    {
        // Arrange

        // Act
        var sameProcess = DesktopAwarenessRules.IsForeignProcess(OwnProcess, OwnProcess);
        var foreignProcess = DesktopAwarenessRules.IsForeignProcess(ForeignProcess, OwnProcess);

        // Assert
        Assert.False(sameProcess);
        Assert.True(foreignProcess);
    }

    [Fact]
    public void IntersectsScreenReturnsFalseWhenWindowIsOutsideScreenBounds()
    {
        // Arrange
        var screen = new Rectangle(0, 0, 1920, 1080);
        var window = new Rectangle(2000, 0, 2180, 100);

        // Act
        var result = DesktopAwarenessRules.IntersectsScreen(window, screen);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldIncludeForeignWindowOnScreenRequiresForeignProcessSizeAndOverlap()
    {
        // Arrange
        var screen = new Rectangle(0, 0, 1920, 1080);
        var window = new Rectangle(100, 100, 400, 300);

        // Act
        var included = DesktopAwarenessRules.ShouldIncludeForeignWindowOnScreen(window, screen, ForeignProcess, OwnProcess);
        var ownProcess = DesktopAwarenessRules.ShouldIncludeForeignWindowOnScreen(window, screen, OwnProcess, OwnProcess);
        var tooSmall = DesktopAwarenessRules.ShouldIncludeForeignWindowOnScreen(
            new Rectangle(0, 0, 100, 100), screen, ForeignProcess, OwnProcess);

        // Assert
        Assert.True(included);
        Assert.False(ownProcess);
        Assert.False(tooSmall);
    }

    [Fact]
    public void QualifyingActiveForeignWindowRejectsOwnProcessAndSmallWindows()
    {
        // Arrange
        var large = new Rectangle(0, 0, 800, 600);
        var small = new Rectangle(0, 0, 100, 100);

        // Act
        var qualifying = DesktopAwarenessRules.QualifyingActiveForeignWindow(large, ForeignProcess, OwnProcess);
        var ownProcess = DesktopAwarenessRules.QualifyingActiveForeignWindow(large, OwnProcess, OwnProcess);
        var tooSmall = DesktopAwarenessRules.QualifyingActiveForeignWindow(small, ForeignProcess, OwnProcess);

        // Assert
        Assert.Equal(large, qualifying);
        Assert.Null(ownProcess);
        Assert.Null(tooSmall);
    }

    [Fact]
    public void WindowCenterReturnsMidpointOfRectangle()
    {
        // Arrange
        var rectangle = new Rectangle(10, 20, 100, 200);

        // Act
        var center = DesktopAwarenessRules.WindowCenter(rectangle);

        // Assert
        Assert.Equal(new Point(60, 120), center);
    }

    [Theory]
    [InlineData(0, 0, 1920, 1080, true)]
    [InlineData(2, 2, 1918, 1078, true)]
    [InlineData(3, 0, 1920, 1080, false)]
    [InlineData(0, 0, 1910, 1080, false)]
    public void CoversScreenWithinToleranceAllowsTwoPixelInset(int left, int top, int right, int bottom, bool expected)
    {
        // Arrange
        var screen = new Rectangle(0, 0, 1920, 1080);
        var window = new Rectangle(left, top, right, bottom);

        // Act
        var result = DesktopAwarenessRules.CoversScreenWithinTolerance(window, screen);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(Keys.A, false)]
    [InlineData(Keys.ShiftKey, true)]
    [InlineData(Keys.Tab, true)]
    [InlineData(Keys.LWin, true)]
    public void IsIgnoredTypingKeyMatchesModifierAndNavigationKeys(Keys key, bool expected)
    {
        // Arrange

        // Act
        var result = DesktopAwarenessRules.IsIgnoredTypingKey(key);

        // Assert
        Assert.Equal(expected, result);
    }
}
