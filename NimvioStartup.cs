using Microsoft.Win32;
using Windows.ApplicationModel;

namespace Nimvio;

internal static class NimvioStartup
{
    internal const string TaskId = "NimvioStartup";
    private const string RegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RegistryValueName = "Nimvio";

    internal static bool IsPackaged
    {
        get
        {
            try
            {
                _ = Package.Current.Id;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    internal static bool StartsWithWindows()
    {
        if (IsPackaged)
        {
            try
            {
                return GetStartupTask().State == StartupTaskState.Enabled;
            }
            catch
            {
                return false;
            }
        }

        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
        return key?.GetValue(RegistryValueName) is string;
    }

    internal static async Task SetStartWithWindowsAsync(bool enabled)
    {
        if (IsPackaged)
        {
            var task = await StartupTask.GetAsync(TaskId);
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

        using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
        if (enabled)
        {
            key.SetValue(RegistryValueName, $"\"{Environment.ProcessPath}\"");
        }
        else
        {
            key.DeleteValue(RegistryValueName, false);
        }
    }

    private static StartupTask GetStartupTask() => StartupTask.GetAsync(TaskId).AsTask().GetAwaiter().GetResult();
}
