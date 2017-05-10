using System;
using System.Collections.Generic;
using System.Text;

namespace DockerProxy.Docker
{
    public class Container
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public string Image { get; set; }
        public HostConfig HostConfig { get; set; }
        public ContainerConfig Config { get; set; }
        public NetworkSettings NetworkSettings { get; set; }

        public override string ToString()
        {
            return $"{Name} ({String.Join(", ", HostConfig.PortBindings.Keys)})";
        }
    }
}