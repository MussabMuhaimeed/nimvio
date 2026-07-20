using Windows.ApplicationModel;

namespace Nimvio;

internal sealed class WindowsNimvioStartupTask(StartupTask task) : INimvioStartupTask
{
    public StartupTaskState State => task.State;

    public async Task RequestEnableAsync() => _ = await task.RequestEnableAsync();

    public void Disable() => task.Disable();
}
