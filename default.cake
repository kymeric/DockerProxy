#tool "nuget:?package=Cake.CoreCLR";
#addin "Cake.Json";
#addin "Cake.FileHelpers";
#addin "Newtonsoft.Json";

#load "./cake/all.cake";

//Arguments
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var artifacts = (DirectoryPath)Argument("artifacts", "./artifacts");

//Constants

//Gather configuration into single object here.  Any variables used across tasks should be gathered here
// to minimize code duplication and bad hand-offs across tasks
var Config = new {
    Version = new {
        Full = BuildVersion.Full,
        Semantic = BuildVersion.Semantic
    },
    Projects = new {
        DockerProxy = new FilePath("./DockerProxy/DockerProxy.csproj"),
        UnitTests = GetFiles("./**/*.UnitTests.csproj")
    },
    Artifacts = artifacts
};

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package");
    
Task("Build")
    .Does(() => {
        //Build Docker Proxy
        DotNetRestore(Config.Projects.DockerProxy.GetDirectory());
        DotNetBuild(Config.Projects.DockerProxy.GetDirectory(), Config.Version.Full, configuration);
    });

Task("Test")
    .Does(() => {
        //Run all UnitTests
        foreach(var projectFile in Config.Projects.UnitTests) {
            DotNetRestore(projectFile.GetDirectory());
            DotNetTest(projectFile.GetDirectory(), configuration);
        }
    });

Task("Package")
    .Does(() => {
        //Build/Publish DotNet artifacts, build Docker image
        var artifactPath = System.IO.Path.GetFullPath(Config.Artifacts + "/DockerProxy-" + Config.Version.Semantic);
        DotNetPublish(Config.Projects.DockerProxy.GetDirectory(), artifactPath, Config.Version.Full, configuration);
    });

Task("Publish")
    .Does(() => {
    });

// DEVELOPER ENVIRONMENT TASKS

Task("Run")
    .Does(() => {
        DotNetRestore(Config.Projects.DockerProxy.GetDirectory());
        DotNetRun(Config.Projects.DockerProxy.GetDirectory());
    });

RunTarget(target);