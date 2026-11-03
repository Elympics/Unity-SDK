using System;
using System.Threading;
using Elympics.Communication.Models.Public;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Replication;
using MatchTcpClients.Synchronizer;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Elympics.Tests
{
    [TestFixture]
    [Category("Core")]
    public class ElympicsClientReadyTests
    {
        private const int MaxPlayers = 2;

        private GameObject _clientObject;
        private ElympicsClient _client;
        private FakeMatchClient _matchClient;

        [SetUp]
        public void SetUp()
        {
            ElympicsWorld.Current = new ElympicsWorld(MaxPlayers);
            _clientObject = new GameObject("Elympics Client", typeof(ElympicsClient), typeof(ElympicsBehavioursManager), typeof(ElympicsFactory));
            _client = _clientObject.GetComponent<ElympicsClient>();
            var behavioursManager = _clientObject.GetComponent<ElympicsBehavioursManager>();
            behavioursManager.factory = _clientObject.GetComponent<ElympicsFactory>();
            var config = ScriptableObject.CreateInstance<ElympicsGameConfig>();
            config.maxPlayers = MaxPlayers;
            _matchClient = new FakeMatchClient();
            var playerData = new InitialMatchPlayerDataGuid { Player = ElympicsPlayer.FromIndex(0) };
            _client.InitializeInternal(config, new PendingMatchConnectClient(), _matchClient, playerData, behavioursManager, MaxPlayers);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_clientObject);

        [Test]
        public void ReadyIsNotSentUntilMarkReadyIsCalled()
        {
            _client.SendReadyIfPending();

            Assert.That(_matchClient.SendReadyCalls, Is.EqualTo(0));
        }

        [Test]
        public void ReadyIsSentOnceAfterMarkReady()
        {
            _client.MarkReady();

            _client.SendReadyIfPending();
            _client.SendReadyIfPending();

            Assert.That(_matchClient.SendReadyCalls, Is.EqualTo(1));
        }

        [Test]
        public void ReadyIsSentAgainOnTheNextTickWhenSendingThrows()
        {
            _client.MarkReady();
            _matchClient.ThrowOnNextSendReady = true;

            _ = Assert.Throws<InvalidOperationException>(_client.SendReadyIfPending);
            _client.SendReadyIfPending();
            _client.SendReadyIfPending();

            Assert.That(_matchClient.SendReadyCalls, Is.EqualTo(2));
        }

        private class FakeMatchClient : IMatchClient
        {
            public int SendReadyCalls { get; private set; }
            public bool ThrowOnNextSendReady { get; set; }

#pragma warning disable CS0067 // The client only subscribes to these.
            public event Action<TimeSynchronizationData> Synchronized;
            public event Action<ElympicsSnapshot> SnapshotReceived;
            public event Action<ElympicsRpcMessageList> RpcMessageListReceived;
#pragma warning restore CS0067

            public void SendReady()
            {
                SendReadyCalls++;
                if (!ThrowOnNextSendReady)
                    return;
                ThrowOnNextSendReady = false;
                throw new InvalidOperationException("Simulated send failure.");
            }

            public void AddInputToSendBuffer(ElympicsInput input) { }
            public void SendRpcMessageList(ElympicsRpcMessageList rpcMessageList, bool reliable) { }
            public void SendBufferInput(long tick) { }
            public void SetLastReceivedSnapshot(long tick) { }
            public void Dispose() { }
        }

        /// <summary>Never finishes connecting, so the client started by <see cref="ElympicsClient.InitializeInternal"/> stays idle.</summary>
        private class PendingMatchConnectClient : IMatchConnectClient
        {
#pragma warning disable CS0067 // The client only subscribes to these.
            public event Action<TimeSynchronizationData> ConnectedWithSynchronizationData;
            public event Action ConnectingFailed;
            public event Action<Guid> AuthenticatedUserMatchWithUserId;
            public event Action<string> AuthenticatedUserMatchFailedWithError;
            public event Action AuthenticatedAsSpectator;
            public event Action<string> AuthenticatedAsSpectatorWithError;
            public event Action<string> MatchJoinedWithError;
            public event Action<MatchInitialData> MatchJoinedWithMatchInitData;
            public event Action<Guid> MatchEndedWithMatchId;
            public event Action DisconnectedByServer;
            public event Action DisconnectedByClient;
#pragma warning restore CS0067

            public UniTask ConnectAndJoinAsPlayerAsync(CancellationToken ct) => UniTask.Never(ct);
            public UniTask ConnectAndJoinAsSpectatorAsync(CancellationToken ct) => UniTask.Never(ct);
            public void Disconnect() { }
            public void Dispose() { }
        }
    }
}
