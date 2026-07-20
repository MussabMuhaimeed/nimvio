using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormSocialEligibilityTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Fact]
    public void CanStartSocialInteractionWhenIdleAndPlaying()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            form.Opacity = 1;
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);
            NimvioFormTestState.SetSocialInteractionCooldown(form, 0);
            NimvioFormTestState.SetDragging(form, false);
            settings.Playing = true;

            // Act
            // (read CanStartSocialInteraction)

            // Assert
            Assert.True(form.CanStartSocialInteraction);
        });
    }

    [Theory]
    [InlineData(false, 1, 0, 3, false)] // Sitting
    [InlineData(true, 0, 0, 3, false)] // Sitting, hidden
    [InlineData(true, 1, 120, 3, false)] // Sitting, social cooldown
    [InlineData(true, 1, 0, 8, false)] // Sleeping
    [InlineData(true, 1, 0, 24, false)] // WatchingYouTube
    public void CanStartSocialInteractionRequiresPlayableIdleState(
        bool playing,
        double opacity,
        int socialCooldown,
        int behaviorValue,
        bool expected)
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var behavior = (NimvioBehavior)behaviorValue;
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            settings.Playing = playing;
            form.Opacity = opacity;
            NimvioFormTestState.SetBehavior(form, behavior);
            NimvioFormTestState.SetSocialInteractionCooldown(form, socialCooldown);
            NimvioFormTestState.SetDragging(form, false);

            // Act
            // (read CanStartSocialInteraction)

            // Assert
            Assert.Equal(expected, form.CanStartSocialInteraction);
        });
    }

    [Fact]
    public void CanStartSocialInteractionIsFalseWhileDragging()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);
            NimvioFormTestState.SetDragging(form, true);

            // Act
            // (read CanStartSocialInteraction)

            // Assert
            Assert.False(form.CanStartSocialInteraction);
        });
    }

    public void Dispose() => _scenario.Dispose();
}
