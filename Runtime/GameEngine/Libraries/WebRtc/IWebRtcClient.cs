using System;

namespace WebRtcWrapper
{
    public interface IWebRtcClient : IWebRtcCommunication, IDisposable
    {
        event Action<string> OfferCreated;

        event Action<string> IceCandidateCreated;

        void SetIceServers(string iceServersJson);
        void CreateOffer(bool restart);

        void OnAnswer(string answerJson);

        void ReceiveWithThread();

        /// <summary>
        /// Use to receive data without using internal thread (defined in ReceiveWithThread) ~pprzestrzelski 14.02.2023
        /// </summary>
        bool ReceiveReliableOnce();

        /// <summary>
        /// Use to receive data without using internal thread (defined in ReceiveWithThread) ~pprzestrzelski 14.02.2023
        /// </summary>
        bool ReceiveUnreliableOnce();

        void Close();
    }
}
