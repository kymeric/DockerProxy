
void DotNetBuild(DirectoryPath path, string version, string config = "Debug")
{
    ProcessExecute("dotnet", $"build --no-incremental -c {config} /p:Version={version}", path);
}

void DotNetTest(DirectoryPath path, string config = "Debug")
{
    ProcessExecute("dotnet", $"test -c {config}", path);
}

void DotNetPublish(DirectoryPath path, string output, string version, string config = "Debug")
{
    ProcessExecute("dotnet", $"publish -o {output} -c {config} /p:Version={version}", path);
}

void DotNetRun(DirectoryPath path, string config = "Debug")
{
    ProcessExecute("dotnet", $"run -c {config}", path);
}

void DotNetRestore(DirectoryPath path)
{
    ProcessExecute("dotnet", $"restore", path);
}