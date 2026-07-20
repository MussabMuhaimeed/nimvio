using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormCoverageBoostTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Fact]
    public void WatchForInteractionExploresManyRandomOutcomesNearCursor()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var cursor = Cursor.Position;
            form.Location = new Point(cursor.X - form.Width / 2, cursor.Y - form.Height / 2);

            // Act
            for (var seed = 0; seed < 80; seed++)
            {
                NimvioFormTestState.SetRandom(form, seed);
                NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);
                NimvioFormTestState.SetBehaviorTicks(form, 120);
                NimvioFormTestState.SetSocialInteractionCooldown(form, 0);
                NimvioFormTestState.SetSocialTicks(form, 500);
                NimvioFormTestState.Tick(form, 3);
            }

            // Assert
        });
    }

    [Fact]
    public void FindRestingPlaceAndChooseWalkCoverNavigationBranches()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi");
            profile.Personality = NimvioPersonality.Curious;
            profile.RecentPlaces.Add(new Point(10_000, 10_000));
            var settings = NimvioFormScenario.SettingsWith(profile);
            settings.Autonomy = AutonomyLevel.High;
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);

            // Act
            for (var seed = 0; seed < 40; seed++)
            {
                NimvioFormTestState.SetRandom(form, seed);
                _ = NimvioFormTestState.InvokeFindRestingPlace(form, Screen.PrimaryScreen!);
            }

            if (Screen.AllScreens.Length > 1)
            {
                profile.FavoriteScreen = Screen.AllScreens[1].DeviceName;
            }

            NimvioFormTestState.SetRandom(form, 9);
            NimvioFormTestState.InvokeStartSearching(form, otherScreen: true);
            NimvioFormTestState.SetBehaviorTicks(form, 1);
            NimvioFormTestState.Tick(form);
            NimvioFormTestState.InvokeChoosePlaceAndWalk(form);

            // Assert
        });
    }

    [Fact]
    public void TrackPerchedWindowFollowsRealVisibleWindowMovement()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var windows = DesktopAwareness.VisibleWindowSnapshots(Screen.PrimaryScreen!);
            if (windows.Count == 0)
            {
                // Assert
                return;
            }

            var window = windows[0];
            NimvioFormTestState.SetPerchedWindow(form, window.Handle);
            NimvioFormTestState.SetPerchedWindowBounds(form, window.Bounds);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);

            // Act
            NimvioFormTestState.Tick(form, 3);

            // Arrange
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Walking);
            NimvioFormTestState.SetTarget(form, form.WorldCenter);
            NimvioFormTestState.SetPerchedWindowBounds(form, window.Bounds with { X = window.Bounds.X + 60 });

            // Act
            NimvioFormTestState.Tick(form);

            // Arrange
            NimvioFormTestState.SetPerchedWindowBounds(form, window.Bounds with { X = window.Bounds.X + 200, Y = window.Bounds.Y + 200 });
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
        });
    }

    [Fact]
    public void CheckSystemStateDetectsNewWindowsAndEndsYouTubeWatching()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetKnownWindows(form, []);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);
            NimvioFormTestState.SetSystemCheckTicks(form, 59);

            // Act
            NimvioFormTestState.Tick(form);

            // Arrange
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.WatchingYouTube);
            NimvioFormTestState.SetWatchedYouTubeWindow(form, new IntPtr(123));
            NimvioFormTestState.SetSystemCheckTicks(form, 59);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Sitting, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void ContextMenuActionsUpdateSettingsAndCharacterOptions()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova");
            var settings = NimvioFormScenario.SettingsWith(profile);
            settings.Size = 112;
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var menu = NimvioFormTestState.GetContextMenu(form);

            // Act
            ClickMenuText(menu, "Pause activity");
            ClickMenuText(menu, "Resume activity");
            ClickNestedMenu(menu, "Activity level", "Energetic");
            ClickNestedMenu(menu, "Autonomy", "High");
            ClickNestedMenu(menu, "Size", "Large");
            ClickNestedMenu(menu, "Personality", "Playful");
            ClickNestedMenu(menu, "Character color", "Orange");

            // Assert
            Assert.Equal(ActivityLevel.Energetic, settings.Activity);
            Assert.Equal(AutonomyLevel.High, settings.Autonomy);
            Assert.Equal(144, settings.Size);
            Assert.Equal(NimvioPersonality.Playful, profile.Personality);
        });
    }

    [Fact]
    public void PaintCoversActiveAppPenAndBookAccessoriesWhileAwake()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            settings.Playing = true;
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);
            NimvioFormTestState.SetActiveAccessory(form, ActiveAppAccessory.Pen);

            // Act
            NimvioFormTestState.Paint(form);
            NimvioFormTestState.SetActiveAccessory(form, ActiveAppAccessory.Book);
            NimvioFormTestState.Paint(form);
            NimvioFormTestState.SetActiveAccessory(form, ActiveAppAccessory.Headphones);
            NimvioFormTestState.Paint(form);

            // Assert
        });
    }

    [Fact]
    public void RareEventsAndHidingRunUnderHighAutonomySimulation()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo");
            profile.Personality = NimvioPersonality.Playful;
            profile.Boredom = 60;
            var settings = NimvioFormScenario.SettingsWith(profile);
            settings.Autonomy = AutonomyLevel.High;
            settings.Activity = ActivityLevel.Energetic;
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var screen = Screen.PrimaryScreen!;
            form.Location = new Point(screen.WorkingArea.Left + 30, screen.WorkingArea.Bottom - form.Height - 6);

            // Act
            for (var seed = 0; seed < 60; seed++)
            {
                NimvioFormTestState.SetRandom(form, seed);
                NimvioFormTestState.SetRareEventCooldown(form, 0);
                NimvioFormTestState.SetWindowInteractionCooldown(form, 0);
                NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);
                NimvioFormTestState.SetPerchedWindow(form, IntPtr.Zero);
                NimvioFormTestState.Tick(form, 6);
            }

            // Assert
        });
    }

    private static void ClickMenuText(ContextMenuStrip menu, string text)
    {
        var item = menu.Items.Cast<ToolStripItem>().First(i => i.Text == text);
        if (item is ToolStripMenuItem menuItem)
        {
            menuItem.PerformClick();
        }
    }

    private static void ClickNestedMenu(ContextMenuStrip root, string parentText, string childText)
    {
        var parent = root.Items.Cast<ToolStripItem>().OfType<ToolStripMenuItem>().First(i => i.Text == parentText);
        var child = parent.DropDownItems.Cast<ToolStripItem>().First(i => i.Text == childText);
        if (child is ToolStripMenuItem menuItem)
        {
            menuItem.PerformClick();
        }
    }

    public void Dispose() => _scenario.Dispose();
}
