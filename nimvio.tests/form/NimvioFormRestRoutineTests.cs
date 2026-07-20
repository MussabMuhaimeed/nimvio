using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormRestRoutineTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Fact]
    public void TickAdvanceStationaryBehaviorProgressesHangingHoldingHappyAndSurprised()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetPerchedWindow(form, new IntPtr(12));

            // Arrange
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Hanging);
            NimvioFormTestState.SetBehaviorTicks(form, 1);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Falling, NimvioFormTestState.GetBehavior(form));

            // Arrange
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.HoldingCursor);
            NimvioFormTestState.SetBehaviorTicks(form, 1);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Happy, NimvioFormTestState.GetBehavior(form));

            // Arrange
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Happy);
            NimvioFormTestState.SetBehaviorTicks(form, 1);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Sitting, NimvioFormTestState.GetBehavior(form));

            // Arrange
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Stumbling);
            NimvioFormTestState.SetBehaviorTicks(form, 1);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Surprised, NimvioFormTestState.GetBehavior(form));

            // Arrange
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Surprised);
            NimvioFormTestState.SetBehaviorTicks(form, 1);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Searching, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Theory]
    [InlineData(0, 0)] // Curious
    [InlineData(0, 50)]
    [InlineData(2, 10)] // Playful
    [InlineData(2, 90)]
    [InlineData(1, 5)] // Calm
    [InlineData(1, 94)]
    public void TickContinueRestRoutineExploresPersonalityRolls(int personalityValue, int randomSeed)
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var personality = (NimvioPersonality)personalityValue;
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova");
            profile.Personality = personality;
            profile.Energy = 80;
            profile.Boredom = 20;
            profile.Curiosity = 20;
            var settings = NimvioFormScenario.SettingsWith(profile);
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetRandom(form, randomSeed);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);
            NimvioFormTestState.SetBehaviorTicks(form, 1);
            NimvioFormTestState.SetRestActionsRemaining(form, 3);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.NotEqual(NimvioBehavior.Sitting, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void TickContinueRestRoutineLowEnergySleeps()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo");
            profile.Energy = 10;
            var settings = NimvioFormScenario.SettingsWith(profile);
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetLastCursorPosition(form, Cursor.Position);
            NimvioFormTestState.SetCursorInteractionCooldown(form, 999);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Waving);
            NimvioFormTestState.SetBehaviorTicks(form, 1);
            NimvioFormTestState.SetRestActionsRemaining(form, 3);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Sleeping, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void TickContinueRestRoutineHighBoredomNearCursorChases()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo");
            profile.Energy = 80;
            profile.Boredom = 75;
            profile.Curiosity = 20;
            var settings = NimvioFormScenario.SettingsWith(profile);
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var cursor = Cursor.Position;
            form.Location = new Point(cursor.X - form.Width / 2, cursor.Y - form.Height / 2);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Waving);
            NimvioFormTestState.SetBehaviorTicks(form, 1);
            NimvioFormTestState.SetRestActionsRemaining(form, 3);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.ChasingCursor, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void TickContinueRestRoutineHighCuriosityInspects()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo");
            profile.Energy = 80;
            profile.Boredom = 20;
            profile.Curiosity = 75;
            var settings = NimvioFormScenario.SettingsWith(profile);
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Waving);
            NimvioFormTestState.SetBehaviorTicks(form, 1);
            NimvioFormTestState.SetRestActionsRemaining(form, 3);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Inspecting, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void TickExhaustedRestActionsStartsSearchingAgain()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetRandom(form, 11);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Feeding);
            NimvioFormTestState.SetBehaviorTicks(form, 1);
            NimvioFormTestState.SetRestActionsRemaining(form, 0);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Searching, NimvioFormTestState.GetBehavior(form));
        });
    }

    public void Dispose() => _scenario.Dispose();
}
