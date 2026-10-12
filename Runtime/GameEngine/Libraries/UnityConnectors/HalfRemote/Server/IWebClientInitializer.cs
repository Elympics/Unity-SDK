using System.Threading.Tasks;
using Elympics.Communication.Models;

namespace UnityConnectors.HalfRemote.Server
{
    internal interface IWebClientInitializer
    {
        Task<string> InitClientAndCreateAnswer(OfferWithCandidates offer);
    }
}
