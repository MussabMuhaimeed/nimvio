using Microsoft.Win32;
using Windows.ApplicationModel;

namespace Nimvio;

internal sealed class WindowsNimvioStartupPlatform : INimvioStartupPlatform
{
    private const string RegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RegistryValueName = "Nimvio";

    public bool IsPackaged
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

    public string? ProcessPath => Environment.ProcessPath;

    public async Task<INimvioStartupTask> GetStartupTaskAsync()
        => new WindowsNimvioStartupTask(await StartupTask.GetAsync(NimvioStartup.TaskId));

    public object? ReadRegistryValue()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
        return key?.GetValue(RegistryValueName);
    }

    public void WriteRegistryValue(string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
        key.SetValue(RegistryValueName, value);
    }

    public void DeleteRegistryValue()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
        key.DeleteValue(RegistryValueName, false);
    }
}
