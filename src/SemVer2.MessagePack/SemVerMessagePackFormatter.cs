using MessagePack;
using MessagePack.Formatters;

namespace System;

public class SemVerMessagePackFormatter : IMessagePackFormatter<SemVer>
{
    public SemVer Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        try
        {
            if (!reader.TryReadStringSpan(out var span)) throw new MessagePackSerializationException("String Expected");
            return SemVer.Parse(span);
        }
        catch (FormatException ex)
        {
            throw new MessagePackSerializationException(ex.Message, ex);
        }
    }

    public void Serialize(ref MessagePackWriter writer, SemVer value, MessagePackSerializerOptions options)
    {
        var span = writer.GetSpan(value.GetRequiredBufferSize());
        value.TryFormat(span, out var bytesWritten);
        writer.Advance(bytesWritten);
    }
}

public class SemVerMessagePackResolver : IFormatterResolver
{
    public static readonly IFormatterResolver Instance = new SemVerMessagePackResolver();

    SemVerMessagePackResolver()
    {

    }

    public IMessagePackFormatter<T> GetFormatter<T>()
    {
        return Cache<T>.formatter!;
    }

    static class Cache<T>
    {
        public static readonly IMessagePackFormatter<T>? formatter;

        static Cache()
        {
            if (typeof(T) == typeof(SemVer))
            {
                formatter = (IMessagePackFormatter<T>)(object)new SemVerMessagePackFormatter();
            }
        }
    }
}
