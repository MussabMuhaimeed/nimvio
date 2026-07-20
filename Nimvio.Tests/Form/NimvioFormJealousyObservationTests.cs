using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormJealousyObservationTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Fact]
    public void ObserveFriendsInteractionWhenFavoriteIsSocializingBecomesSadAndJealous()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"),
                NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));
            settings.Profiles[2].FavoriteFriendId = "mimo";
            settings.Profiles[2].Happiness = 50;
            var context = _scenario.CreateContext(settings, createInitialForms: true);
            var nova = context.Forms[0];
            var mimo = context.Forms[1];
            var lumi = context.Forms[2];
            mimo.Location = new Point(300, 200);
            lumi.Location = new Point(100, 200);
            NimvioFormTestState.SetBehavior(lumi, NimvioBehavior.Sitting);
            NimvioFormTestState.SetJealousyCooldown(lumi, 0);

            // Act
            lumi.ObserveFriendsInteraction(nova, mimo);

            // Assert
            Assert.Equal(NimvioBehavior.Sad, NimvioFormTestState.GetBehavior(lumi));
            Assert.Equal("What about me?", NimvioFormTestState.GetSpeechText(lumi));
            Assert.Equal(1800, NimvioFormTestState.GetJealousyCooldown(lumi));
            Assert.Equal(47, lumi.Profile.Happiness);
        });
    }

    [Fact]
    public void ObserveFriendsInteractionWhenFavoriteIsNotInvolvedDoesNothing()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"),
                NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));
            settings.Profiles[2].FavoriteFriendId = "mimo";
            var context = _scenario.CreateContext(settings, createInitialForms: true);
            var lumi = context.Forms[2];
            NimvioFormTestState.SetBehavior(lumi, NimvioBehavior.Sitting);
            NimvioFormTestState.SetJealousyCooldown(lumi, 0);

            // Act
            lumi.ObserveFriendsInteraction(context.Forms[0], context.Forms[2]);

            // Assert
            Assert.Equal(NimvioBehavior.Sitting, NimvioFormTestState.GetBehavior(lumi));
            Assert.Null(NimvioFormTestState.GetSpeechText(lumi));
        });
    }

    [Fact]
    public void ObserveFriendsInteractionWhileCooldownActiveIsIgnored()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"),
                NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));
            settings.Profiles[2].FavoriteFriendId = "mimo";
            var context = _scenario.CreateContext(settings, createInitialForms: true);
            var lumi = context.Forms[2];
            NimvioFormTestState.SetBehavior(lumi, NimvioBehavior.Sitting);
            NimvioFormTestState.SetJealousyCooldown(lumi, 500);

            // Act
            lumi.ObserveFriendsInteraction(context.Forms[0], context.Forms[1]);

            // Assert
            Assert.Equal(NimvioBehavior.Sitting, NimvioFormTestState.GetBehavior(lumi));
        });
    }

    [Fact]
    public void ObserveFriendsInteractionWithoutFavoriteFriendDoesNothing()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"),
                NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));
            var context = _scenario.CreateContext(settings, createInitialForms: true);
            var lumi = context.Forms[2];
            NimvioFormTestState.SetBehavior(lumi, NimvioBehavior.Thinking);

            // Act
            lumi.ObserveFriendsInteraction(context.Forms[0], context.Forms[1]);

            // Assert
            Assert.Equal(NimvioBehavior.Thinking, NimvioFormTestState.GetBehavior(lumi));
        });
    }

    public void Dispose() => _scenario.Dispose();
}
