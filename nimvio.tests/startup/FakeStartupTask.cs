using Nimvio;
using Windows.ApplicationModel;

namespace Nimvio.Tests.Startup;

internal sealed class FakeStartupTask(StartupTaskState state) : INimvioStartupTask
{
    public StartupTaskState State { get; } = state;
    public int EnableRequestCount { get; private set; }
    public int DisableCount { get; private set; }

    public Task RequestEnableAsync()
    {
        EnableRequestCount++;
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public void Disable() => DisableCount++;
}
