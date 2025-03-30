#if NETCOREAPP2_1_OR_GREATER || SEMVER2_SYSTEMTEXTJSON

using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace System;

public class SemVerJsonConverter : JsonConverter<SemVer>
{
    public override SemVer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            if (reader.TokenType != JsonTokenType.String) throw new JsonException("Expected string");
            return SemVer.Parse(reader.ValueSpan);
        }
        catch (FormatException e)
        {
            throw new JsonException(e.Message, e);
        }
    }

    public override void Write(Utf8JsonWriter writer, SemVer value, JsonSerializerOptions options)
    {
        var capacity = value.GetRequiredBufferSize();
        var rentedArray = capacity > 256 ? ArrayPool<byte>.Shared.Rent(capacity) : null; 
        try
        {
            scoped var buffer = rentedArray.AsSpan();
            if (rentedArray == null) buffer = stackalloc byte[capacity];
            value.TryFormat(buffer, out var bytesWritten);
            writer.WriteStringValue(buffer[..bytesWritten]);
        }
        finally
        {
            if (rentedArray != null) ArrayPool<byte>.Shared.Return(rentedArray);
        }
    }
}

#endif