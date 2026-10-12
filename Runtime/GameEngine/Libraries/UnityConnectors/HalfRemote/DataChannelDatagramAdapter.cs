using System;
using MatchTcpLibrary.TransportLayer.Interfaces;
using Proto.ProtoClient.NetworkClient;

namespace UnityConnectors.HalfRemote
{
    internal class DataChannelDatagramAdapter : IDatagramCommunication
    {
        private readonly IDataChannel _channel;

        public DataChannelDatagramAdapter(IDataChannel channel) => _channel = channel;

        public event Action<byte[]> Received
        {
            add => _channel.DataReceived += value;
            remove => _channel.DataReceived -= value;
        }

        public event Action<string> ReceivingError
        {
            add => _channel.Error += value;
            remove => _channel.Error -= value;
        }

        public event Action ReceivingEnded
        {
            add => _channel.Disconnected += value;
            remove => _channel.Disconnected -= value;
        }

        public void Send(byte[] data) => _channel.Send(data);
    }
}
