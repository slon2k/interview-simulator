using System.Text.Json;
using System.Text.Json.Serialization;

namespace InterviewSimulator.Api.Features.Common;

public sealed class LenientEnumJsonConverter<T> : JsonConverter<T>
    where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            reader.Skip();
            return (T)(object)(-1);
        }

        var value = reader.GetString();
        if (Enum.TryParse<T>(value, ignoreCase: true, out var result) && Enum.IsDefined(result))
        {
            return result;
        }

        // Unknown name -> sentinel; FluentValidation IsInEnum() rejects it (field-level 400).
        return (T)(object)(-1);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (!Enum.IsDefined(value))
        {
            throw new JsonException($"Cannot serialize undefined {typeof(T).Name} value '{value}'.");
        }

        writer.WriteStringValue(value.ToString());
    }
}
