using System;
using MatchTcpClients.Synchronizer;

namespace MatchTcpClients
{
    public struct GameServerClientConfig
    {
        public ClientSynchronizerConfig ClientSynchronizerConfig { get; set; }
        public TimeSpan SessionConnectTimeout { get; set; }
        public TimeSpan OfferTimeout { get; set; }
        public int OfferMaxRetries { get; set; }
        public TimeSpan OfferRetryDelay { get; set; }
        public int InitialSynchronizeMaxRetries { get; set; }
    }
}
