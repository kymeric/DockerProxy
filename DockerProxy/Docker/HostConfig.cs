using System;
using System.Collections.Generic;
using System.Text;

namespace DockerProxy.Docker
{
    public class HostConfig
    {
        public Dictionary<string, PortBinding[]> PortBindings { get; set; }
    }
}