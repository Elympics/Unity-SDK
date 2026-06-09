#nullable enable
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using MatchTcpLibrary.TransportLayer.Interfaces;

namespace MatchTcpLibrary.TransportLayer.TcpUdp
{
    internal sealed class TcpUdpNetworkClient : INetworkClient
    {
        public event Action? Connected;
        public event Action<string>? ChannelOpened;
        public event Action<string, byte[]>? DataReceived;
        public event Action? Disconnected;

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected == value)
                    return;
                _isConnected = value;
                if (!value)
                    Disconnected?.Invoke();
            }
        }

        public IReadOnlyDictionary<string, IDataChannel> Channels => _channels;
        private readonly Dictionary<string, IDataChannel> _channels = new();

        private readonly IPEndPoint _endpoint;
        private readonly IReadOnlyList<(string Label, Func<IDataChannel> Factory)> _channelSpecs;

        public TcpUdpNetworkClient(IPEndPoint endpoint, IReadOnlyList<(string Label, Func<IDataChannel> Factory)> channelSpecs)
        {
            _endpoint = endpoint;
            _channelSpecs = channelSpecs;
        }

        public void CreateAndBind()
        {
            foreach (var channel in _channels.Values)
                channel.Dispose();
            _channels.Clear();

            foreach (var spec in _channelSpecs)
            {
                var channel = spec.Factory();
                channel.DataReceived += data => DataReceived?.Invoke(spec.Label, data);
                channel.Disconnected += RaiseDisconnected;
                channel.CreateAndBind();
                _channels[spec.Label] = channel;
            }
        }

        public async UniTask Connect(CancellationToken ct = default)
        {
            foreach (var spec in _channelSpecs)
                await _channels[spec.Label].ConnectAsync(_endpoint, ct);
            IsConnected = true;
            Connected?.Invoke();
        }

        private void RaiseDisconnected() => IsConnected = false;

        public void Disconnect()
        {
            foreach (var channel in _channels.Values)
                channel.Disconnect();
            IsConnected = false;
        }

        public void Dispose() => Disconnect();
    }
}
