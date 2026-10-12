using System;

namespace Proto.ProtoClient.NetworkClient
{
    public interface IProtoNetworkClient
    {
        void Send(byte[] data);
        void Receive();

        event Action<byte[]> Received;
        event Action<string> ReceivingError;
        event Action ReceivingEnded;
    }
}
