using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormSystemStateTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    public NimvioFormSystemStateTests()
    {
        DesktopAwareness.ClearTestOverrides();
    }

    [Fact]
    public void TickCheckSystemStateMarksIgnoredUsersAndReturnsFromAway()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova");
            profile.LastInteractionUtc = DateTime.UtcNow.AddMinutes(-30);
            var settings = NimvioFormScenario.SettingsWith(profile);
            DesktopAwareness.UserIdleTimeOverride = () => TimeSpan.FromSeconds(1);
            DesktopAwareness.ActiveWindowOverride = () => null;
            DesktopAwareness.VisibleWindowHandlesOverride = () => [];
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetSystemCheckTicks(form, 59);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Sad, NimvioFormTestState.GetBehavior(form));

            // Arrange
            NimvioFormTestState.SetUserWasAway(form, true);
            NimvioFormTestState.SetSystemCheckTicks(form, 59);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Happy, NimvioFormTestState.GetBehavior(form));
            Assert.Equal("You're back!", NimvioFormTestState.GetSpeechText(form));
        });
    }

    [Fact]
    public void TickTrackPerchedWindowWhenHandleMissingStartsFallOrSearch()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetPerchedWindow(form, new IntPtr(999999));
            NimvioFormTestState.SetPerchedWindowBounds(form, new Rectangle(100, 100, 400, 300));
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Falling, NimvioFormTestState.GetBehavior(form));

            // Arrange
            NimvioFormTestState.SetPerchedWindow(form, new IntPtr(999998));
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Walking);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Searching, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void TickWhilePausedSkipsAutonomyButStillUpdatesMind()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));
            settings.Playing = false;
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var energyBefore = form.Profile.Energy;
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Walking);
            NimvioFormTestState.SetTarget(form, new PointF(form.Left + 300, form.Top));

            // Act
            NimvioFormTestState.Tick(form, 120);

            // Assert
            Assert.Equal(NimvioBehavior.Walking, NimvioFormTestState.GetBehavior(form));
            Assert.NotEqual(energyBefore, form.Profile.Energy);
        });
    }

    [Fact]
    public void TickLongSimulationExercisesRareEventsAndWindowHelpers()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova");
            profile.Personality = NimvioPersonality.Playful;
            var settings = NimvioFormScenario.SettingsWith(profile);
            settings.Autonomy = AutonomyLevel.High;
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var screen = Screen.PrimaryScreen!;
            form.Location = new Point(
                screen.WorkingArea.Left + 40,
                screen.WorkingArea.Bottom - form.Height - 8);
            NimvioFormTestState.SetRandom(form, 42);
            NimvioFormTestState.SetRareEventCooldown(form, 0);
            NimvioFormTestState.SetWindowInteractionCooldown(form, 0);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);
            NimvioFormTestState.SetPerchedWindow(form, IntPtr.Zero);
            _ = NimvioFormTestState.InvokeCanHideAtSafeEdge(form);
            _ = NimvioFormTestState.InvokeTryHopToNearbyWindow(form);

            // Act
            NimvioFormTestState.Tick(form, 400);

            // Assert
        });
    }

    public void Dispose()
    {
        DesktopAwareness.ClearTestOverrides();
        _scenario.Dispose();
    }
}
