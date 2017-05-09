#tool "nuget:?package=GitVersion.CommandLine";

var gitVersion = GitVersion();

var BuildVersion = new {
    Full = gitVersion.SemVer + "+" + gitVersion.Sha,
    Semantic = gitVersion.FullSemVer,
    MajorMinorPatch = gitVersion.MajorMinorPatch,
    Tag = gitVersion.PreReleaseLabel
};
