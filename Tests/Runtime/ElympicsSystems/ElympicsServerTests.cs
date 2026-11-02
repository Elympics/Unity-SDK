using System;
using System.Collections.Generic;
using System.Reflection;
using Elympics.ElympicsSystems;
using Elympics.Replication;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Elympics.Tests
{
    [TestFixture]
    [Category("Core")]
    public class ElympicsServerTests
    {
        private const int MaxPlayers = 2;

        private GameObject _serverObject;
        private TickableElympicsServer _server;
        private RecordingServerUpdateLoop _updateLoop;
        private List<int> _submittedPlayers;

        [SetUp]
        public void SetUp()
        {
            ElympicsWorld.Current = new ElympicsWorld(MaxPlayers);
            _serverObject = new GameObject("Elympics Server", typeof(TickableElympicsServer), typeof(ElympicsBehavioursManager), typeof(ElympicsFactory));
            _server = _serverObject.GetComponent<TickableElympicsServer>();
            var behavioursManager = _serverObject.GetComponent<ElympicsBehavioursManager>();
            behavioursManager.factory = _serverObject.GetComponent<ElympicsFactory>();
            var config = ScriptableObject.CreateInstance<ElympicsGameConfig>();
            config.maxPlayers = MaxPlayers;
            _updateLoop = new RecordingServerUpdateLoop();
            _server.InitializeInternal(config, null!, behavioursManager, new NullServerPlayerHandler(), null!, _updateLoop);
            behavioursManager.InitializeInternal(_server, MaxPlayers);
            _server.MarkInitialized();
            _submittedPlayers = new List<int>();
            _server.Score.PlayerScoreUpdated += (playerIndex, _, _, _, _) => _submittedPlayers.Add(playerIndex);
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            Object.DestroyImmediate(_serverObject);
        }

        [Test]
        public void TicksAreFinalizedWhenScoreIsNeverEnabled()
        {
            for (var i = 0; i < 3; i++)
                Assert.DoesNotThrow(_server.RunTick);

            Assert.That(_updateLoop.FinalizedTicks, Is.EqualTo(3));
            Assert.That(_submittedPlayers, Is.Empty);
        }

        [Test]
        public void ScoreEnabledBeforeTheStartIsSubmittedForAllPlayersAtTheStart()
        {
            _server.RunTick();
            _server.Score.Enable();
            _server.Score[1] = 5f;

            _server.RunTick();
            Assert.That(_submittedPlayers, Is.Empty);

            for (var i = 0; i < 100 && !_server.IsMatchStarted; i++)
                _server.RunTick();
            _server.RunTick();

            Assert.That(_server.IsMatchStarted, Is.True);
            Assert.That(_submittedPlayers, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void MatchStartIsScheduledOnlyOnceAllHumanPlayersAreReady()
        {
            SetHumanPlayers(0, 1);

            _server.RunTick();
            Assert.That(_server.StartTick, Is.Null);

            MarkPlayerReady(0);
            _server.RunTick();
            Assert.That(_server.StartTick, Is.Null, "One of two players is ready.");

            MarkPlayerReady(1);
            var tickOfScheduling = _server.Tick;
            _server.RunTick();

            Assert.That(_server.StartTick, Is.EqualTo(tickOfScheduling + _server.Config.MatchStartDelayTicks));
        }

        [Test]
        public void MatchStartIsNotRescheduledOnceSet()
        {
            SetHumanPlayers(0);
            MarkPlayerReady(0);
            _server.RunTick();
            var startTick = _server.StartTick;

            _server.RunTick();
            _server.RunTick();

            Assert.That(_server.StartTick, Is.EqualTo(startTick));
        }

        [Test]
        public void ReadyTimeoutEndsTheMatchWithoutResult()
        {
            // The ready timeout is wall-clock based (min. 180 s), so the server init time is moved back instead of waiting.
            LogAssert.ignoreFailingMessages = true;
            SetHumanPlayers(0, 1);
            MarkPlayerReady(0);
            SetInitializedUtc(DateTime.UtcNow.AddSeconds(-(_server.Config.ReadyTimeoutSeconds + 1)));

            _server.RunTick();

            Assert.That(_server.StartTick, Is.Null);
            Assert.That(_server.Initialized, Is.False, "The match should have ended.");
            Assert.That(InvokeResolveMatchResult(new ResultMatchPlayerDatas()), Is.Null, "A match that never started has no result.");
        }

        [Test]
        public void ReadyTimeoutDoesNotEndTheMatchEarly()
        {
            SetHumanPlayers(0, 1);
            SetInitializedUtc(DateTime.UtcNow.AddSeconds(-(_server.Config.ReadyTimeoutSeconds - 60)));

            _server.RunTick();

            Assert.That(_server.Initialized, Is.True);
            Assert.That(_server.StartTick, Is.Null);
        }

        [Test]
        public void MatchStartHandlerThrowingDoesNotStopOtherHandlers()
        {
            LogAssert.ignoreFailingMessages = true;
            var handlerObject = new GameObject("Handlers", typeof(ElympicsBehaviour));
            handlerObject.transform.SetParent(_serverObject.transform);
            var before = handlerObject.AddComponent<RecordingMatchStartHandler>();
            _ = handlerObject.AddComponent<ThrowingMatchStartHandler>();
            var after = handlerObject.AddComponent<RecordingMatchStartHandler>();
            var behaviour = handlerObject.GetComponent<ElympicsBehaviour>();
            behaviour.InitializeInternal(_server);

            Assert.DoesNotThrow(() => behaviour.OnMatchStarted(7));

            Assert.That(before.StartTicks, Is.EqualTo(new[] { 7L }));
            Assert.That(after.StartTicks, Is.EqualTo(new[] { 7L }));
        }

        [Test]
        public void PlayerRpcSentBeforeStartTickIsIgnoredAndLaterOnesAreNot()
        {
            _server.RunTick();
            var startTick = _server.StartTick!.Value;

            Assert.That(_server.ShouldIgnorePlayerRpc(CreateRpc(sender: 0, sentOnTick: startTick - 1)), Is.True);
            Assert.That(_server.ShouldIgnorePlayerRpc(CreateRpc(sender: 0, sentOnTick: startTick)), Is.False);
            Assert.That(_server.ShouldIgnorePlayerRpc(CreateRpc(sender: 1, sentOnTick: startTick + 5)), Is.False);
        }

        [Test]
        public void WorldRpcIsNeverIgnored()
        {
            _server.RunTick();
            var startTick = _server.StartTick!.Value;

            Assert.That(_server.ShouldIgnorePlayerRpc(CreateRpc(sender: -1, sentOnTick: startTick - 1)), Is.False);
            Assert.That(_server.ShouldIgnorePlayerRpc(CreateRpc(sender: -1, sentOnTick: 0)), Is.False);
        }

        [Test]
        public void PlayerRpcIsIgnoredWhileTheStartIsNotScheduled()
        {
            SetHumanPlayers(0);
            _server.RunTick();

            Assert.That(_server.StartTick, Is.Null);
            Assert.That(_server.ShouldIgnorePlayerRpc(CreateRpc(sender: 0, sentOnTick: 100)), Is.True);
        }

        private static ElympicsRpcMessage CreateRpc(int sender, long sentOnTick) => new() { Sender = sender, SentOnTick = sentOnTick };

        private void SetHumanPlayers(params int[] playerIndices)
        {
            var humans = (HashSet<ElympicsPlayer>)GetServerField("_humanPlayers").GetValue(_server)!;
            foreach (var index in playerIndices)
                _ = humans.Add(ElympicsPlayer.FromIndex(index));
            SetInitializedUtc(DateTime.UtcNow);
        }

        // The ready callback is queued to the main thread, so its effect is applied directly.
        private void MarkPlayerReady(int playerIndex)
        {
            var ready = (HashSet<ElympicsPlayer>)GetServerField("_readyPlayers").GetValue(_server)!;
            _ = ready.Add(ElympicsPlayer.FromIndex(playerIndex));
        }

        private void SetInitializedUtc(DateTime utc) => GetServerField("_initializedUtc").SetValue(_server, utc);

        private ResultMatchPlayerDatas? InvokeResolveMatchResult(ResultMatchPlayerDatas result) =>
            (ResultMatchPlayerDatas?)typeof(ElympicsServer).GetMethod("ResolveMatchResult", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_server, new object[] { result });

        private static FieldInfo GetServerField(string name) => typeof(ElympicsServer).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;

        private class RecordingMatchStartHandler : MonoBehaviour, IMatchStartHandler
        {
            public List<long> StartTicks { get; } = new();
            public void OnMatchStarted(long startTick) => StartTicks.Add(startTick);
        }

        private class ThrowingMatchStartHandler : MonoBehaviour, IMatchStartHandler
        {
            public void OnMatchStarted(long startTick) => throw new InvalidOperationException("Handler failure expected by the test");
        }

        private class TickableElympicsServer : ElympicsServer
        {
            public void MarkInitialized()
            {
                CreateScore(MaxPlayers);
                SetInitialized();
            }

            public void RunTick()
            {
                ElympicsFixedUpdate();
                // Done by DefaultServerElympicsUpdateLoop in a real match.
                NotifyMatchStartedIfReached();
                ElympicsLateFixedUpdate();
            }
        }

        private class RecordingServerUpdateLoop : IServerElympicsUpdateLoop
        {
            public int FinalizedTicks { get; private set; }
            public long Tick => FinalizedTicks;
            public ElympicsSnapshot GenerateSnapshot() => new(Tick, default, new FactoryState(new Dictionary<int, FactoryPartState>()), new Dictionary<int, byte[]>(), null);
            public void FinalizeTick(ElympicsSnapshot snapshot) => FinalizedTicks++;
            public void HandleRenderFrame(in RenderData renderData) { }
        }
    }
}
