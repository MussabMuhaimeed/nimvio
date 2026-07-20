using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormSocialInteractionTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Fact]
    public void BeginSocialInteractionHuggingStrengthensRelationshipAndSocializesMind()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            settings.Profiles[0].Happiness = 40;
            settings.Profiles[0].Boredom = 40;
            var context = _scenario.CreateContext(settings, createInitialForms: true);
            var nova = context.Forms[0];
            var mimo = context.Forms[1];
            nova.Location = new Point(100, 100);
            mimo.Location = new Point(220, 100);

            // Act
            nova.BeginSocialInteraction(mimo, NimvioBehavior.Hugging, mirrored: false);

            // Assert
            Assert.Equal(NimvioBehavior.Hugging, NimvioFormTestState.GetBehavior(nova));
            Assert.Equal(5, nova.Profile.Relationships["mimo"]);
            Assert.Equal(43, nova.Profile.Happiness);
            Assert.Equal(35, nova.Profile.Boredom);
            Assert.Equal(1200, NimvioFormTestState.GetSocialInteractionCooldown(nova));
        });
    }

    [Fact]
    public void BeginSocialInteractionHuggingAfterConflictUsesLargerRepairBonus()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            settings.Profiles[0].Relationships["mimo"] = -15;
            var context = _scenario.CreateContext(settings, createInitialForms: true);
            var nova = context.Forms[0];
            var mimo = context.Forms[1];

            // Act
            nova.BeginSocialInteraction(mimo, NimvioBehavior.Hugging, mirrored: false);

            // Assert
            Assert.Equal(-6, nova.Profile.Relationships["mimo"]);
            Assert.Equal("Friends again?", NimvioFormTestState.GetSpeechText(nova));
        });
    }

    [Fact]
    public void BeginSocialInteractionArguingWeakensRelationship()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));
            settings.Profiles[0].Relationships["lumi"] = 10;
            var context = _scenario.CreateContext(settings, createInitialForms: true);

            // Act
            context.Forms[0].BeginSocialInteraction(context.Forms[1], NimvioBehavior.Arguing, mirrored: false);

            // Assert
            Assert.Equal(7, context.Forms[0].Profile.Relationships["lumi"]);
        });
    }

    [Fact]
    public void BeginSocialInteractionStrongBondSetsFavoriteFriend()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            settings.Profiles[0].Relationships["mimo"] = 24;
            var context = _scenario.CreateContext(settings, createInitialForms: true);

            // Act
            context.Forms[0].BeginSocialInteraction(context.Forms[1], NimvioBehavior.Hugging, mirrored: false);

            // Assert
            Assert.Equal("mimo", context.Forms[0].Profile.FavoriteFriendId);
        });
    }

    [Fact]
    public void BeginSocialInteractionCompetingOffsetsTargetBasedOnMirroring()
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
            nova.Location = new Point(300, 200);
            mimo.Location = new Point(420, 200);

            // Act
            nova.BeginSocialInteraction(mimo, NimvioBehavior.Competing, mirrored: false);
            var forwardTarget = NimvioFormTestState.GetTarget(nova);

            nova.BeginSocialInteraction(mimo, NimvioBehavior.Competing, mirrored: true);
            var mirroredTarget = NimvioFormTestState.GetTarget(nova);

            // Assert
            Assert.True(forwardTarget.X > nova.WorldCenter.X);
            Assert.True(mirroredTarget.X < nova.WorldCenter.X);
            Assert.Equal(NimvioBehavior.Competing, NimvioFormTestState.GetBehavior(nova));
        });
    }

    public void Dispose() => _scenario.Dispose();
}
