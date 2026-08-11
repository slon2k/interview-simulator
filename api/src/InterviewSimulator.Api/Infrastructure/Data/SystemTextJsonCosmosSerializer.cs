using System.Text.Json;

using Microsoft.Azure.Cosmos;

namespace InterviewSimulator.Api.Infrastructure.Data;

// CosmosSystemTextJsonSerializer is internal in Cosmos SDK 3.x; this wrapper provides the same behaviour.
internal sealed class SystemTextJsonCosmosSerializer(JsonSerializerOptions options) : CosmosSerializer
{
    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

            return JsonSerializer.Deserialize<T>(stream, options)!;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, input, options);
        stream.Position = 0;
        return stream;
    }
}
