using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DockerProxy.Docker;
using Serilog;
using Stateless;

namespace DockerProxy
{
    public class Proxy : IDisposable
    {
        private readonly StateMachine<State, Trigger> _machine;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Container> _openTrigger;
        private readonly CancellationTokenSource _cancellation;
        private readonly List<Task> _proxyTasks = new List<Task>();
        private readonly List<Task> _connectionTasks = new List<Task>();
        private readonly TaskCompletionSource<bool> _closed = new TaskCompletionSource<bool>();

        public Container Container { get; private set; }
        public Dictionary<string, PortBinding[]> PortBindings => Container?.HostConfig.PortBindings;
        public Task Closed => _closed.Task;

        private static readonly ILogger Logger = Log.ForContext<Proxy>();
        private long _sentBytes;
        private long _receivedBytes;

        public Proxy(CancellationToken shutdown)
        {
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(shutdown);
            _cancellation.Token.Register(() => _closed.SetCanceled());

            _machine = new StateMachine<State, Trigger>(State.Initial);
            _machine.OnTransitioned(transition => Logger.Debug("Transition: {0} -> ({1}) -> {2}", transition.Source, transition.Trigger, transition.Destination));
            _openTrigger = _machine.SetTriggerParameters<Container>(Trigger.Open);

            _machine
                .Configure(State.Initial)
                .Permit(Trigger.Open, State.Open);

            _machine.Configure(State.Open)
                .Permit(Trigger.Close, State.Closed)
                .OnEntryFrom(_openTrigger, OnOpen);

            _machine.Configure(State.Closed)
                .OnEntry(OnClose);
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            if (!Task.WaitAll(_proxyTasks.Union(_connectionTasks).ToArray(), 5000))
            {
                Logger.Warning(
                    "Error cleaning up proxy and connection tasks for {0}\r\nProxy Tasks:\r\n{1}\r\nConnection Tasks:\r\n{2}",
                    Container.Name,
                    String.Join(", ", _proxyTasks.Select(t => t.Status)),
                    String.Join(", ", _connectionTasks.Select(t => t.Status)));
            }
            else
            {
                Logger.Debug("Proxy completed receiving {0:0.00} MB and sending {1:0.00} MB", _receivedBytes / 1024.0 / 1024, _sentBytes / 1024.0 / 1024);
            }
        }

        void OnClose()
        {
            _closed.SetResult(true);
        }

        void OnOpen(Container container)
        {
            Container = container;
            //Only proxying TCP, need to support UDP?
            _proxyTasks.AddRange(PortBindings.Where(e => e.Key.EndsWith("/tcp"))
                .SelectMany(e =>
                {
                    var containerPort = int.Parse(e.Key.Split('/')[0]);
                    return e.Value.Select(pb => ProxyAsync(pb.HostPort, containerPort));
                }));
        }

        private async Task ProxyAsync(int hostPort, int containerPort)
        {
            //TODO: CONTAINER IP
            var nat = Container.NetworkSettings.Networks["nat"];

            var ip = nat.IPAddress;
            Logger.Information("Proxying - localhost:{0} -> {1}:{2}", hostPort, ip, containerPort);
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, hostPort));
                listener.Listen(5);
                while (!_cancellation.IsCancellationRequested)
                {
                    var client = await listener.AcceptAsync();
                    _connectionTasks.Add(HandleClientAsync(client, ip, containerPort));
                }
            }
        }

        private async Task HandleClientAsync(Socket client, string ip, int containerPort)
        {
            using (client)
            {
                using (var docker = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    await docker.ConnectAsync(ip, containerPort);
                    _cancellation.Token.Register(() =>
                    {
                        client.Dispose();
                        docker.Dispose();
                    });

                    var incoming = ProxyAsync(client, docker);
                    var outgoing = ProxyAsync(docker, client);

                    try
                    {
                        var result = await Task.WhenAll(incoming, outgoing);
                    }
                    catch (Exception)
                    {
                    }
                    _receivedBytes += incoming.Result;
                    _sentBytes += outgoing.Result;
                }
            }
        }

        private async Task<long> ProxyAsync(Socket from, Socket to)
        {
            long ret = 0;
            var buffer = new ArraySegment<byte>(new byte[16384], 0, 16384);
            try
            {
                while (from.Connected && to.Connected)
                {
                    var read = await from.ReceiveAsync(buffer, SocketFlags.None);
                    ret += read;
                    if (read == 0)
                        return ret;
                    await to.SendAsync(new ArraySegment<byte>(buffer.Array, 0, read), SocketFlags.None);
                }
            }
            catch (Exception)
            {
            }
            return ret;
        }

        public Task OpenAsync(Container container)
        {
            return _machine.FireAsync(_openTrigger, container);
        }

        public Task CloseAsync()
        {
            return _machine.FireAsync(Trigger.Close);
        }

        enum State
        {
            Initial,
            Open,
            Closed
        }

        enum Trigger
        {
            Open,
            Close
        }
    }
}