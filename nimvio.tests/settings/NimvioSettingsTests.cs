using System.Text.Json;
using Nimvio;
using Xunit;

namespace Nimvio.Tests.Settings;

public sealed class NimvioSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "nimvio-settings-tests", Guid.NewGuid().ToString("N"));
    private string SettingsPath => Path.Combine(_directory, "nested", "settings.json");

    [Fact]
    public void LoadWhenFileDoesNotExistReturnsSafeDefaultsWithoutCreatingFile()
    {
        // Arrange

        // Act
        var settings = NimvioSettings.Load(SettingsPath);

        // Assert
        Assert.True(settings.Playing);
        Assert.Equal(1f, settings.Speed);
        Assert.Equal(112, settings.Size);
        Assert.Equal(ActivityLevel.Normal, settings.Activity);
        Assert.Equal(AutonomyLevel.Normal, settings.Autonomy);
        Assert.True(settings.PauseInFullscreen);
        var profile = Assert.Single(settings.Profiles);
        Assert.Equal(NimvioCharacterName.Nova, profile.Name);
        Assert.False(File.Exists(SettingsPath));
    }

    [Fact]
    public void SaveAndLoadRoundTripsEveryPersistedSettingAndMemoryField()
    {
        // Arrange
        var seen = new DateTime(2026, 7, 20, 8, 30, 0, DateTimeKind.Utc);
        var interaction = seen.AddMinutes(-4);
        var nova = new NimvioProfile
        {
            Id = "nova-id", Name = NimvioCharacterName.Nova, Personality = NimvioPersonality.Playful,
            AccentArgb = -123456, Energy = 31.5f, Curiosity = 82.25f, Boredom = 9.75f,
            Happiness = 64.5f, RecentPlaces = [new Point(120, 240), new Point(-30, 50)],
            FavoriteScreen = @"\\.\DISPLAY2", LastSeenUtc = seen, LastInteractionUtc = interaction,
            Relationships = { ["mimo-id"] = 47.5f }, FavoriteFriendId = "mimo-id"
        };
        var mimo = new NimvioProfile { Id = "mimo-id", Name = NimvioCharacterName.Mimo };
        var original = new NimvioSettings
        {
            Playing = false, Speed = 1.75f, Size = 144, Activity = ActivityLevel.Energetic,
            Autonomy = AutonomyLevel.High, PauseInFullscreen = false,
            AllowedScreens = [@"\\.\DISPLAY2"], Profiles = [nova, mimo]
        };

        // Act
        original.Save(SettingsPath);
        var loaded = NimvioSettings.Load(SettingsPath);

        // Assert
        Assert.False(loaded.Playing);
        Assert.Equal(1.75f, loaded.Speed);
        Assert.Equal(144, loaded.Size);
        Assert.Equal(ActivityLevel.Energetic, loaded.Activity);
        Assert.Equal(AutonomyLevel.High, loaded.Autonomy);
        Assert.False(loaded.PauseInFullscreen);
        Assert.Equal([@"\\.\DISPLAY2"], loaded.AllowedScreens);
        var loadedNova = Assert.Single(loaded.Profiles, profile => profile.Id == "nova-id");
        Assert.Equal(NimvioPersonality.Playful, loadedNova.Personality);
        Assert.Equal(-123456, loadedNova.AccentArgb);
        Assert.Equal(31.5f, loadedNova.Energy);
        Assert.Equal(82.25f, loadedNova.Curiosity);
        Assert.Equal(9.75f, loadedNova.Boredom);
        Assert.Equal(64.5f, loadedNova.Happiness);
        Assert.Equal(nova.RecentPlaces, loadedNova.RecentPlaces);
        Assert.Equal(@"\\.\DISPLAY2", loadedNova.FavoriteScreen);
        Assert.Equal(seen, loadedNova.LastSeenUtc);
        Assert.Equal(interaction, loadedNova.LastInteractionUtc);
        Assert.Equal(47.5f, loadedNova.Relationships["mimo-id"]);
        Assert.Equal("mimo-id", loadedNova.FavoriteFriendId);
    }

    [Fact]
    public void LoadWhenJsonIsCorruptRecoversWithUsableDefaults()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "{ this is not valid JSON");

        // Act
        var settings = NimvioSettings.Load(SettingsPath);

        // Assert
        Assert.Single(settings.Profiles);
        Assert.Equal(NimvioCharacterName.Nova, settings.Profiles[0].Name);
        Assert.NotNull(settings.AllowedScreens);
    }

    [Fact]
    public void LoadRemovesUnsupportedAndDuplicateCharactersDeterministically()
    {
        // Arrange
        WriteRaw(new NimvioSettings
        {
            Profiles =
            [
                new() { Id = "nova-first", Name = NimvioCharacterName.Nova, Energy = 11 },
                new() { Id = "unsupported", Name = NimvioCharacterName.Unknown },
                new() { Id = "nova-second", Name = NimvioCharacterName.Nova, Energy = 99 },
                new() { Id = "lumi", Name = NimvioCharacterName.Lumi },
                new() { Id = "mimo", Name = NimvioCharacterName.Mimo }
            ]
        });

        // Act
        var settings = NimvioSettings.Load(SettingsPath);

        // Assert
        Assert.Equal([NimvioCharacterName.Nova, NimvioCharacterName.Lumi, NimvioCharacterName.Mimo], settings.Profiles.Select(profile => profile.Name));
        Assert.Equal("nova-first", settings.Profiles[0].Id);
        Assert.Equal(11, settings.Profiles[0].Energy);
    }

    [Fact]
    public void LoadNormalizesRelationshipsAndSelectsBestValidFavorite()
    {
        // Arrange
        WriteRaw(new NimvioSettings
        {
            Profiles =
            [
                new()
                {
                    Id = "nova", Name = NimvioCharacterName.Nova, FavoriteFriendId = "missing",
                    Relationships = { ["nova"] = 70, ["missing"] = 90, ["mimo"] = 150, ["lumi"] = 35 }
                },
                new() { Id = "mimo", Name = NimvioCharacterName.Mimo },
                new() { Id = "lumi", Name = NimvioCharacterName.Lumi }
            ]
        });

        // Act
        var nova = NimvioSettings.Load(SettingsPath).Profiles.Single(profile => profile.Id == "nova");

        // Assert
        Assert.Equal(2, nova.Relationships.Count);
        Assert.Equal(100, nova.Relationships["mimo"]);
        Assert.Equal(35, nova.Relationships["lumi"]);
        Assert.Equal("mimo", nova.FavoriteFriendId);
    }

    [Fact]
    public void RemoveProfileMemoryRemovesOrphansAndRecalculatesFavorite()
    {
        // Arrange
        var settings = new NimvioSettings
        {
            Profiles =
            [
                new() { Id = "nova", Name = NimvioCharacterName.Nova, Relationships = { ["mimo"] = 80, ["lumi"] = 25 }, FavoriteFriendId = "mimo" },
                new() { Id = "lumi", Name = NimvioCharacterName.Lumi, Relationships = { ["mimo"] = -15 } }
            ]
        };

        // Act
        settings.RemoveProfileMemory("mimo");

        // Assert
        var nova = settings.Profiles.Single(profile => profile.Id == "nova");
        Assert.False(nova.Relationships.ContainsKey("mimo"));
        Assert.Equal("lumi", nova.FavoriteFriendId);
        Assert.Empty(settings.Profiles.Single(profile => profile.Id == "lumi").Relationships);
    }

    [Fact]
    public void LoadWhenCollectionsAreNullRepairsThem()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, """
            {
              "AllowedScreens": null,
              "Profiles": [
                { "Id": "nova", "Name": "Nova", "Relationships": null }
              ]
            }
            """);

        // Act
        var settings = NimvioSettings.Load(SettingsPath);

        // Assert
        Assert.NotNull(settings.AllowedScreens);
        Assert.NotNull(Assert.Single(settings.Profiles).Relationships);
    }

    [Fact]
    public void SaveCreatesParentDirectoryAndWritesReadableJsonWithMemoryFields()
    {
        // Arrange
        var settings = new NimvioSettings { Profiles = [new() { Name = NimvioCharacterName.Nova, Energy = 44, FavoriteScreen = "DISPLAY-X" }] };

        // Act
        settings.Save(SettingsPath);

        // Assert
        Assert.True(File.Exists(SettingsPath));
        var json = File.ReadAllText(SettingsPath);
        Assert.Contains(Environment.NewLine, json);
        using var document = JsonDocument.Parse(json);
        var profile = document.RootElement.GetProperty("Profiles")[0];
        Assert.Equal(44, profile.GetProperty("Energy").GetSingle());
        Assert.Equal("DISPLAY-X", profile.GetProperty("FavoriteScreen").GetString());
        Assert.True(profile.TryGetProperty("Relationships", out _));
        Assert.True(profile.TryGetProperty("LastSeenUtc", out _));
    }

    [Fact]
    public void CharacterNameUsesTextJsonAndUnknownNameIsDiscardedWithoutLosingValidProfiles()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, """
            {
              "Profiles": [
                { "Id": "nova", "Name": "Nova" },
                { "Id": "bad", "Name": "NotACharacter" }
              ]
            }
            """);

        // Act
        var settings = NimvioSettings.Load(SettingsPath);

        // Assert
        var profile = Assert.Single(settings.Profiles);
        Assert.Equal(NimvioCharacterName.Nova, profile.Name);

        // Act
        settings.Save(SettingsPath);

        // Assert
        using var document = JsonDocument.Parse(File.ReadAllText(SettingsPath));
        Assert.Equal("Nova", document.RootElement.GetProperty("Profiles")[0].GetProperty("Name").GetString());
    }

    [Fact]
    public void LoadWhenDuplicateCharacterNamesKeepsFirstProfilePerName()
    {
        // Arrange
        WriteRaw(new NimvioSettings
        {
            Profiles =
            [
                new() { Id = "nova", Name = NimvioCharacterName.Nova, Energy = 11 },
                new() { Id = "mimo", Name = NimvioCharacterName.Mimo },
                new() { Id = "lumi", Name = NimvioCharacterName.Lumi },
                new() { Id = "extra", Name = NimvioCharacterName.Nova, Energy = 99 }
            ]
        });

        // Act
        var settings = NimvioSettings.Load(SettingsPath);

        // Assert
        Assert.Equal(3, settings.Profiles.Count);
        Assert.Equal("nova", settings.Profiles[0].Id);
        Assert.Equal(11, settings.Profiles[0].Energy);
    }

    [Fact]
    public void LoadWhenAllRelationshipsAreNonPositiveClearsFavoriteFriend()
    {
        // Arrange
        WriteRaw(new NimvioSettings
        {
            Profiles =
            [
                new()
                {
                    Id = "nova", Name = NimvioCharacterName.Nova, FavoriteFriendId = "mimo",
                    Relationships = { ["mimo"] = -5, ["lumi"] = 0 }
                },
                new() { Id = "mimo", Name = NimvioCharacterName.Mimo },
                new() { Id = "lumi", Name = NimvioCharacterName.Lumi }
            ]
        });

        // Act
        var nova = NimvioSettings.Load(SettingsPath).Profiles.Single(profile => profile.Id == "nova");

        // Assert
        Assert.Null(nova.FavoriteFriendId);
        Assert.Equal(-5, nova.Relationships["mimo"]);
        Assert.Equal(0, nova.Relationships["lumi"]);
    }

    [Fact]
    public void RemoveProfileMemoryWhenFavoriteRemovedSelectsNextBestRelationship()
    {
        // Arrange
        var settings = new NimvioSettings
        {
            Profiles =
            [
                new() { Id = "nova", Name = NimvioCharacterName.Nova, Relationships = { ["mimo"] = 90, ["lumi"] = 40 }, FavoriteFriendId = "mimo" },
                new() { Id = "mimo", Name = NimvioCharacterName.Mimo },
                new() { Id = "lumi", Name = NimvioCharacterName.Lumi }
            ]
        };

        // Act
        settings.RemoveProfileMemory("mimo");

        // Assert
        var nova = settings.Profiles.Single(profile => profile.Id == "nova");
        Assert.Equal("lumi", nova.FavoriteFriendId);
        Assert.Equal(40, nova.Relationships["lumi"]);
    }

    [Fact]
    public void LoadWhenProfilesArrayIsEmptyAddsDefaultNovaProfile()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, """{ "Profiles": [] }""");

        // Act
        var settings = NimvioSettings.Load(SettingsPath);

        // Assert
        var profile = Assert.Single(settings.Profiles);
        Assert.Equal(NimvioCharacterName.Nova, profile.Name);
    }

    [Fact]
    public void LoadPreservesValidFavoriteFriendWithoutRecalculating()
    {
        // Arrange
        WriteRaw(new NimvioSettings
        {
            Profiles =
            [
                new()
                {
                    Id = "nova", Name = NimvioCharacterName.Nova, FavoriteFriendId = "lumi",
                    Relationships = { ["mimo"] = 10, ["lumi"] = 25 }
                },
                new() { Id = "mimo", Name = NimvioCharacterName.Mimo },
                new() { Id = "lumi", Name = NimvioCharacterName.Lumi }
            ]
        });

        // Act
        var nova = NimvioSettings.Load(SettingsPath).Profiles.Single(profile => profile.Id == "nova");

        // Assert
        Assert.Equal("lumi", nova.FavoriteFriendId);
    }

    [Fact]
    public void LoadDropsRelationshipsToUnknownProfileIds()
    {
        // Arrange
        WriteRaw(new NimvioSettings
        {
            Profiles =
            [
                new()
                {
                    Id = "nova", Name = NimvioCharacterName.Nova,
                    Relationships = { ["mimo"] = 40, ["ghost"] = 80 }
                },
                new() { Id = "mimo", Name = NimvioCharacterName.Mimo }
            ]
        });

        // Act
        var nova = NimvioSettings.Load(SettingsPath).Profiles.Single(profile => profile.Id == "nova");

        // Assert
        Assert.Single(nova.Relationships);
        Assert.Equal(40, nova.Relationships["mimo"]);
    }

    [Fact]
    public void LoadExcludesBlankProfileIdsFromRelationshipGraph()
    {
        // Arrange
        WriteRaw(new NimvioSettings
        {
            Profiles =
            [
                new() { Id = "nova", Name = NimvioCharacterName.Nova, Relationships = { ["mimo"] = 40 } },
                new() { Id = "   ", Name = NimvioCharacterName.Mimo }
            ]
        });

        // Act
        var nova = NimvioSettings.Load(SettingsPath).Profiles.Single(profile => profile.Id == "nova");

        // Assert
        Assert.Empty(nova.Relationships);
    }

    [Fact]
    public void RemoveProfileMemoryWhenNoPositiveRelationshipsRemainClearsFavorite()
    {
        // Arrange
        var settings = new NimvioSettings
        {
            Profiles =
            [
                new() { Id = "nova", Name = NimvioCharacterName.Nova, Relationships = { ["mimo"] = 50 }, FavoriteFriendId = "mimo" },
                new() { Id = "mimo", Name = NimvioCharacterName.Mimo }
            ]
        };

        // Act
        settings.RemoveProfileMemory("mimo");

        // Assert
        var nova = settings.Profiles.Single(profile => profile.Id == "nova");
        Assert.Null(nova.FavoriteFriendId);
        Assert.Empty(nova.Relationships);
    }

    [Fact]
    public void EnabledScreensWhenAllowedListEmptyReturnsAllScreens()
    {
        // Arrange
        var settings = new NimvioSettings { AllowedScreens = [] };

        // Act
        var enabled = settings.EnabledScreens();

        // Assert
        Assert.Equal(Screen.AllScreens.Length, enabled.Length);
    }

    [Fact]
    public void EnabledScreensWhenNoDeviceMatchesFallsBackToAllScreens()
    {
        // Arrange
        var settings = new NimvioSettings { AllowedScreens = [@"\\.\DISPLAY_DOES_NOT_EXIST"] };

        // Act
        var enabled = settings.EnabledScreens();

        // Assert
        Assert.Equal(Screen.AllScreens.Length, enabled.Length);
    }

    [Fact]
    public void EnabledScreensWhenDeviceMatchesFiltersToAllowedScreens()
    {
        // Arrange
        var deviceName = Screen.PrimaryScreen!.DeviceName;
        var settings = new NimvioSettings { AllowedScreens = [deviceName] };

        // Act
        var enabled = settings.EnabledScreens();

        // Assert
        Assert.NotEmpty(enabled);
        Assert.All(enabled, screen => Assert.Equal(deviceName, screen.DeviceName));
    }

    private void WriteRaw(NimvioSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
