using Windows.ApplicationModel;

namespace Nimvio;

internal static class NimvioStartup
{
    internal const string TaskId = "NimvioStartup";
    
    private static readonly INimvioStartupPlatform Platform = new WindowsNimvioStartupPlatform();

    internal static bool IsPackaged => Platform.IsPackaged;

    internal static bool StartsWithWindows() => StartsWithWindows(Platform);

    internal static bool StartsWithWindows(INimvioStartupPlatform platform)
    {
        if (platform.IsPackaged)
        {
            try
            {
                return platform.GetStartupTaskAsync().GetAwaiter().GetResult().State == StartupTaskState.Enabled;
            }
            catch
            {
                return false;
            }
        }

        return platform.ReadRegistryValue() is string;
    }

    internal static Task SetStartWithWindowsAsync(bool enabled) => SetStartWithWindowsAsync(enabled, Platform);

    internal static async Task SetStartWithWindowsAsync(bool enabled, INimvioStartupPlatform platform)
    {
        if (platform.IsPackaged)
        {
            var task = await platform.GetStartupTaskAsync();
            if (enabled)
            {
                if (task.State == StartupTaskState.Disabled)
                {
                    await task.RequestEnableAsync();
                }
            }
            else
            {
                task.Disable();
            }

            return;
        }

        if (enabled)
        {
            platform.WriteRegistryValue($"\"{platform.ProcessPath}\"");
        }
        else
        {
            platform.DeleteRegistryValue();
        }
    }
}
