using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#if NETCOREAPP3_0_OR_GREATER
using System.Text.Unicode;
#endif

namespace System;

[Serializable]
[DebuggerDisplay("{ToString()}")]
#if NETCOREAPP3_1_OR_GREATER || SEMVER2_SYSTEMTEXTJSON
[System.Text.Json.Serialization.JsonConverter(typeof(SemVerJsonConverter))]
#endif
public readonly struct SemVer : IEquatable<SemVer>, IComparable<SemVer>, IComparable
#if NET6_0_OR_GREATER
, ISpanFormattable
#endif
#if NET7_0_OR_GREATER
, ISpanParsable<SemVer>
#endif
#if NET8_0_OR_GREATER
, IUtf8SpanFormattable
, IUtf8SpanParsable<SemVer>
#endif
{
    public uint Major { get; }
    public uint Minor { get; }
    public uint Patch { get; }
    public string? Prerelease { get; }
    public string? Build { get; }

    public static readonly SemVer Zero = default;

    SemVer(uint major, uint minor, uint patch, string? prerelease, string? build)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
        Build = build;
    }

    public static SemVer Create(Version version)
    {
        return new SemVer((uint)version.Major, (uint)version.Minor, (uint)version.Build, null, null);
    }

    public static SemVer Create(uint major, uint minor, uint patch)
    {
        return new SemVer(major, minor, patch, null, null);
    }

    public static SemVer Create(uint major, uint minor, uint patch, string? prerelease, string? build)
    {
        if (prerelease != null) CheckValidIdentifier(prerelease, nameof(prerelease));
        if (build != null) CheckValidIdentifier(build, nameof(build));

        return new SemVer(major, minor, patch, prerelease, build);
    }

    public bool Equals(SemVer other)
    {
        return Major == other.Major &&
            Minor == other.Minor &&
            Patch == other.Patch &&
            Prerelease == other.Prerelease &&
            Build == other.Build;
    }

    public override bool Equals(object? obj)
    {
        return obj is SemVer ver && Equals(ver);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Patch, Prerelease, Build);
    }

    public static bool operator ==(SemVer left, SemVer right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SemVer left, SemVer right)
    {
        return !(left == right);
    }

    public int CompareTo(SemVer other)
    {
        var major = Major.CompareTo(other.Major);
        if (major != 0) return major;

        var minor = Minor.CompareTo(other.Minor);
        if (minor != 0) return minor;

        var patch = Patch.CompareTo(other.Patch);
        if (patch != 0) return patch;

        if (Prerelease != null)
        {
            if (other.Prerelease == null) return -1;
            return Prerelease.CompareTo(other.Prerelease);
        }
        else if (other.Prerelease != null)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    public int CompareTo(object? obj)
    {
        if (obj == null)
        {
            return 1;
        }

        if (obj is SemVer semVer)
        {
            return this.CompareTo(semVer);
        }

        throw new ArgumentException("Object must be of type SemVer.", nameof(obj));
    }

    public static bool operator >(SemVer lhs, SemVer rhs)
    {
        return lhs.CompareTo(rhs) == 1;
    }

    public static bool operator <(SemVer lhs, SemVer rhs)
    {
        return lhs.CompareTo(rhs) == -1;
    }

    public static bool operator >=(SemVer lhs, SemVer rhs)
    {
        return lhs.CompareTo(rhs) != -1;
    }

    public static bool operator <=(SemVer lhs, SemVer rhs)
    {
        return lhs.CompareTo(rhs) != 1;
    }

    public override string ToString()
    {
        var size = GetRequiredBufferSize();
#if NETCOREAPP2_1_OR_GREATER
        return string.Create(size, this, static (span, state) =>
        {
            state.TryFormat(span, out _);
        });
#else
        var rentedArray = size > 256 ? ArrayPool<char>.Shared.Rent(size) : null;
        try
        {
            scoped var buffer = rentedArray.AsSpan();
            if (buffer == null) buffer = stackalloc char[size];
            TryFormat(buffer, out var charsWritten);
            unsafe
            {
                return new string((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer)), 0, charsWritten);
            }
        }
        finally
        {
            if (rentedArray != null) ArrayPool<char>.Shared.Return(rentedArray);
        }
#endif
    }

    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    public bool TryFormat(Span<char> destination, out int charsWritten) => TryFormat(destination, out charsWritten, default, null);

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        charsWritten = GetRequiredBufferSize();
        if (destination.Length < charsWritten)
        {
            charsWritten = 0;
            return false;
        }

        Major.TryFormat(destination, out var c);
        destination[c] = '.';
        destination = destination[(c + 1)..];
        Minor.TryFormat(destination, out c);
        destination[c] = '.';
        destination = destination[(c + 1)..];
        Patch.TryFormat(destination, out c);
        destination = destination[c..];

        if (Prerelease != null)
        {
            destination[0] = '-';
            Prerelease.AsSpan().TryCopyTo(destination[1..]);
            destination = destination[(Prerelease.Length + 1)..];
        }

        if (Build != null)
        {
            destination[0] = '+';
            Build.AsSpan().TryCopyTo(destination[1..]);
        }

        return true;
    }

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten) => TryFormat(utf8Destination, out bytesWritten, default, null);

    public unsafe bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        bytesWritten = GetRequiredBufferSize();
        if (utf8Destination.Length < bytesWritten)
        {
            bytesWritten = 0;
            return false;
        }

#if NET8_0_OR_GREATER
        Major.TryFormat(utf8Destination, out var c);
#else
        Utf8Formatter.TryFormat(Major, utf8Destination, out var c);
#endif
        utf8Destination[c] = (byte)'.';
        utf8Destination = utf8Destination[(c + 1)..];

#if NET8_0_OR_GREATER
        Minor.TryFormat(utf8Destination, out c);
#else
        Utf8Formatter.TryFormat(Minor, utf8Destination, out c);
#endif
        utf8Destination[c] = (byte)'.';
        utf8Destination = utf8Destination[(c + 1)..];

#if NET8_0_OR_GREATER
        Patch.TryFormat(utf8Destination, out c);
#else
        Utf8Formatter.TryFormat(Patch, utf8Destination, out c);
#endif
        utf8Destination = utf8Destination[c..];

        if (Prerelease != null)
        {
            utf8Destination[0] = (byte)'-';
#if NETCOREAPP3_0_OR_GREATER
            Utf8.FromUtf16(Prerelease, utf8Destination[1..], out _, out var b);
            utf8Destination = utf8Destination[(b + 1)..];
#else
            fixed (char* src = Prerelease)
            fixed (byte* dst = utf8Destination[1..])
            {
                var b = Encoding.UTF8.GetBytes(src, Prerelease.Length, dst, utf8Destination.Length - 1);
                utf8Destination = utf8Destination[(b + 1)..];
            }
#endif
        }

        if (Build != null)
        {
            utf8Destination[0] = (byte)'+';
#if NETCOREAPP3_0_OR_GREATER
            Utf8.FromUtf16(Build, utf8Destination[1..], out _, out var b);
#else
            fixed (char* src = Build)
            fixed (byte* dst = utf8Destination[1..])
            {
                var b = Encoding.UTF8.GetBytes(src, Build.Length, dst, utf8Destination.Length - 1);
            }
#endif
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetRequiredBufferSize()
    {
        var count = 2 + FormattingHelpers.CountDigits(Major) + FormattingHelpers.CountDigits(Minor) + FormattingHelpers.CountDigits(Patch);
        if (Prerelease != null) count += 1 + Prerelease.Length;
        if (Build != null) count += 1 + Build.Length;
        return count;
    }

    public static SemVer Parse(string s) => Parse(s.AsSpan(), null);

    public static SemVer Parse(string s, IFormatProvider? provider)
    {
        return Parse(s.AsSpan(), provider);
    }

    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out SemVer result) => TryParse(s, null, out result);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out SemVer result)
    {
        if (s == null)
        {
            result = default;
            return false;
        }

        return TryParse(s.AsSpan(), provider, out result);
    }

    public static SemVer Parse(ReadOnlySpan<char> s) => Parse(s, null);

    public static SemVer Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out var semVer)) throw new FormatException($"The input string '{s.ToString()}' was not in a correct format.");
        return semVer;
    }

    public static bool TryParse(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out SemVer result) => TryParse(s, null, out result);

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out SemVer result)
    {
        // Major
        var p = s.IndexOf('.');
        if (p == -1 || s.Length == p + 1 || !uint.TryParse(s[..p], out var major)) goto FAIL;
        s = s[(p + 1)..];

        // Minor
        p = s.IndexOf('.');
        if (p == -1 || s.Length == p + 1 || !uint.TryParse(s[..p], out var minor)) goto FAIL;
        s = s[(p + 1)..];

        // Check Pre-release & Build metadata
        var p0 = s.IndexOf('-');
        int p1;
        if (p0 == -1 || s.Length == p0 + 1)
        {
            p1 = s.IndexOf('+');
        }
        else
        {
            p1 = s[p0..].IndexOf('+');
            if (p1 != -1) p1 += p0;
        }

        if (p0 == -1) p = p1;
        else if (p1 == -1) p = p0;
        else p = Math.Min(p0, p1);

        // Patch
        if (!uint.TryParse(p == -1 ? s : s[..p], out var patch)) goto FAIL;

        // Pre-release
        string? prerelease = null;
        if (p0 != -1)
        {
            if (s.Length == p0 + 1) goto FAIL;

            var slice = p1 == -1 ? s[(p0 + 1)..] : s[(p0 + 1)..p1];
            if (!IsValidIdentifier(slice)) goto FAIL;
            prerelease = slice.ToString();
        }

        // Build metadata
        string? build = null;
        if (p1 != -1)
        {
            if (s.Length == p1 + 1) goto FAIL;

            var slice = s[(p1 + 1)..];
            if (!IsValidIdentifier(slice)) goto FAIL;
            build = slice.ToString();
        }

        result = new SemVer(major, minor, patch, prerelease, build);
        return true;

    FAIL:
        result = default;
        return false;
    }

    public static SemVer Parse(ReadOnlySpan<byte> utf8Text) => Parse(utf8Text, null);

    public static SemVer Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
    {
        if (!TryParse(utf8Text, provider, out var semVer)) throw new FormatException($"The input string '{Encoding.UTF8.GetString(utf8Text.ToArray())}' was not in a correct format.");
        return semVer;
    }

    public static bool TryParse(ReadOnlySpan<byte> utf8Text, [MaybeNullWhen(false)] out SemVer result) => TryParse(utf8Text, null, out result);

    public static unsafe bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out SemVer result)
    {
        // Major
        var p = utf8Text.IndexOf((byte)'.');
        if (p == -1 || utf8Text.Length == p + 1 ||
#if NET8_0_OR_GREATER
            !uint.TryParse(utf8Text[..p], out var major)
#else
            !Utf8Parser.TryParse(utf8Text[..p], out uint major, out _)
#endif
        )
        {
            goto FAIL;
        }
        utf8Text = utf8Text[(p + 1)..];

        // Minor
        p = utf8Text.IndexOf((byte)'.');
        if (p == -1 || utf8Text.Length == p + 1 ||
#if NET8_0_OR_GREATER
            !uint.TryParse(utf8Text[..p], out var minor)
#else
            !Utf8Parser.TryParse(utf8Text[..p], out uint minor, out _)
#endif
        )
        {
            goto FAIL;
        }
        utf8Text = utf8Text[(p + 1)..];

        // Check Pre-release & Build metadata
        var p0 = utf8Text.IndexOf((byte)'-');
        var p1 = utf8Text.IndexOf((byte)'+');

        if (p0 == -1) p = p1;
        else if (p1 == -1) p = p0;
        else p = Math.Min(p0, p1);

        // Patch
#if NET8_0_OR_GREATER
        if (!uint.TryParse(p == -1 ? utf8Text : utf8Text[..p], out var patch)) goto FAIL;
#else
        if (!Utf8Parser.TryParse(p == -1 ? utf8Text : utf8Text[..p], out uint patch, out _)) goto FAIL;
#endif

        // Pre-release
        string? prerelease = null;
        if (p0 != -1)
        {
            if (utf8Text.Length == p0 + 1) goto FAIL;

            var slice = p1 == -1 ? utf8Text[(p0 + 1)..] : utf8Text[(p0 + 1)..p1];

#if NETSTANDARD2_1_OR_GREATER
            prerelease = Encoding.UTF8.GetString(slice);
#else
            fixed (byte* src = slice)
            {
                prerelease = Encoding.UTF8.GetString(src, slice.Length);
            }
#endif
            if (!IsValidIdentifier(prerelease.AsSpan())) goto FAIL;
        }

        // Build metadata
        string? build = null;
        if (p1 != -1)
        {
            if (utf8Text.Length == p1 + 1) goto FAIL;

            var slice = utf8Text[(p1 + 1)..];
#if NETSTANDARD2_1_OR_GREATER
            build = Encoding.UTF8.GetString(slice);
#else
            fixed (byte* src = slice)
            {
                build = Encoding.UTF8.GetString(src, slice.Length);
            }
#endif
            if (!IsValidIdentifier(build.AsSpan())) goto FAIL;
        }

        result = new SemVer(major, minor, patch, prerelease, build);
        return true;

    FAIL:
        result = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsValidChar(char c)
    {
        if ((uint)((c | 0x20) - 'a') <= 'z' - 'a') return true;
        if ((uint)(c - '0') <= (uint)('9' - '0')) return true;
        if (c is '.') return true;
        return false;
    }

    static void CheckValidIdentifier(string identifier, string paramName)
    {
        if (identifier == "") throw new ArgumentException("Identifiers must not empty", paramName);

        foreach (var c in identifier.AsSpan())
        {
            if (!IsValidChar(c)) throw new ArgumentException("Identifiers must comprise only ASCII alphanumerics and hyphens", paramName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsValidIdentifier(ReadOnlySpan<char> identifier)
    {
        if (identifier.IsEmpty) return false;

        foreach (var c in identifier)
        {
            if (!IsValidChar(c)) return false;
        }

        return true;
    }
}
