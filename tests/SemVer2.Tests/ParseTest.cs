namespace SemVer2Tests;

public class ParseTest
{
    [Theory]
    [InlineData(["1.2.3", 1, 2, 3])]
    [InlineData(["12.34.56", 12, 34, 56])]
    public void Test_Parse_Simple(string text, uint major, uint minor, uint patch)
    {
        var semVer = SemVer.Parse(text);
        Assert.Equal(major, semVer.Major);
        Assert.Equal(minor, semVer.Minor);
        Assert.Equal(patch, semVer.Patch);
        Assert.Null(semVer.Prerelease);
        Assert.Null(semVer.Build);
    }

    [Theory]
    [InlineData(["1.2.3-alpha", 1, 2, 3, "alpha", null])]
    [InlineData(["1.0.0-beta+001", 1, 0, 0, "beta", "001"])]
    public void Test_Parse_Complex(string text, uint major, uint minor, uint patch, string? prerelease, string?build)
    {
        var semVer = SemVer.Parse(text);
        Assert.Equal(major, semVer.Major);
        Assert.Equal(minor, semVer.Minor);
        Assert.Equal(patch, semVer.Patch);
        Assert.Equal(prerelease, semVer.Prerelease);
        Assert.Equal(build, semVer.Build);
    }
}
