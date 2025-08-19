using System;
namespace Elympics.Communication.Models
{
    [Serializable]
    internal struct SignalingResponse
    {
        public string answer;
        public string iceCandidatesRoute;
    }
}
