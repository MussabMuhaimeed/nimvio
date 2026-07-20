using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormDragAndMenuTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Fact]
    public void DragClickWithoutMovementWavesAndComforts()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var happinessBefore = form.Profile.Happiness;

            // Act
            NimvioFormTestState.InvokeBeginDrag(form);
            NimvioFormTestState.InvokeEndDrag(form);

            // Assert
            Assert.Equal(NimvioBehavior.Waving, NimvioFormTestState.GetBehavior(form));
            Assert.True(form.Profile.Happiness > happinessBefore);
        });
    }

    [Fact]
    public void DragWithHighVelocityThrowsCharacter()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.InvokeBeginDrag(form);
            NimvioFormTestState.SetDidDrag(form, true);
            NimvioFormTestState.SetVelocity(form, new PointF(20, 15));

            // Act
            NimvioFormTestState.InvokeEndDrag(form);

            // Assert
            Assert.Equal(NimvioBehavior.Thrown, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void DragWithSmallMovementStartsSearching()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.InvokeBeginDrag(form);
            NimvioFormTestState.SetDidDrag(form, true);
            NimvioFormTestState.SetVelocity(form, new PointF(1, 1));

            // Act
            NimvioFormTestState.InvokeEndDrag(form);

            // Assert
            Assert.Equal(NimvioBehavior.Searching, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void TogglePlayingAndGoToScreenUpdateBehaviorState()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            settings.Playing = true;
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);

            // Act
            NimvioFormTestState.InvokeTogglePlaying(form);

            // Assert
            Assert.False(settings.Playing);

            // Arrange

            // Act
            NimvioFormTestState.InvokeGoToScreen(form, 0);

            // Assert
            Assert.Equal(NimvioBehavior.Walking, NimvioFormTestState.GetBehavior(form));

            // Arrange

            // Act
            NimvioFormTestState.InvokeGoToScreen(form, -1);
            NimvioFormTestState.InvokeGoToScreen(form, 99);

            // Assert
        });
    }

    public void Dispose() => _scenario.Dispose();
}
