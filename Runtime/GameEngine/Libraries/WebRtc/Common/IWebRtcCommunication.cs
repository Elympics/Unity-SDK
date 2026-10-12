using System;

namespace WebRtcWrapper
{
    public interface IWebRtcCommunication
    {
        event Action<byte[]> ReliableReceived;
        event Action<string> ReliableReceivingError;
        event Action ReliableReceivingEnded;

        event Action<byte[]> UnreliableReceived;
        event Action<string> UnreliableReceivingError;
        event Action UnreliableReceivingEnded;

        event Action<string> IceConnectionStateChanged;
        event Action<string> ConnectionStateChanged;

        void SendReliable(byte[] data);
        void SendUnreliable(byte[] data);
    }
}
