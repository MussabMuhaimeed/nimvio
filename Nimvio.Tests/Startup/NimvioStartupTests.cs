using Nimvio;
using Windows.ApplicationModel;
using Xunit;

namespace Nimvio.Tests.Startup;

public sealed class NimvioStartupTests
{
    [Fact]
    public void StartsWithWindowsUnpackagedEmptyStringRegistryValueReturnsTrue()
    {
        // Arrange
        var platform = new FakeStartupPlatform { RegistryValue = string.Empty };

        // Act
        var result = NimvioStartup.StartsWithWindows(platform);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TaskIdIsStableForPackagedManifestCompatibility()
    {
        // Arrange

        // Act
        var taskId = NimvioStartup.TaskId;

        // Assert
        Assert.Equal("NimvioStartup", taskId);
    }

    [Fact]
    public void StartsWithWindowsUnpackagedStringRegistryValueReturnsTrue()
    {
        // Arrange
        var platform = new FakeStartupPlatform { RegistryValue = "\"C:\\Apps\\Nimvio.exe\"" };

        // Act
        var result = NimvioStartup.StartsWithWindows(platform);

        // Assert
        Assert.True(result);
        Assert.Equal(1, platform.RegistryReadCount);
        Assert.Equal(0, platform.StartupTaskRequestCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(42)]
    public void StartsWithWindowsUnpackagedMissingOrNonStringValueReturnsFalse(object? registryValue)
    {
        // Arrange
        var platform = new FakeStartupPlatform { RegistryValue = registryValue };

        // Act
        var result = NimvioStartup.StartsWithWindows(platform);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(StartupTaskState.Enabled, true)]
    [InlineData(StartupTaskState.Disabled, false)]
    public void StartsWithWindowsPackagedUsesStartupTaskState(StartupTaskState state, bool expected)
    {
        // Arrange
        var platform = new FakeStartupPlatform { IsPackaged = true, Task = new FakeStartupTask(state) };

        // Act
        var result = NimvioStartup.StartsWithWindows(platform);

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(1, platform.StartupTaskRequestCount);
        Assert.Equal(0, platform.RegistryReadCount);
    }

    [Fact]
    public void StartsWithWindowsPackagedApiFailureReturnsFalse()
    {
        // Arrange
        var platform = new FakeStartupPlatform { IsPackaged = true, StartupTaskException = new UnauthorizedAccessException() };

        // Act
        var result = NimvioStartup.StartsWithWindows(platform);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SetStartWithWindowsAsyncUnpackagedEnableWritesQuotedExecutablePath()
    {
        // Arrange
        var platform = new FakeStartupPlatform { ProcessPath = @"C:\Program Files\Nimvio\Nimvio.exe" };

        // Act
        await NimvioStartup.SetStartWithWindowsAsync(true, platform);

        // Assert
        Assert.Equal("\"C:\\Program Files\\Nimvio\\Nimvio.exe\"", platform.WrittenRegistryValue);
        Assert.Equal(0, platform.RegistryDeleteCount);
        Assert.Equal(0, platform.StartupTaskRequestCount);
    }

    [Fact]
    public async Task SetStartWithWindowsAsyncUnpackagedDisableDeletesValueWithoutWriting()
    {
        // Arrange
        var platform = new FakeStartupPlatform();

        // Act
        await NimvioStartup.SetStartWithWindowsAsync(false, platform);

        // Assert
        Assert.Equal(1, platform.RegistryDeleteCount);
        Assert.Null(platform.WrittenRegistryValue);
    }

    [Fact]
    public async Task SetStartWithWindowsAsyncPackagedDisabledTaskRequestsEnableOnce()
    {
        // Arrange
        var task = new FakeStartupTask(StartupTaskState.Disabled);
        var platform = new FakeStartupPlatform { IsPackaged = true, Task = task };

        // Act
        await NimvioStartup.SetStartWithWindowsAsync(true, platform);

        // Assert
        Assert.Equal(1, task.EnableRequestCount);
        Assert.Equal(0, task.DisableCount);
        Assert.Null(platform.WrittenRegistryValue);
    }

    [Fact]
    public async Task SetStartWithWindowsAsyncPackagedAlreadyEnabledDoesNotRequestAgain()
    {
        // Arrange
        var task = new FakeStartupTask(StartupTaskState.Enabled);
        var platform = new FakeStartupPlatform { IsPackaged = true, Task = task };

        // Act
        await NimvioStartup.SetStartWithWindowsAsync(true, platform);

        // Assert
        Assert.Equal(0, task.EnableRequestCount);
    }

    [Fact]
    public async Task SetStartWithWindowsAsyncPackagedDisableDisablesTaskWithoutRegistryAccess()
    {
        // Arrange
        var task = new FakeStartupTask(StartupTaskState.Enabled);
        var platform = new FakeStartupPlatform { IsPackaged = true, Task = task };

        // Act
        await NimvioStartup.SetStartWithWindowsAsync(false, platform);

        // Assert
        Assert.Equal(1, task.DisableCount);
        Assert.Equal(0, platform.RegistryDeleteCount);
        Assert.Null(platform.WrittenRegistryValue);
    }
}
