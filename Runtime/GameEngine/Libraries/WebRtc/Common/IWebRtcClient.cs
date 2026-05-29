using System;
using Cysharp.Threading.Tasks;
using Elympics.GameEngine.Libraries.WebRtc;

namespace WebRtcWrapper
{
    internal interface IWebRtcClient : IWebRtcCommunication, IDisposable
    {
        event Action<string> IceCandidateCreated;

        event Action ReliableChannelOpened;
        event Action UnreliableChannelOpened;

        event Action<(IceCandidateStats LocalCandidate, IceCandidateStats RemoteCandidate)> CandidatePairChosen;

        void SetIceServers(string iceServersJson);

        UniTask<string> CreateOffer(bool restart);

        UniTask OnAnswer(string answerJson);

        void Close();
    }
}
