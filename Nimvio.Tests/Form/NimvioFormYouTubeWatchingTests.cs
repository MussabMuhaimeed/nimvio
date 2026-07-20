using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormYouTubeWatchingTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Fact]
    public void BeginYouTubeWatchingPositionsCharacterAndShowsLeadSpeech()
    {
        // Arrange
        var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));
        var window = new WindowSnapshot(new IntPtr(9001), new Rectangle(100, 100, 800, 500), "chrome", "Cute cats");

        NimvioFormTestHost.RunSta(() =>
        {
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            form.Location = new Point(50, 50);

            // Act
            form.BeginYouTubeWatching(window, index: 0, viewerCount: 2);

            // Assert
            Assert.Equal(NimvioBehavior.WatchingYouTube, NimvioFormTestState.GetBehavior(form));
            Assert.Equal(window.Handle, form.PerchedWindowHandle);
            Assert.Equal("Let's watch!", NimvioFormTestState.GetSpeechText(form));
            var target = NimvioFormTestState.GetTarget(form);
            Assert.InRange(target.X, window.Bounds.Left, window.Bounds.Right);
            Assert.True(target.Y < window.Bounds.Top + 20);
        });
    }

    [Fact]
    public void BeginYouTubeWatchingForAdditionalViewerTargetsSecondSlotAlongWindowTop()
    {
        // Arrange
        var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
        var window = new WindowSnapshot(new IntPtr(9002), new Rectangle(0, 0, 600, 400), "chrome", "Stream");

        NimvioFormTestHost.RunSta(() =>
        {
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var spacing = window.Bounds.Width / 3f;

            // Act
            form.BeginYouTubeWatching(window, index: 1, viewerCount: 2);

            // Assert
            var target = NimvioFormTestState.GetTarget(form);
            Assert.Equal(window.Bounds.Left + spacing * 2, target.X, precision: 1);
            Assert.Equal(NimvioBehavior.WatchingYouTube, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void BeginYouTubeWatchingWhenAlreadyWatchingSameWindowIsNoOp()
    {
        // Arrange
        var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
        var window = new WindowSnapshot(new IntPtr(9003), new Rectangle(20, 20, 640, 360), "chrome", "Video");

        NimvioFormTestHost.RunSta(() =>
        {
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            form.BeginYouTubeWatching(window, index: 0, viewerCount: 1);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.WatchingYouTube);
            var speech = NimvioFormTestState.GetSpeechText(form);

            // Act
            form.BeginYouTubeWatching(window, index: 0, viewerCount: 1);

            // Assert
            Assert.Equal(speech, NimvioFormTestState.GetSpeechText(form));
            Assert.Equal(NimvioBehavior.WatchingYouTube, NimvioFormTestState.GetBehavior(form));
        });
    }

    public void Dispose() => _scenario.Dispose();
}
