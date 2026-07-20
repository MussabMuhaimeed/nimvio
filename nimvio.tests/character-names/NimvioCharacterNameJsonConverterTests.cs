using System.Text.Json;
using Nimvio;
using Xunit;

namespace Nimvio.Tests.CharacterNames;

public sealed class NimvioCharacterNameJsonConverterTests
{
    [Theory]
    [InlineData(NimvioCharacterName.Nova, "\"Nova\"")]
    [InlineData(NimvioCharacterName.Mimo, "\"Mimo\"")]
    [InlineData(NimvioCharacterName.Lumi, "\"Lumi\"")]
    [InlineData(NimvioCharacterName.Unknown, "\"Unknown\"")]
    public void SerializeWritesStableTextName(NimvioCharacterName value, string expectedJson)
    {
        // Arrange

        // Act
        var json = JsonSerializer.Serialize(value);

        // Assert
        Assert.Equal(expectedJson, json);
    }

    [Theory]
    [InlineData("\"Nova\"", NimvioCharacterName.Nova)]
    [InlineData("\"Mimo\"", NimvioCharacterName.Mimo)]
    [InlineData("\"Lumi\"", NimvioCharacterName.Lumi)]
    public void DeserializeKnownTextNameReturnsMatchingEnum(string json, NimvioCharacterName expected)
    {
        // Arrange

        // Act
        var value = JsonSerializer.Deserialize<NimvioCharacterName>(json);

        // Assert
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("\"NotACharacter\"")]
    [InlineData("\"nova\"")]
    [InlineData("\"\"")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void DeserializeUnknownOrWrongTokenReturnsUnknown(string json)
    {
        // Arrange

        // Act
        var value = JsonSerializer.Deserialize<NimvioCharacterName>(json);

        // Assert
        Assert.Equal(NimvioCharacterName.Unknown, value);
    }

    [Theory]
    [InlineData(1, NimvioCharacterName.Nova)]
    [InlineData(2, NimvioCharacterName.Mimo)]
    [InlineData(3, NimvioCharacterName.Lumi)]
    public void DeserializeLegacyDefinedNumberRemainsBackwardCompatible(int number, NimvioCharacterName expected)
    {
        // Arrange

        // Act
        var value = JsonSerializer.Deserialize<NimvioCharacterName>(number.ToString());

        // Assert
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999)]
    public void DeserializeUnknownNumberReturnsUnknown(int number)
    {
        // Arrange

        // Act
        var value = JsonSerializer.Deserialize<NimvioCharacterName>(number.ToString());

        // Assert
        Assert.Equal(NimvioCharacterName.Unknown, value);
    }

    [Fact]
    public void RoundTripAllDefinedValuesPreservesValue()
    {
        foreach (var expected in Enum.GetValues<NimvioCharacterName>())
        {
            // Arrange

            // Act
            var json = JsonSerializer.Serialize(expected);
            var actual = JsonSerializer.Deserialize<NimvioCharacterName>(json);

            // Assert
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [InlineData("\"Unknown\"")]
    public void DeserializeUnknownTextNameReturnsUnknown(string json)
    {
        // Arrange

        // Act
        var value = JsonSerializer.Deserialize<NimvioCharacterName>(json);

        // Assert
        Assert.Equal(NimvioCharacterName.Unknown, value);
    }

    [Fact]
    public void DeserializeLegacyZeroNumberMapsToUnknown()
    {
        // Arrange

        // Act
        var value = JsonSerializer.Deserialize<NimvioCharacterName>("0");

        // Assert
        Assert.Equal(NimvioCharacterName.Unknown, value);
    }

    [Fact]
    public void ProfileJsonUsesConverterForNameProperty()
    {
        // Arrange
        var profile = new NimvioProfile { Name = NimvioCharacterName.Mimo };

        // Act
        var json = JsonSerializer.Serialize(profile);
        using var document = JsonDocument.Parse(json);

        // Assert
        Assert.Equal("Mimo", document.RootElement.GetProperty("Name").GetString());
    }
}
