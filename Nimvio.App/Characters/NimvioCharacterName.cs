using System.Text.Json.Serialization;

namespace Nimvio;

[JsonConverter(typeof(NimvioCharacterNameJsonConverter))]
public enum NimvioCharacterName
{
    Unknown,
    Nova,
    Mimo,
    Lumi
}
