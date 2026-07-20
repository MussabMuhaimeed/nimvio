using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormPerchedStateTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Fact]
    public void IsSafelyPerchedWhenPerchedAndSeatedBehavior()
    {
        // Arrange
        var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));

        NimvioFormTestHost.RunSta(() =>
        {
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetPerchedWindow(form, new IntPtr(42));
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);

            // Act
            var safelyPerched = form.IsSafelyPerched;
            var handle = form.PerchedWindowHandle;

            // Assert
            Assert.True(safelyPerched);
            Assert.Equal(new IntPtr(42), handle);
        });
    }

    [Theory]
    [InlineData(1)] // Walking
    [InlineData(27)] // Falling
    [InlineData(26)] // Hanging
    public void IsSafelyPerchedIsFalseWhileMovingOrUnsafeBehaviors(int behaviorValue)
    {
        // Arrange
        var behavior = (NimvioBehavior)behaviorValue;
        var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));

        NimvioFormTestHost.RunSta(() =>
        {
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetPerchedWindow(form, new IntPtr(99));
            NimvioFormTestState.SetBehavior(form, behavior);

            // Act
            var safelyPerched = form.IsSafelyPerched;

            // Assert
            Assert.False(safelyPerched);
        });
    }

    [Fact]
    public void IsSafelyPerchedIsFalseWithoutPerchedWindow()
    {
        // Arrange
        var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));

        NimvioFormTestHost.RunSta(() =>
        {
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);

            // Act
            var safelyPerched = form.IsSafelyPerched;

            // Assert
            Assert.False(safelyPerched);
        });
    }

    public void Dispose() => _scenario.Dispose();
}
