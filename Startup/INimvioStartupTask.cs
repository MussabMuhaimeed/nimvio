using Windows.ApplicationModel;

namespace Nimvio;

internal interface INimvioStartupTask
{
    StartupTaskState State { get; }

    Task RequestEnableAsync();
    
    void Disable();
}
