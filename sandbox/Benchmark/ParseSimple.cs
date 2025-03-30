using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using NuGet.Versioning;
using Semver;

[Orderer(SummaryOrderPolicy.SlowestToFastest)]
public class ParseSimple
{
    const string Text = "1.2.3";

    [Benchmark(Description = "Nuget.Versioning")]
    public NuGetVersion NugetVersioning()
    {
        NuGetVersion.TryParseStrict(Text, out var version);
        return version!;
    }

    [Benchmark(Description = "WalkerCodeRanger/semver")]
    public SemVersion Semver()
    {
        return SemVersion.Parse(Text);
    }

    [Benchmark(Description = "adamreeve/semver.net")]
    public SemanticVersioning.Version SemverNet()
    {
        return SemanticVersioning.Version.Parse(Text);
    }

    [Benchmark]
    public SemVer SemVer2()
    {
        return SemVer.Parse(Text);
    }
}