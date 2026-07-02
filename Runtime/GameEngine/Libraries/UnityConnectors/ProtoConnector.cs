using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GameBotCore.V1._1;
using GameEngineCore;
using Proto.ProtoClient;
using ProtoLog;

namespace UnityConnectors
{
    internal class ProtoConnector : IGameEngineLogger, IGameBotLogger, IDisposable
    {
        private readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(1);

        private readonly IPAddress _address;
        private readonly int _port;
        private readonly Func<TcpClient, ProtoClient> _protoClientFactory;

        private ProtoClient _client;
        private TcpClient _tcpClient;
        private DateTime _lastUpdateFromGameServer;

        public ProtoConnector(IPAddress address, int port, Func<TcpClient, ProtoClient> protoClientFactory)
        {
            _address = address;
            _port = port;
            _protoClientFactory = protoClientFactory;
        }

        public void Connect() => Task.Run(ConnectAsync);

        public async Task ConnectAsync()
        {
            Console.WriteLine($"{nameof(ProtoConnector)} connecting to {_address}:{_port}");

            var timeElapsed = TimeSpan.Zero;
            var counter = 0;
            Console.WriteLine($"{nameof(ProtoConnector)} before while");
            while (timeElapsed < _connectionTimeout)
            {
                counter++;
                Console.WriteLine($"{nameof(ProtoConnector)} connection attempt {counter}");
                try
                {
                    _tcpClient = new TcpClient();
                    Console.WriteLine($"{nameof(ProtoConnector)} trying to connect");
                    _tcpClient.Connect(new IPEndPoint(_address, _port));
                    Console.WriteLine($"{nameof(ProtoConnector)} creating client using factory");
                    _client = _protoClientFactory.Invoke(_tcpClient);
                    Console.WriteLine($"{nameof(ProtoConnector)} starting connection checker");
                    RunConnectionChecker();
                    Console.WriteLine($"{nameof(ProtoConnector)} connected to {_address}:{_port}");
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{nameof(ProtoConnector)} error while connecting:\n{e}\n");
                    await Task.Delay(_checkInterval);
                    timeElapsed += _checkInterval;
                }

                Console.WriteLine($"{nameof(ProtoConnector)} not connected, retrying in {_checkInterval}");
            }
            Console.WriteLine($"{nameof(ProtoConnector)} after while");
        }

        public void UpdateLastUpdateFromGameServer() => _lastUpdateFromGameServer = DateTime.Now;

        private void RunConnectionChecker()
        {
            UpdateLastUpdateFromGameServer();
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var intervalSinceLastUpdate = DateTime.Now - _lastUpdateFromGameServer;
                    if (intervalSinceLastUpdate > _connectionTimeout)
                    {
                        Dispose();
                        return;
                    }

                    await Task.Delay(_checkInterval);
                }
            });
        }

        public void Verbose(string message, params object[] arguments) => _client.Send(new LogVerboseMsg { Log = string.Format(message, arguments) });
        public void Debug(string message, params object[] arguments) => _client.Send(new LogDebugMsg { Log = string.Format(message, arguments) });
        public void Info(string message, params object[] arguments) => _client.Send(new LogInfoMsg { Log = string.Format(message, arguments) });
        public void Warning(string message, params object[] arguments) => _client.Send(new LogWarningMsg { Log = string.Format(message, arguments) });
        public void Warning(string message, Exception exception, params object[] arguments) => _client.Send(new LogWarningMsg { Log = string.Format(message, arguments) + Environment.NewLine + exception });
        public void Error(string message, params object[] arguments) => _client.Send(new LogErrorMsg { Log = string.Format(message, arguments) });
        public void Error(string message, Exception exception, params object[] arguments) => _client.Send(new LogErrorMsg { Log = string.Format(message, arguments) + Environment.NewLine + exception });
        public void Fatal(string message, params object[] arguments) => _client.Send(new LogFatalMsg { Log = string.Format(message, arguments) });
        public void Fatal(string message, Exception exception, params object[] arguments) => _client.Send(new LogFatalMsg { Log = string.Format(message, arguments) + Environment.NewLine + exception });

        public void Dispose()
        {
            _tcpClient?.Dispose();
        }
    }
}
