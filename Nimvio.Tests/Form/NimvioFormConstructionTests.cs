using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormConstructionTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Fact]
    public void ConstructorAppliesSettingsAndRecordsPresence()
    {
        // Arrange
        var profile = NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova");
        var settings = NimvioFormScenario.SettingsWith(profile);
        settings.Size = 144;

        NimvioFormTestHost.RunSta(() =>
        {
            // Act
            var context = _scenario.CreateContext(settings, createInitialForms: true);
            var form = Assert.Single(context.Forms);

            // Assert
            Assert.Same(profile, form.Profile);
            Assert.Equal(new Size(144, 144), form.ClientSize);
            Assert.False(form.ShowInTaskbar);
            Assert.Equal(FormBorderStyle.None, form.FormBorderStyle);
            Assert.True(form.TopMost);
            Assert.True((DateTime.UtcNow - profile.LastSeenUtc).TotalMinutes < 1);
        });
    }

    [Fact]
    public void WorldCenterTracksFormPosition()
    {
        // Arrange
        var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));

        NimvioFormTestHost.RunSta(() =>
        {
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            form.Location = new Point(200, 100);

            // Act
            var center = form.WorldCenter;

            // Assert
            Assert.Equal(form.Left + form.Width / 2f, center.X);
            Assert.Equal(form.Top + form.Height / 2f, center.Y);
        });
    }

    public void Dispose() => _scenario.Dispose();
}
