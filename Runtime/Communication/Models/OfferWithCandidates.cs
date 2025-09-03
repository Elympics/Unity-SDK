using System;
using JetBrains.Annotations;

namespace Elympics.Communication.Models
{
    [Serializable]
    [UsedImplicitly]
    internal struct OfferWithCandidates
    {
        public string offer;
        public string[] candidates;
    }
}
