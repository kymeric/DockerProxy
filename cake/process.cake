int ProcessExecute(string command, string args, DirectoryPath workingDirectory = null)
{
    var settings = new ProcessSettings {
        Arguments = args,
        WorkingDirectory = workingDirectory
    };
    Information($"Executing:\t{command} {args}\r\n");
    var ret = StartProcess(command, settings);
    if(ret != 0)
        throw new InvalidOperationException($"Failed to execute: {command} {args}");
    return ret;
}

IProcess ProcessStart(string command, string args, DirectoryPath workingDirectory = null)
{
    var settings = new ProcessSettings {
        Arguments = args,
        WorkingDirectory = workingDirectory
    };
    Information($"Starting:\t{command} {args}\r\n");
    return StartAndReturnProcess(command, settings);
}