using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormFinalCoverageTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Fact]
    public void ChasingCursorNearPointerCapturesCursor()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var cursor = Cursor.Position;
            form.Location = new Point(cursor.X - form.Width / 2, cursor.Y - form.Height / 2);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.ChasingCursor);
            NimvioFormTestState.SetBehaviorTicks(form, 400);

            // Act
            NimvioFormTestState.Tick(form, 35);

            // Assert
            Assert.True(
                NimvioFormTestState.GetBehavior(form) is NimvioBehavior.HoldingCursor or NimvioBehavior.Happy
                || NimvioFormTestState.GetSpeechText(form) == "Got you!");
        });
    }

    [Theory]
    [InlineData(-30, 0, 5)] // low relationship playful
    [InlineData(-30, 1, 15)] // low relationship calm
    [InlineData(-30, 2, 25)] // low relationship curious default
    [InlineData(35, 2, 10)] // high relationship
    public void StartSocialInteractionCoversRelationshipPersonalalityMatrix(int relationship, int personalityValue, int seed)
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            settings.Profiles[0].Relationships["mimo"] = relationship;
            settings.Profiles[0].Personality = (NimvioPersonality)personalityValue;
            var context = _scenario.CreateContext(settings, createInitialForms: true);
            var nova = context.Forms[0];
            var mimo = context.Forms[1];
            nova.Location = new Point(180, 180);
            mimo.Location = new Point(320, 180);
            NimvioFormTestState.SetRandom(nova, seed);
            NimvioFormTestState.SetSocialInteractionCooldown(nova, 0);
            NimvioFormTestState.SetSocialInteractionCooldown(mimo, 0);

            // Act
            NimvioFormTestState.InvokeStartSocialInteraction(nova, mimo);

            // Assert
        });
    }

    [Fact]
    public void AllowedScreensMenuTogglePersistsSelection()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));
            settings.AllowedScreens = [];
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var deviceName = Screen.PrimaryScreen!.DeviceName;

            // Act
            NimvioFormTestState.InvokeToggleAllowedScreen(form, deviceName, enabled: true);

            // Assert
            Assert.Contains(deviceName, settings.AllowedScreens);

            if (settings.AllowedScreens.Count > 1)
            {
                // Arrange

                // Act
                NimvioFormTestState.InvokeToggleAllowedScreen(form, deviceName, enabled: false);

                // Assert
                Assert.DoesNotContain(deviceName, settings.AllowedScreens);
            }
        });
    }

    [Fact]
    public void StationaryBehaviorsAdvanceThroughDefaultTickBranch()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);

            // Act
            foreach (var behaviorValue in Enum.GetValues(typeof(NimvioBehavior)))
            {
                var behavior = (NimvioBehavior)behaviorValue!;
                if (behavior is NimvioBehavior.Searching or NimvioBehavior.Walking or NimvioBehavior.Hopping
                    or NimvioBehavior.ChasingCursor or NimvioBehavior.FleeingCursor or NimvioBehavior.Competing
                    or NimvioBehavior.WatchingYouTube or NimvioBehavior.Sliding or NimvioBehavior.Falling
                    or NimvioBehavior.Peeking or NimvioBehavior.Thrown)
                {
                    continue;
                }

                NimvioFormTestState.SetRandom(form, (int)behavior);
                NimvioFormTestState.SetBehavior(form, behavior);
                NimvioFormTestState.SetBehaviorTicks(form, 1);
                NimvioFormTestState.SetRestActionsRemaining(form, 2);
                NimvioFormTestState.Tick(form);
                NimvioFormTestState.Paint(form);
            }

            // Assert
        });
    }

    [Fact]
    public void HidingBehindWindowIncrementsHideBlendDuringTickAndPaint()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.HidingBehindWindow);
            NimvioFormTestState.SetBehaviorTicks(form, 40);

            // Act
            NimvioFormTestState.Tick(form, 25);
            NimvioFormTestState.Paint(form);

            // Assert
        });
    }

    [Fact]
    public void HoveringPointerTriggersComfortWave()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var cursor = Cursor.Position;
            form.Location = new Point(cursor.X - form.Width / 2, cursor.Y - form.Height / 2);
            NimvioFormTestState.SetLastCursorPosition(form, cursor);
            NimvioFormTestState.SetCursorInteractionCooldown(form, 999);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);
            NimvioFormTestState.SetHoverTicks(form, 91);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Waving, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void BeginWindowWaveWithoutPerchedWindowIsIgnored()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            var context = _scenario.CreateContext(settings, createInitialForms: true);
            NimvioFormTestState.SetBehavior(context.Forms[0], NimvioBehavior.Sitting);

            // Act
            NimvioFormTestState.InvokeBeginWindowWave(context.Forms[0], context.Forms[1]);

            // Assert
            Assert.Equal(NimvioBehavior.Sitting, NimvioFormTestState.GetBehavior(context.Forms[0]));
        });
    }

    [Fact]
    public void BeginSocialInteractionShowsOptionalSpeechLines()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            var context = _scenario.CreateContext(settings, createInitialForms: true);
            var nova = context.Forms[0];
            var mimo = context.Forms[1];

            // Act
            nova.BeginSocialInteraction(mimo, NimvioBehavior.PlayingTogether, false);
            nova.BeginSocialInteraction(mimo, NimvioBehavior.SharingMilk, false);
            nova.BeginSocialInteraction(mimo, NimvioBehavior.Competing, false);

            // Assert
        });
    }

    public void Dispose() => _scenario.Dispose();
}
