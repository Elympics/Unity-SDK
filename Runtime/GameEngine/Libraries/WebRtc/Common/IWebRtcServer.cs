using System;

namespace WebRtcWrapper
{
    public interface IWebRtcServer : IDisposable
    {
        void Start(bool withReceiveThread = true);
        void Stop();
        IWebRtcServerClient CreateClient();

        /// <summary>
        /// Use to receive data without using internal thread (defined in Start) ~pprzestrzelski 26.09.2022
        /// </summary>
        void ReceiveReliableOnce();

        /// <summary>
        /// Use to receive data without using internal thread (defined in Start) ~pprzestrzelski 26.09.2022
        /// </summary>
        void ReceiveUnreliableOnce();
    }
}
