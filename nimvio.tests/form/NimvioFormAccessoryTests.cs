using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormAccessoryTests
{
    [Theory]
    [InlineData("Spotify", ActiveAppAccessory.Headphones)]
    [InlineData("vlc", ActiveAppAccessory.Headphones)]
    [InlineData("Code", ActiveAppAccessory.Pen)]
    [InlineData("devenv", ActiveAppAccessory.Pen)]
    [InlineData("chrome", ActiveAppAccessory.Book)]
    [InlineData("msedge", ActiveAppAccessory.Book)]
    [InlineData("explorer", ActiveAppAccessory.None)]
    public void AccessoryForMapsKnownProcesses(string processName, ActiveAppAccessory expected)
    {
        // Arrange
        // (processName and expected from inline data)

        // Act
        var actual = NimvioForm.AccessoryFor(processName);

        // Assert
        Assert.Equal(expected, actual);
    }
}
