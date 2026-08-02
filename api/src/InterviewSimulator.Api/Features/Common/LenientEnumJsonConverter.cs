using System.Text.Json;
using System.Text.Json.Serialization;

namespace InterviewSimulator.Api.Features.Common;

// Maps unknown string values to (T)(-1) instead of throwing, so FluentValidation can return 400.
public sealed class LenientEnumJsonConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
            {
                return result;
            }

            return (T)(object)(-1);
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
