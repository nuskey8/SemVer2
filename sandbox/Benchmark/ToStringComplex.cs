using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using NuGet.Versioning;
using Semver;

[Orderer(SummaryOrderPolicy.SlowestToFastest)]
public class ToStringComplex
{
    const string Text = "1.0.0-beta+exp.sha.5114f85";

    readonly NuGetVersion nuGetVersion = NuGetVersion.Parse(Text);
    readonly SemVersion semverVersion = SemVersion.Parse(Text);
    readonly SemanticVersioning.Version semanticVersioningVersion = SemanticVersioning.Version.Parse(Text);
    readonly SemVer semver2 = SemVer.Parse(Text);

    [Benchmark(Description = "Nuget.Versioning")]
    public string NugetVersioning()
    {
        return nuGetVersion.ToFullString();
    }

    [Benchmark(Description = "WalkerCodeRanger/semver")]
    public string Semver()
    {
        return semverVersion.ToString();
    }

    [Benchmark(Description = "adamreeve/semver.net")]
    public string SemverNet()
    {
        return semanticVersioningVersion.Clean();
    }

    [Benchmark]
    public string SemVer2()
    {
        return semver2.ToString();
    }
}