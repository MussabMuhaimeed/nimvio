namespace Nimvio;

internal interface INimvioStartupPlatform
{
    bool IsPackaged { get; }
    
    string? ProcessPath { get; }

    Task<INimvioStartupTask> GetStartupTaskAsync();

    object? ReadRegistryValue();

    void WriteRegistryValue(string value);

    void DeleteRegistryValue();
}
