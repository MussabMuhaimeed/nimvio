using Nimvio;
using Windows.ApplicationModel;

namespace Nimvio.Tests.Startup;

internal sealed class FakeStartupPlatform : INimvioStartupPlatform
{
    public bool IsPackaged { get; init; }
    public string? ProcessPath { get; init; } = @"C:\Nimvio.exe";
    public object? RegistryValue { get; init; }
    public FakeStartupTask Task { get; init; } = new(StartupTaskState.Disabled);
    public Exception? StartupTaskException { get; init; }
    public int StartupTaskRequestCount { get; private set; }
    public int RegistryReadCount { get; private set; }
    public int RegistryDeleteCount { get; private set; }
    public string? WrittenRegistryValue { get; private set; }

    public Task<INimvioStartupTask> GetStartupTaskAsync()
    {
        StartupTaskRequestCount++;
        return StartupTaskException is null
            ? System.Threading.Tasks.Task.FromResult<INimvioStartupTask>(Task)
            : System.Threading.Tasks.Task.FromException<INimvioStartupTask>(StartupTaskException);
    }

    public object? ReadRegistryValue()
    {
        RegistryReadCount++;
        return RegistryValue;
    }

    public void WriteRegistryValue(string value) => WrittenRegistryValue = value;
    public void DeleteRegistryValue() => RegistryDeleteCount++;
}
