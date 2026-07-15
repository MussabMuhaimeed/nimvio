using Microsoft.Win32;
using System.Text.Json;

namespace Nimvio;

internal enum NimvioPersonality { Curious, Calm, Playful }
internal enum ActivityLevel { Calm, Normal, Energetic }
internal enum AutonomyLevel { Low, Normal, High }

internal sealed class NimvioProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Nova";
    public NimvioPersonality Personality { get; set; } = NimvioPersonality.Curious;
    public int AccentArgb { get; set; } = Color.FromArgb(86, 221, 242).ToArgb();
    public float Energy { get; set; } = 72;
    public float Curiosity { get; set; } = 66;
    public float Boredom { get; set; } = 18;
    public float Happiness { get; set; } = 75;
    public List<Point> RecentPlaces { get; set; } = [];
    public string? FavoriteScreen { get; set; }
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastInteractionUtc { get; set; } = DateTime.UtcNow;
    public Dictionary<string, float> Relationships { get; set; } = [];
    public string? FavoriteFriendId { get; set; }
}

internal sealed class NimvioSettings
{
    public bool Playing { get; set; } = true;
    public float Speed { get; set; } = 1f;
    public int Size { get; set; } = 112;
    public ActivityLevel Activity { get; set; } = ActivityLevel.Normal;
    public AutonomyLevel Autonomy { get; set; } = AutonomyLevel.Normal;
    public bool QuietHoursEnabled { get; set; }
    public int QuietStartHour { get; set; } = 22;
    public int QuietEndHour { get; set; } = 7;
    public bool PauseInFullscreen { get; set; } = true;
    public List<string> AllowedScreens { get; set; } = [];
    public List<NimvioProfile> Profiles { get; set; } = [];

    private static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nimvio");
    private static string FilePath => Path.Combine(Folder, "settings.json");
    private const string StartupKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static NimvioSettings Load()
    {
        NimvioSettings settings;
        try
        {
            settings = File.Exists(FilePath)
                ? JsonSerializer.Deserialize<NimvioSettings>(File.ReadAllText(FilePath)) ?? new()
                : new();
        }
        catch { settings = new(); }

        settings.Profiles ??= [];
        settings.AllowedScreens ??= [];
        var supportedNames = new[] { "Nova", "Mimo", "Lumi" };
        settings.Profiles.RemoveAll(profile => !supportedNames.Contains(profile.Name));
        if (settings.Profiles.Count > 3) settings.Profiles.RemoveRange(3, settings.Profiles.Count - 3);
        if (settings.Profiles.Count == 0) settings.Profiles.Add(new NimvioProfile());
        foreach (var profile in settings.Profiles)
        {
            profile.Relationships ??= [];
        }
        return settings;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public bool IsQuietTime(DateTime now)
    {
        if (!QuietHoursEnabled) return false;
        return QuietStartHour < QuietEndHour
            ? now.Hour >= QuietStartHour && now.Hour < QuietEndHour
            : now.Hour >= QuietStartHour || now.Hour < QuietEndHour;
    }

    public Screen[] EnabledScreens()
    {
        var screens = Screen.AllScreens;
        if (AllowedScreens.Count == 0) return screens;
        var enabled = screens.Where(screen => AllowedScreens.Contains(screen.DeviceName)).ToArray();
        return enabled.Length == 0 ? screens : enabled;
    }

    public static bool StartsWithWindows()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupKey);
        return key?.GetValue("Nimvio") is string;
    }

    public static void SetStartWithWindows(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(StartupKey);
        if (enabled) key.SetValue("Nimvio", $"\"{Environment.ProcessPath}\"");
        else key.DeleteValue("Nimvio", false);
    }
}
