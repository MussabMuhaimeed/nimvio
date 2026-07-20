using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormSocialRollTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Theory]
    [InlineData(40, 0)]
    [InlineData(40, 37)]
    [InlineData(-25, 10)]
    [InlineData(0, 55)]
    public void StartSocialInteractionPicksInteractionFromRelationshipAndRandom(int relationship, int seed)
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            settings.Profiles[0].Relationships["mimo"] = relationship;
            settings.Profiles[0].Personality = NimvioPersonality.Playful;
            settings.Profiles[1].Personality = NimvioPersonality.Calm;
            var context = _scenario.CreateContext(settings, createInitialForms: true);
            var nova = context.Forms[0];
            var mimo = context.Forms[1];
            nova.Location = new Point(200, 200);
            mimo.Location = new Point(320, 200);
            NimvioFormTestState.SetRandom(nova, seed);
            NimvioFormTestState.SetBehavior(nova, NimvioBehavior.Sitting);
            NimvioFormTestState.SetBehavior(mimo, NimvioBehavior.Sitting);
            NimvioFormTestState.SetSocialInteractionCooldown(nova, 0);
            NimvioFormTestState.SetSocialInteractionCooldown(mimo, 0);

            // Act
            NimvioFormTestState.InvokeStartSocialInteraction(nova, mimo);

            // Assert
            Assert.True(NimvioFormTestState.GetBehavior(nova) is NimvioBehavior.Hugging
                or NimvioBehavior.SharingMilk or NimvioBehavior.PlayingTogether
                or NimvioBehavior.Competing or NimvioBehavior.Arguing);
        });
    }

    [Fact]
    public void BeginWindowWaveWhenSafelyPerchedWavesAtFriend()
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
            nova.Location = new Point(100, 100);
            mimo.Location = new Point(260, 100);
            NimvioFormTestState.SetPerchedWindow(nova, new IntPtr(50));
            NimvioFormTestState.SetBehavior(nova, NimvioBehavior.Sitting);

            // Act
            NimvioFormTestState.InvokeBeginWindowWave(nova, mimo);

            // Assert
            Assert.Equal(NimvioBehavior.Waving, NimvioFormTestState.GetBehavior(nova));
        });
    }

    [Fact]
    public void SocialTickFindsNearbyFriendAndStartsInteraction()
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
            nova.Location = new Point(300, 300);
            mimo.Location = new Point(360, 300);
            NimvioFormTestState.SetRandom(nova, 4);
            NimvioFormTestState.SetSocialTicks(nova, 1);
            NimvioFormTestState.SetBehavior(nova, NimvioBehavior.Sitting);
            NimvioFormTestState.SetBehavior(mimo, NimvioBehavior.Sitting);
            NimvioFormTestState.SetSocialInteractionCooldown(nova, 0);
            NimvioFormTestState.SetSocialInteractionCooldown(mimo, 0);

            // Act
            NimvioFormTestState.Tick(nova);

            // Assert
            Assert.True(NimvioFormTestState.GetBehavior(nova) is NimvioBehavior.Hugging
                or NimvioBehavior.SharingMilk or NimvioBehavior.PlayingTogether
                or NimvioBehavior.Competing or NimvioBehavior.Arguing);
        });
    }

    public void Dispose() => _scenario.Dispose();
}
