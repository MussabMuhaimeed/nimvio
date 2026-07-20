using System.Reflection;
using Nimvio;
using Xunit;

namespace Nimvio.Tests.Application;

public sealed class NimvioApplicationContextTests : IDisposable
{
    private readonly string _settingsPath;
    private readonly List<NimvioApplicationContext> _contexts = [];

    public NimvioApplicationContextTests()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nimvio-application-context-tests", Guid.NewGuid().ToString("N"));
        _settingsPath = Path.Combine(directory, "settings.json");
    }

    [Fact]
    public void CreateAddCharacterMenuListsOnlyMissingCharacters()
    {
        // Arrange
        var settings = SettingsWithProfiles(NimvioCharacterName.Nova, NimvioCharacterName.Mimo);
        RunSta(() =>
        {
            // Arrange
            var context = CreateContext(settings, createInitialForms: false);

            // Act
            var menu = context.CreateAddCharacterMenu("Add character");

            // Assert
            var item = Assert.Single(menu.DropDownItems.Cast<ToolStripItem>());
            Assert.Equal("Lumi", item.Text);
        });
    }

    [Fact]
    public void CreateAddCharacterMenuWhenAllCharactersPresentShowsDisabledPlaceholder()
    {
        // Arrange
        var settings = SettingsWithProfiles(NimvioCharacterName.Nova, NimvioCharacterName.Mimo, NimvioCharacterName.Lumi);
        RunSta(() =>
        {
            // Arrange
            var context = CreateContext(settings, createInitialForms: false);

            // Act
            var menu = context.CreateAddCharacterMenu("Add character");

            // Assert
            var item = Assert.Single(menu.DropDownItems.Cast<ToolStripItem>());
            Assert.Equal("All characters are already added", item.Text);
            Assert.False(item.Enabled);
        });
    }

    [Fact]
    public void AddCompanionAddsProfileWithExpectedPersonalityAndPersists()
    {
        // Arrange
        var settings = SettingsWithProfiles(NimvioCharacterName.Nova);
        RunSta(() =>
        {
            // Arrange
            var context = CreateContext(settings, createInitialForms: false);

            // Act
            context.AddCompanion(NimvioCharacterName.Mimo);

            // Assert
            var mimo = Assert.Single(context.Settings.Profiles, profile => profile.Name == NimvioCharacterName.Mimo);
            Assert.Equal(NimvioPersonality.Playful, mimo.Personality);
            Assert.Equal(Color.FromArgb(255, 176, 76).ToArgb(), mimo.AccentArgb);
            Assert.Single(context.Forms);
            var reloaded = NimvioSettings.Load(_settingsPath);
            Assert.Contains(reloaded.Profiles, profile => profile.Name == NimvioCharacterName.Mimo);
        });
    }

    [Fact]
    public void AddCompanionAssignsCalmPersonalityToLumi()
    {
        // Arrange
        var settings = SettingsWithProfiles(NimvioCharacterName.Nova);
        RunSta(() =>
        {
            // Arrange
            var context = CreateContext(settings, createInitialForms: false);

            // Act
            context.AddCompanion(NimvioCharacterName.Lumi);

            // Assert
            var lumi = Assert.Single(context.Settings.Profiles, profile => profile.Name == NimvioCharacterName.Lumi);
            Assert.Equal(NimvioPersonality.Calm, lumi.Personality);
            Assert.Equal(Color.FromArgb(211, 126, 255).ToArgb(), lumi.AccentArgb);
        });
    }

    [Fact]
    public void AddCompanionWhenCharacterAlreadyActiveDoesNotAddDuplicateProfile()
    {
        // Arrange
        var settings = SettingsWithProfiles(NimvioCharacterName.Nova);
        RunSta(() =>
        {
            // Arrange
            var context = CreateContext(settings, createInitialForms: false);
            var profileCount = context.Settings.Profiles.Count;

            // Act
            context.AddCompanion(NimvioCharacterName.Nova);

            // Assert
            Assert.Equal(profileCount, context.Settings.Profiles.Count);
            Assert.Empty(context.Forms);
        });
    }

    [Fact]
    public void AddCompanionWhenThreeFormsAlreadyOpenDoesNotAddAnother()
    {
        // Arrange
        var settings = SettingsWithProfiles(NimvioCharacterName.Nova, NimvioCharacterName.Mimo, NimvioCharacterName.Lumi);
        RunSta(() =>
        {
            // Arrange
            var context = CreateContext(settings, createInitialForms: true);

            // Act
            context.AddCompanion(NimvioCharacterName.Nova);

            // Assert
            Assert.Equal(3, context.Forms.Count);
            Assert.Equal(3, context.Settings.Profiles.Count);
        });
    }

    [Fact]
    public void FindNearbyFormReturnsClosestWithinDistance()
    {
        // Arrange
        var settings = SettingsWithProfiles(NimvioCharacterName.Nova, NimvioCharacterName.Mimo, NimvioCharacterName.Lumi);
        RunSta(() =>
        {
            // Arrange
            var context = CreateContext(settings, createInitialForms: true);
            var source = context.Forms[0];
            var near = context.Forms[1];
            var far = context.Forms[2];
            PlaceForm(source, 0, 0);
            PlaceForm(near, 40, 0);
            PlaceForm(far, 400, 0);

            // Act
            var found = context.FindNearbyForm(source, 100);

            // Assert
            Assert.Same(near, found);
        });
    }

    [Fact]
    public void FindNearbyFormIgnoresSourceAndDisposedForms()
    {
        // Arrange
        var settings = SettingsWithProfiles(NimvioCharacterName.Nova, NimvioCharacterName.Mimo);
        RunSta(() =>
        {
            // Arrange
            var context = CreateContext(settings, createInitialForms: true);
            var source = context.Forms[0];
            var other = context.Forms[1];
            PlaceForm(source, 0, 0);
            PlaceForm(other, 30, 0);
            other.Close();

            // Act
            var found = context.FindNearbyForm(source, 100);

            // Assert
            Assert.Null(found);
        });
    }

    [Fact]
    public void FindPerchedFriendPrefersClosestEligiblePerchedCompanion()
    {
        // Arrange
        var settings = SettingsWithProfiles(NimvioCharacterName.Nova, NimvioCharacterName.Mimo, NimvioCharacterName.Lumi);
        RunSta(() =>
        {
            // Arrange
            var context = CreateContext(settings, createInitialForms: true);
            var source = context.Forms[0];
            var nearPerched = context.Forms[1];
            var farPerched = context.Forms[2];
            PlaceForm(source, 0, 0);
            PlaceForm(nearPerched, 50, 0);
            PlaceForm(farPerched, 200, 0);
            SetPerchedWindow(source, new IntPtr(100));
            SetPerchedWindow(nearPerched, new IntPtr(200));
            SetPerchedWindow(farPerched, new IntPtr(300));

            // Act
            var found = context.FindPerchedFriend(source, 120);

            // Assert
            Assert.Same(nearPerched, found);
        });
    }

    [Fact]
    public void FindPerchedFriendSkipsFormsPerchedOnSameWindowAsSource()
    {
        // Arrange
        var settings = SettingsWithProfiles(NimvioCharacterName.Nova, NimvioCharacterName.Mimo);
        RunSta(() =>
        {
            // Arrange
            var context = CreateContext(settings, createInitialForms: true);
            var source = context.Forms[0];
            var sameWindow = context.Forms[1];
            PlaceForm(source, 0, 0);
            PlaceForm(sameWindow, 40, 0);
            SetPerchedWindow(source, new IntPtr(500));
            SetPerchedWindow(sameWindow, new IntPtr(500));

            // Act
            var found = context.FindPerchedFriend(source, 100);

            // Assert
            Assert.Null(found);
        });
    }

    [Fact]
    public void RemoveFormWhenMultipleCharactersRemovesProfileAndMemory()
    {
        // Arrange
        var settings = SettingsWithProfiles(
            new NimvioProfile { Id = "nova", Name = NimvioCharacterName.Nova, Relationships = { ["mimo"] = 40 } },
            new NimvioProfile { Id = "mimo", Name = NimvioCharacterName.Mimo });
        RunSta(() =>
        {
            // Arrange
            var context = CreateContext(settings, createInitialForms: true);
            var mimoForm = context.Forms.Single(form => form.Profile.Name == NimvioCharacterName.Mimo);

            // Act
            context.RemoveForm(mimoForm);

            // Assert
            Assert.DoesNotContain(context.Settings.Profiles, profile => profile.Id == "mimo");
            Assert.Empty(context.Settings.Profiles.Single(profile => profile.Id == "nova").Relationships);
            Assert.Single(context.Forms);
            var reloaded = NimvioSettings.Load(_settingsPath);
            Assert.DoesNotContain(reloaded.Profiles, profile => profile.Id == "mimo");
        });
    }

    [Fact]
    public void ArrangeYouTubeWatchingAndNotifySocialInteractionDoNotThrowWithMultipleForms()
    {
        // Arrange
        var settings = SettingsWithProfiles(NimvioCharacterName.Nova, NimvioCharacterName.Mimo, NimvioCharacterName.Lumi);
        var window = new WindowSnapshot(new IntPtr(42), new Rectangle(10, 10, 400, 300), "chrome", "Video");
        RunSta(() =>
        {
            // Arrange
            var context = CreateContext(settings, createInitialForms: true);
            var first = context.Forms[0];
            var second = context.Forms[1];

            // Act
            context.ArrangeYouTubeWatching(window);
            context.NotifySocialInteraction(first, second);

            // Assert
        });
    }

    [Fact]
    public void SavePersistsCurrentSettings()
    {
        // Arrange
        var settings = SettingsWithProfiles(NimvioCharacterName.Nova);
        settings.Speed = 2.25f;
        RunSta(() =>
        {
            // Arrange
            var context = CreateContext(settings, createInitialForms: false);

            // Act
            context.Save();

            // Assert
            Assert.Equal(2.25f, NimvioSettings.Load(_settingsPath).Speed);
        });
    }

    [Fact]
    public void ConstructorCreatesFormForEachProfileUpToThree()
    {
        // Arrange
        var settings = SettingsWithProfiles(NimvioCharacterName.Nova, NimvioCharacterName.Mimo, NimvioCharacterName.Lumi);
        RunSta(() =>
        {
            // Act
            var context = CreateContext(settings, createInitialForms: true);

            // Assert
            Assert.Equal(3, context.Forms.Count);
        });
    }

    private NimvioApplicationContext CreateContext(NimvioSettings settings, bool createInitialForms)
    {
        settings.PersistenceFilePath = _settingsPath;
        var context = new NimvioApplicationContext(settings, startBackgroundServices: false, createInitialForms);
        _contexts.Add(context);
        return context;
    }

    private static NimvioSettings SettingsWithProfiles(params NimvioCharacterName[] names)
        => SettingsWithProfiles(names.Select(name => new NimvioProfile { Name = name }).ToArray());

    private static NimvioSettings SettingsWithProfiles(params NimvioProfile[] profiles)
        => new() { Profiles = profiles.ToList() };

    private static void PlaceForm(NimvioForm form, int x, int y)
    {
        form.Location = new Point(x, y);
        form.Refresh();
    }

    private static void SetPerchedWindow(NimvioForm form, IntPtr handle)
    {
        typeof(NimvioForm).GetField("_perchedWindow", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(form, handle);
    }

    /*=========================================================================
    ** Constants for thread apartment states.
    =========================================================================*/
    private static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(30));
        if (error is not null)
        {
            throw error;
        }
    }

    public void Dispose()
    {
        RunSta(() =>
        {
            foreach (var context in _contexts)
            {
                context.ShutdownForTests();
            }
        });

        var directory = Path.GetDirectoryName(_settingsPath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }
}
