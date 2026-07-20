using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormSummonTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Fact]
    public void SummonToClampsWithinScreenEnablesVisibilityAndWaving()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            settings.Playing = false;
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var screen = Screen.PrimaryScreen!;
            var area = screen.WorkingArea;

            // Act
            form.SummonTo(screen, 12);

            // Assert
            Assert.InRange(form.Left, area.Left, Math.Max(area.Left, area.Right - form.Width));
            Assert.InRange(form.Top, area.Top, Math.Max(area.Top, area.Bottom - form.Height));
            Assert.Equal(1, form.Opacity);
            Assert.Equal(NimvioBehavior.Waving, NimvioFormTestState.GetBehavior(form));
            Assert.True(settings.Playing);
        });
    }

    public void Dispose() => _scenario.Dispose();
}
