using System.Text.Json;
using System.Text.Json.Serialization;

namespace TurkiyeSimulasyonu.Api.Json;

// API'den çıkan bütün tarihler UTC'ye çevrilip "Z" ile yazılır: 2026-09-20T10:00:00Z
public sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDateTimeOffset();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.UtcDateTime);
    }
}