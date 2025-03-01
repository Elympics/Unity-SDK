using System;
using MatchTcpClients;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
    [Serializable]
    public class ClientConnectionSettings
    {
        [Tooltip("In seconds")] public float sessionConnectTimeout = 15;
        public int sessionConnectRetries = 5;
        [Tooltip("In seconds")] public float synchronizerTimeout = 20;
        [Tooltip("In seconds")] public float minContinuousSynchronizationInterval = 0.02f;
        [Tooltip("In seconds")] public float unreliablePingTimeout = 5;
        [Tooltip("In seconds")] public float webRtcOfferTimeout = 5;
        public int webRtcOfferMaxRetries = 5;
        [Tooltip("In seconds")] public float webRtcOfferRetryDelay = 1;
        public int initialSynchronizeMaxRetries = 3;

        public GameServerClientConfig GameServerClientConfig => new()
        {
            ClientSynchronizerConfig = new ClientSynchronizerConfig
            {
                TimeoutTime = TimeSpan.FromSeconds(synchronizerTimeout),
                ContinuousSynchronizationMinimumInterval = TimeSpan.FromSeconds(minContinuousSynchronizationInterval),
                UnreliablePingTimeoutInMilliseconds = TimeSpan.FromSeconds(unreliablePingTimeout)
            },
            SessionConnectTimeout = TimeSpan.FromSeconds(sessionConnectTimeout),
            SessionConnectRetries = sessionConnectRetries,
            OfferTimeout = TimeSpan.FromSeconds(webRtcOfferTimeout),
            OfferMaxRetries = webRtcOfferMaxRetries,
            OfferRetryDelay = TimeSpan.FromSeconds(webRtcOfferRetryDelay),
            InitialSynchronizeMaxRetries = initialSynchronizeMaxRetries
        };
    }
}
