using System;

namespace Proto.ProtoClient.NetworkClient
{
    public interface IDatagramCommunication
    {
        event Action<byte[]> Received;
        event Action<string> ReceivingError;
        event Action ReceivingEnded;

        void Send(byte[] data);
    }
}
