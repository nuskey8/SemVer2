using ConsoleAppFramework;

var app = ConsoleApp.Create();
app.Add<Commands>();
app.Run(args);

class Commands
{
    /// <summary>
    /// Prints valid versions sorted by SemVer precedence
    /// </summary>
    /// <param name="increment">-i, Increment a version by the specified level. (major | minor | patch | prerelease | release)</param>
    /// <param name="preid">Identifier to be used to prerelease version increments.</param>
    /// <returns></returns>
    [Command("")]
    public int Root([Argument] string[] input, string? increment = null, string? preid = null)
    {
        var list = new List<SemVer>();

        foreach (var i in input)
        {
            if (!SemVer.TryParse(i, out var version)) continue;

            if (increment != null)
            {
                switch (increment.ToLowerInvariant())
                {
                    case "major":
                        version = SemVer.Create(version.Major + 1, version.Minor, version.Patch, version.Prerelease, null);
                        break;
                    case "minor":
                        version = SemVer.Create(version.Major, version.Minor + 1, version.Patch, version.Prerelease, null);
                        break;
                    case "patch":
                        version = SemVer.Create(version.Major, version.Minor, version.Patch + 1, version.Prerelease, null);
                        break;
                    case "release":
                        version = SemVer.Create(version.Major, version.Minor, version.Patch, null, null);
                        break;
                    case "prerelease":
                        if (preid == null)
                        {
                            Console.WriteLine("Parameter 'preid' is required.");
                            return 1;
                        }

                        version = SemVer.Create(version.Major, version.Minor, version.Patch, preid, null);
                        break;
                }
            }

            list.Add(version);
        }

        foreach (var v in list.OrderBy(x => x))
        {
            Console.WriteLine(v);
        }

        return 0;
    }

    [Command("valid")]
    public void Valid([Argument] string[] input)
    {
    }
}