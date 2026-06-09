#nullable enable
using System;
using Cysharp.Threading.Tasks;
using Elympics.GameEngine.Libraries.WebRtc;
using MatchTcpLibrary.TransportLayer.Interfaces;

namespace WebRtcWrapper
{
    internal interface IWebRtcClient : IDisposable
    {
        event Action<string>? IceCandidateCreated;

        event Action<(IceCandidateStats LocalCandidate, IceCandidateStats RemoteCandidate)>? CandidatePairChosen;

        event Action<string>? IceConnectionStateChanged;
        event Action<string>? ConnectionStateChanged;

        void SetIceServers(string iceServersJson);

        /// <summary>
        /// Creates a data channel with the given label.
        /// </summary>
        /// <remarks>
        /// Must be called before <see cref="CreateOffer"/> as the
        /// signaling protocol is a single offer/answer exchange with no renegotiation. This means every channel
        /// must be part of the initial SDP.
        /// </remarks>
        IDataChannel CreateDataChannel(string label, bool reliable);

        UniTask<string> CreateOffer(bool restart);

        UniTask OnAnswer(string answerJson);

        void Close();
    }
}
