# SemVer2
 Fast C# Implementation of Semantic Versioning 2.0 for .NET

[![NuGet](https://img.shields.io/nuget/v/SemVer2.svg)](https://www.nuget.org/packages/SemVer2)
[![Releases](https://img.shields.io/github/release/nuskey8/SemVer2.svg)](https://github.com/nuskey8/SemVer2/releases)
[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)

English | [日本語](./README_JA.md)

## Overview

SemVer2 is a high-performance Semantic Versioning 2.0 implementation for .NET. It supports modern interfaces such as `ISpanFormattable` and `ISpanParsable<T>` and is designed to operate faster than libraries like [semver](https://github.com/WalkerCodeRanger/semver) and [NuGet.Versioning](https://www.nuget.org/packages/NuGet.Versioning).

![img](./docs/images/img-benchmark.png)

Additionally, it includes extensions for System.Text.Json and [MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp) and provides a CLI tool available as a .NET Tool.

## Installation

### NuGet packages

SemVer2 requires .NET Standard 2.1 or later. You can get the package from NuGet.

### .NET CLI

```ps1
dotnet add package SemVer2
```

### Package Manager

```ps1
Install-Package SemVer2
```

## Usage

You can use the `SemVer` struct to handle Semantic Versioning.

```cs
var semver = SemVer.Create(1, 0, 0, "alpha", "001");
Console.WriteLine(semver);            // 1.0.0-alpha+001
Console.WriteLine(semver.Major);      // 1
Console.WriteLine(semver.Minor);      // 0
Console.WriteLine(semver.Patch);      // 0
Console.WriteLine(semver.Prerelease); // alpha
Console.WriteLine(semver.Build);      // 001
```

`SemVer` implements `ISpanFormattable`, `ISpanParsable<T>`, `IUtf8SpanFormattable`, and `IUtf8SpanParsable<T>`, allowing conversions between UTF-16/UTF-8 strings.

```cs
var semver = SemVer.Parse("1.0.0-alpha+001");

Span<char> buffer = stackalloc char[15];
semver.TryFormat(buffer, out var charsWritten);
```

`SemVer` also implements `IComparable<T>` and provides overloaded comparison operators (`>`, `<`, `>=`, `<=`), enabling version comparisons.

```cs
var a = SemVer.Parse("1.0.0");
var b = SemVer.Parse("1.0.1");
var c = SemVer.Parse("1.0.0-alpha");

Console.WriteLine(a < b); // True
Console.WriteLine(a < c); // False
```

## System.Text.Json

`SemVer` provides `SemVerJsonConverter` for System.Text.Json. This is included by default in .NET Core 3.0 and later. For .NET Standard 2.1, you can install the [SemVer2.SystemTextJson](https://www.nuget.org/packages/SemVer2.SystemTextJson/) package.

```ps1
Install-Package SemVer2.SystemTextJson
```

```cs
var options = new JsonSerializerOptions()
{
    Converters =
    {
        new SemVerJsonConverter()
    }
};

JsonSerializer.Serialize(SemVer.Parse("1.0.0"), options);
```

## MessagePack-CSharp

Similarly, `SemVerMessagePackFormatter` and `SemVerMessagePackResolver` are provided for [MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp). You can use them by installing the [SemVer2.MessagePack](https://www.nuget.org/packages/SemVer2.MessagePack/) package.

```ps1
Install-Package SemVer2.MessagePack
```

```cs
var resolver = MessagePack.Resolvers.CompositeResolver.Create(
    SemVerMessagePackResolver.Instance,
    MessagePack.Resolvers.StandardResolver.Instance);
var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

MessagePackSerializer.Serialize(SemVer.Parse("1.0.0"), options);
```

## CLI

SemVer2 provides a .NET Tool that allows you to validate and increment versions via the command line, similar to [node-semver](https://github.com/npm/node-semver).

```
dotnet tool install --global SemVer2.Cli
```

You can use it as follows:

```bash
$ dotnet semver 1.2.3
1.2.3
$ dotnet semver foo

$ dotnet semver 1.0.0 -i patch
1.0.1

$ dotnet semver 1.0.0,1.0.1,1.0.0-alpha
1.0.0-alpha
1.0.0
1.0.1
```

Command details are as follows:

```
Usage: [arguments...] [options...] [-h|--help] [--version]

Prints valid versions sorted by SemVer precedence

Arguments:
  [0] <string[]>

Options:
  -i|--increment <string?>    Increment a version by the specified level. (major | minor | patch | prerelease | release) (Default: null)
  --preid <string?>           Identifier to be used for prerelease version increments. (Default: null)
```

## Performance

Benchmark results are as follows:

### Parse (`1.2.3`)

| Method                  |      Mean |    Error |   StdDev |
| ----------------------- | --------: | -------: | -------: |
| adamreeve/semver.net    | 306.70 ns | 6.142 ns | 5.746 ns |
| WalkerCodeRanger/semver | 186.98 ns | 2.114 ns | 1.650 ns |
| Nuget.Versioning        |  97.16 ns | 1.245 ns | 0.972 ns |
| SemVer2                 |  25.85 ns | 0.541 ns | 1.464 ns |

### Parse (`1.0.0-beta+exp.sha.5114f85`)

| Method                  |      Mean |    Error |   StdDev |
| ----------------------- | --------: | -------: | -------: |
| adamreeve/semver.net    | 474.64 ns | 1.458 ns | 1.139 ns |
| WalkerCodeRanger/semver | 363.51 ns | 2.213 ns | 2.070 ns |
| Nuget.Versioning        | 201.76 ns | 1.612 ns | 1.346 ns |
| SemVer2                 |  60.53 ns | 0.592 ns | 0.525 ns |

### ToString (`1.0.0-beta+exp.sha.5114f85`)

| Method                  |      Mean |    Error |   StdDev |    Median |
| ----------------------- | --------: | -------: | -------: | --------: |
| adamreeve/semver.net    | 194.92 ns | 0.915 ns | 0.856 ns | 195.11 ns |
| WalkerCodeRanger/semver |  67.94 ns | 2.613 ns | 7.703 ns |  63.78 ns |
| Nuget.Versioning        |  37.58 ns | 0.783 ns | 1.599 ns |  37.24 ns |
| SemVer2                 |  20.79 ns | 0.166 ns | 0.139 ns |  20.79 ns |

## License

This library is released under the [MIT License](./LICENSE).

