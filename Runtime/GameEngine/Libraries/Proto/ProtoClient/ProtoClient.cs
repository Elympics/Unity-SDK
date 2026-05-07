using System;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Proto.ProtoClient.NetworkClient;

namespace Proto.ProtoClient
{
    internal abstract class ProtoClient
    {
        public event Action<string> ReceivingError;
        public event Action ReceivingEnded;

        private readonly IProtoNetworkClient _networkClient;

        protected ProtoClient(IProtoNetworkClient networkClient)
        {
            Il2Cpp.ForceAotFix();
            _networkClient = networkClient;
        }

        public void Send(IMessage message)
        {
            var packed = Any.Pack(message).ToByteArray();
            _networkClient.Send(packed);
        }

        public void Receive()
        {
            _networkClient.Received += OnReceived;
            _networkClient.ReceivingError += OnReceivingError;
            _networkClient.ReceivingEnded += OnReceivingEnded;
            _networkClient.Receive();
        }


        private void OnReceived(byte[] data)
        {
            var message = Any.Parser.ParseFrom(data);
            OnMessage(message);
        }

        private void OnReceivingError(string error) => ReceivingError?.Invoke(error);
        private void OnReceivingEnded() => ReceivingEnded?.Invoke();

        protected abstract void OnMessage(Any message);
    }
}
