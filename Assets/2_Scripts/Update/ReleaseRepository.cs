/// <summary>
/// The GitHub repository releases are published to. The in-game updater checks it and the build window uploads to
/// it, so moving releases to another repository is a change to this one value.
/// </summary>
public static class ReleaseRepository
{
    public const string Name = "danielnoam/ElectroGrid";
    public const string LatestReleaseApiUrl = "https://api.github.com/repos/" + Name + "/releases/latest";
    public const string LatestReleasePageUrl = "https://github.com/" + Name + "/releases/latest";
}
