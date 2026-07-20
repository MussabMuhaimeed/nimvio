using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nimvio;

internal sealed class NimvioCharacterNameJsonConverter : JsonConverter<NimvioCharacterName>
{
    public NimvioCharacterNameJsonConverter() { }

    public override NimvioCharacterName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String
            && Enum.TryParse<NimvioCharacterName>(reader.GetString(), ignoreCase: false, out var name)
            && name != NimvioCharacterName.Unknown)
        {
            return name;
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value)
            && Enum.IsDefined(typeof(NimvioCharacterName), value))
        {
            return (NimvioCharacterName)value;
        }

        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
        {
            reader.Skip();
        }

        return NimvioCharacterName.Unknown;
    }

    public override void Write(Utf8JsonWriter writer, NimvioCharacterName value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
