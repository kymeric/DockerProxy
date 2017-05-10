using System;
using System.Collections.Generic;
using System.Text;

namespace DockerProxy.Docker
{
    public class NetworkSettings
    {
        public Dictionary<string, Network> Networks { get; set; }
    }
}