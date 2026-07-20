using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormPaintCoverageTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Fact]
    public void RunPaintForTestsCoversEveryBehaviorAccessoryAndFacingCombination()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);

            // Act
            foreach (var behaviorValue in Enum.GetValues(typeof(NimvioBehavior)))
            {
                var behavior = (NimvioBehavior)behaviorValue!;
                NimvioFormTestState.SetBehavior(form, behavior);
                NimvioFormTestState.SetPhase(form, 2.5f);
                foreach (var accessory in Enum.GetValues<ActiveAppAccessory>())
                {
                    NimvioFormTestState.SetActiveAccessory(form, accessory);
                    NimvioFormTestState.SetFacingRight(form, true);
                    NimvioFormTestState.SetBlinkFrames(form, 0);
                    NimvioFormTestState.Paint(form);
                    NimvioFormTestState.SetFacingRight(form, false);
                    NimvioFormTestState.SetBlinkFrames(form, 4);
                    NimvioFormTestState.Paint(form);
                }
            }

            // Assert
            // (coverage exercise completes without exception)
        });
    }

    [Fact]
    public void RunPaintForTestsCoversSleepingAccessoriesAndSpeechBubble()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var calm = NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi");
            calm.Personality = NimvioPersonality.Calm;
            var settings = NimvioFormScenario.SettingsWith(calm);
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);

            // Act
            settings.Playing = false;
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sleeping);
            NimvioFormTestState.SetActiveAccessory(form, ActiveAppAccessory.Headphones);
            NimvioFormTestState.Paint(form);

            settings.Playing = true;
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.WatchingYouTube);
            NimvioFormTestState.SetActiveAccessory(form, ActiveAppAccessory.Book);
            NimvioFormTestState.Paint(form);

            NimvioFormTestState.SetBehavior(form, NimvioBehavior.HidingBehindWindow);
            NimvioFormTestState.SetSpeech(form, "Hello there!");
            NimvioFormTestState.Paint(form);

            // Assert
            // (coverage exercise completes without exception)
        });
    }

    public void Dispose() => _scenario.Dispose();
}
