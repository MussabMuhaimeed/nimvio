using System.Text.Json;

namespace Nimvio;

internal sealed class NimvioSettings
{
    public bool Playing { get; set; } = true;
    public float Speed { get; set; } = 1f;
    public int Size { get; set; } = 112;

    public ActivityLevel Activity { get; set; } = ActivityLevel.Normal;
    public AutonomyLevel Autonomy { get; set; } = AutonomyLevel.Normal;
    
    public bool PauseInFullscreen { get; set; } = true;
    public List<string> AllowedScreens { get; set; } = [];
    public List<NimvioProfile> Profiles { get; set; } = [];

    private static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nimvio");
    
    private static string FilePath => Path.Combine(Folder, "settings.json");

    internal string? PersistenceFilePath { get; set; }
    
    public static NimvioSettings Load(string? filePath = null)
    {
        filePath ??= FilePath;
        var persistencePath = filePath;
        NimvioSettings settings;
        try
        {
            settings = File.Exists(filePath)
                ? JsonSerializer.Deserialize<NimvioSettings>(File.ReadAllText(filePath)) ?? new()
                : new();
        }
        catch { settings = new(); }

        settings.Profiles ??= [];
        settings.AllowedScreens ??= [];
        settings.Profiles.RemoveAll(profile => profile.Name == NimvioCharacterName.Unknown);
        settings.Profiles = settings.Profiles
            .GroupBy(profile => profile.Name)
            .Select(group => group.First())
            .ToList();
        if (settings.Profiles.Count > 3)
        {
            settings.Profiles.RemoveRange(3, settings.Profiles.Count - 3);
        }

        if (settings.Profiles.Count == 0)
        {
            settings.Profiles.Add(new NimvioProfile());
        }

        foreach (var profile in settings.Profiles)
        {
            profile.Relationships ??= [];
        }
        settings.NormalizeRelationshipMemory();
        settings.PersistenceFilePath = persistencePath;
        return settings;
    }

    public void RemoveProfileMemory(string removedProfileId)
    {
        foreach (var profile in Profiles)
        {
            profile.Relationships.Remove(removedProfileId);
            if (profile.FavoriteFriendId == removedProfileId)
            {
                profile.FavoriteFriendId = null;
            }
        }

        NormalizeRelationshipMemory();
    }

    private void NormalizeRelationshipMemory()
    {
        var validIds = Profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.Id))
            .Select(profile => profile.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var profile in Profiles)
        {
            profile.Relationships ??= [];
            profile.Relationships = profile.Relationships
                .Where(pair => pair.Key != profile.Id && validIds.Contains(pair.Key))
                .ToDictionary(pair => pair.Key, pair => Math.Clamp(pair.Value, -100, 100), StringComparer.Ordinal);

            if (profile.FavoriteFriendId is null
                || !profile.Relationships.TryGetValue(profile.FavoriteFriendId, out var favoriteRelationship)
                || favoriteRelationship <= 0)
            {
                profile.FavoriteFriendId = profile.Relationships
                    .Where(pair => pair.Value > 0)
                    .OrderByDescending(pair => pair.Value)
                    .Select(pair => pair.Key)
                    .FirstOrDefault();
            }
        }
    }

    public void Save(string? filePath = null)
    {
        try
        {
            filePath ??= PersistenceFilePath ?? FilePath;
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(filePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public Screen[] EnabledScreens()
    {
        var screens = Screen.AllScreens;
        if (AllowedScreens.Count == 0)
        {
            return screens;
        }

        var enabled = screens.Where(screen => AllowedScreens.Contains(screen.DeviceName)).ToArray();
        return enabled.Length == 0 ? screens : enabled;
    }

    public static bool StartsWithWindows() => NimvioStartup.StartsWithWindows();

    public static Task SetStartWithWindowsAsync(bool enabled) => NimvioStartup.SetStartWithWindowsAsync(enabled);
}
