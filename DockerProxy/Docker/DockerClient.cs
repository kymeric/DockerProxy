using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace DockerProxy.Docker
{
    public class DockerClient
    {
        public static async Task<string> InspectAsync(string id)
        {
            return await DockerAsync($"inspect {id}");
        }

        public static async Task<Container[]> ContainerListAsync()
        {
            var containerIds = await DockerAsync("container ls -q");

            return await Task.WhenAll(containerIds
                .Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries)
                .Select(async containerId =>
                {
                    containerId = containerId.Trim();
                    var json = await InspectAsync(containerId);
                    var containers = JsonConvert.DeserializeObject<Container[]>(json);
                    if (containers.Length != 1)
                        throw new InvalidOperationException($"Error inspecting: {containerId}");
                    return containers[0];
                }));
        }

        private static Task<string> DockerAsync(string arguments)
        {
            var ret = new TaskCompletionSource<string>();

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo("docker", arguments)
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                },
                EnableRaisingEvents = true
            };
            var output = new StringBuilder();
            var error = new StringBuilder();
            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    output.AppendLine(args.Data);

            };
            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    error.AppendLine(args.Data);
            };
            if(!process.Start())
                throw new InvalidOperationException($"Unable to start process: docker {arguments}");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.Exited += (sender, args) =>
            {
                if (process.ExitCode != 0)
                {
                    ret.SetException(
                        new InvalidOperationException(
                            $"Error running process 'docker {arguments}': ExitCode {process.ExitCode}\r\n{error}"));
                }
                else
                {
                    var result = output.ToString().Trim();
                    Logger.Verbose("docker {0} = {1}", arguments, result);
                    ret.SetResult(result);
                }
            };

            return ret.Task;
        }

        private static readonly ILogger Logger = Log.ForContext<DockerClient>();
    }
}
