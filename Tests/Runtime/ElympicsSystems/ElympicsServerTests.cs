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

            for (var i = 0; i < 100 && !_server.IsGameStarted; i++)
                _server.RunTick();
            _server.RunTick();

            Assert.That(_server.IsGameStarted, Is.True);
            Assert.That(_submittedPlayers, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void GameStartIsScheduledOnlyOnceAllHumanPlayersAreReady()
        {
            SetHumanPlayers(0, 1);

            _server.RunTick();
            Assert.That(_server.GameStartTick, Is.Null);

            MarkPlayerReady(0);
            _server.RunTick();
            Assert.That(_server.GameStartTick, Is.Null, "One of two players is ready.");

            MarkPlayerReady(1);
            var tickOfScheduling = _server.Tick;
            _server.RunTick();

            Assert.That(_server.GameStartTick, Is.EqualTo(tickOfScheduling + _server.Config.GameStartDelayTicks));
        }

        [Test]
        public void GameStartIsNotRescheduledOnceSet()
        {
            SetHumanPlayers(0);
            MarkPlayerReady(0);
            _server.RunTick();
            var startTick = _server.GameStartTick;

            _server.RunTick();
            _server.RunTick();

            Assert.That(_server.GameStartTick, Is.EqualTo(startTick));
        }

        // The timeouts are wall-clock based (min. 30 s), so the reference times are moved back instead of waiting.
        [Test]
        public void ReadyTimeoutFromTheFirstConnectionEndsTheMatchWithoutResult()
        {
            LogAssert.ignoreFailingMessages = true;
            SetHumanPlayers(0, 1);
            MarkPlayerReady(0);
            _server.FirstHumanConnectedUtc = DateTime.UtcNow.AddSeconds(-(_server.Config.ReadyTimeoutSeconds + 1));

            _server.RunTick();

            Assert.That(_server.GameStartTick, Is.Null);
            Assert.That(_server.Initialized, Is.False, "The match should have ended.");
            Assert.That(_server.ResolveMatchResult(new ResultMatchPlayerDatas()), Is.Null, "A match that never started has no result.");
        }

        [Test]
        public void ReadyTimeoutIsCountedFromTheFirstConnectionNotFromServerInit()
        {
            SetHumanPlayers(0, 1);
            _server.InitializedUtc = DateTime.UtcNow.AddSeconds(-(_server.Config.ReadyTimeoutSeconds + 1));
            _server.FirstHumanConnectedUtc = DateTime.UtcNow;

            _server.RunTick();

            Assert.That(_server.Initialized, Is.True);
            Assert.That(_server.GameStartTick, Is.Null);
        }

        [Test]
        public void NoPlayerTimeoutEndsTheMatchWhenNoPlayerConnects()
        {
            LogAssert.ignoreFailingMessages = true;
            SetHumanPlayers(0, 1);
            _server.InitializedUtc = DateTime.UtcNow.AddSeconds(-(_server.Config.NoPlayerTimeoutSeconds + 1));

            _server.RunTick();

            Assert.That(_server.Initialized, Is.False, "The match should have ended.");
        }

        [Test]
        public void ReadyTimeoutDoesNotApplyBeforeTheFirstConnection()
        {
            SetHumanPlayers(0, 1);
            _server.InitializedUtc = DateTime.UtcNow.AddSeconds(-(_server.Config.ReadyTimeoutSeconds + 1));
            Assume.That(_server.Config.ReadyTimeoutSeconds + 1, Is.LessThan(_server.Config.NoPlayerTimeoutSeconds));

            _server.RunTick();

            Assert.That(_server.Initialized, Is.True);
        }

        [Test]
        public void GameStartUtcIsTheStartTimeOfTheStartTick()
        {
            var startUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _server.RunTick();
            var startTick = _server.GameStartTick!.Value;
            while (_server.Tick < startTick)
            {
                Assert.That(_server.GameStartUtc, Is.Null);
                _server.RunTick();
            }

            _server.TickStartUtc = startUtc;
            _server.RunTick();

            Assert.That(_server.GameStartUtc, Is.EqualTo(startUtc));
        }

        [TestCase(TerminationOption.Any, new[] { 1 })]
        [TestCase(TerminationOption.All, new[] { 0, 1 })]
        public void LeavingPlayersEndTheMatchImmediatelyWithoutRejoinTimeout(TerminationOption option, int[] leaving)
        {
            StartGameWithConnectedPlayers(option, rejoinTimeoutSeconds: 0f);

            Disconnect(leaving);
            _server.RunTick();

            Assert.That(_server.Initialized, Is.False, "The match should have ended.");
        }

        [Test]
        public void AllOptionKeepsTheMatchWhileOnePlayerIsConnected()
        {
            StartGameWithConnectedPlayers(TerminationOption.All, rejoinTimeoutSeconds: 0f);

            Disconnect(1);
            RunTicks(5);

            Assert.That(_server.Initialized, Is.True, "One player is still connected.");
        }

        [Test]
        public void NoneOptionNeverEndsTheMatchWhenPlayersLeave()
        {
            StartGameWithConnectedPlayers(TerminationOption.None, rejoinTimeoutSeconds: 0f);

            Disconnect(0, 1);
            RunTicks(5);

            Assert.That(_server.Initialized, Is.True);
        }

        [Test]
        public void RejoinTimeoutEndsTheMatchAtItsDeadline()
        {
            LogAssert.ignoreFailingMessages = true;
            StartGameWithConnectedPlayers(TerminationOption.Any, rejoinTimeoutSeconds: 1f);
            var timeoutTicks = _server.Config.RejoinTimeoutTicks;

            Disconnect(1);
            RunTicks((int)timeoutTicks);
            Assert.That(_server.Initialized, Is.True, "The rejoin timeout hasn't passed yet.");

            _server.RunTick();
            Assert.That(_server.Initialized, Is.False, "The match should have ended.");
        }

        [Test]
        public void RejoiningBeforeTheDeadlineKeepsTheMatchAndResetsTheTimeout()
        {
            LogAssert.ignoreFailingMessages = true;
            StartGameWithConnectedPlayers(TerminationOption.Any, rejoinTimeoutSeconds: 1f);
            var timeoutTicks = (int)_server.Config.RejoinTimeoutTicks;

            Disconnect(1);
            RunTicks(timeoutTicks - 1);
            Connect(1);
            RunTicks(timeoutTicks + 5);
            Assert.That(_server.Initialized, Is.True, "The player rejoined in time.");

            Disconnect(1);
            RunTicks(timeoutTicks);
            Assert.That(_server.Initialized, Is.True, "The timeout restarts when players leave again.");
        }

        [Test]
        public void GameStartHandlerThrowingDoesNotStopOtherHandlers()
        {
            LogAssert.ignoreFailingMessages = true;
            var handlerObject = new GameObject("Handlers", typeof(ElympicsBehaviour));
            handlerObject.transform.SetParent(_serverObject.transform);
            var before = handlerObject.AddComponent<RecordingGameStartHandler>();
            _ = handlerObject.AddComponent<ThrowingGameStartHandler>();
            var after = handlerObject.AddComponent<RecordingGameStartHandler>();
            var behaviour = handlerObject.GetComponent<ElympicsBehaviour>();
            behaviour.InitializeInternal(_server);

            Assert.DoesNotThrow(() => behaviour.OnGameStarted(7));

            Assert.That(before.StartTicks, Is.EqualTo(new[] { 7L }));
            Assert.That(after.StartTicks, Is.EqualTo(new[] { 7L }));
        }

        [TestCase(0, -1, true, Description = "A player RPC sent before the start tick")]
        [TestCase(0, 0, false, Description = "A player RPC sent at the start tick")]
        [TestCase(1, 5, false, Description = "A player RPC sent after the start tick")]
        [TestCase(-1, -1, false, Description = "A world RPC sent before the start tick")]
        public void PlayerRpcIsIgnoredOnlyWhenSentBeforeTheStartTick(int sender, long ticksAfterStart, bool expectedIgnored)
        {
            _server.RunTick();
            var startTick = _server.GameStartTick!.Value;

            Assert.That(_server.ShouldIgnorePlayerRpc(CreateRpc(sender, startTick + ticksAfterStart)), Is.EqualTo(expectedIgnored));
        }

        [TestCase(-2, -1, true, Description = "A local player RPC sent from outside input gathering before the start tick")]
        [TestCase(-2, 0, false, Description = "A local player RPC sent from outside input gathering at the start tick")]
        [TestCase(0, -1, true, Description = "A local player RPC sent from input gathering before the start tick")]
        public void LocalPlayerRpcIsIgnoredWhenSentBeforeTheStartTickWhateverTheSender(int sender, long ticksAfterStart, bool expectedIgnored)
        {
            _server.RunTick();
            var startTick = _server.GameStartTick!.Value;

            Assert.That(_server.ShouldIgnoreLocalPlayerRpc(CreateRpc(sender, startTick + ticksAfterStart)), Is.EqualTo(expectedIgnored));
        }

        [Test]
        public void PlayerRpcIsIgnoredWhileTheStartIsNotScheduled()
        {
            SetHumanPlayers(0);
            _server.RunTick();

            Assert.That(_server.GameStartTick, Is.Null);
            Assert.That(_server.ShouldIgnorePlayerRpc(CreateRpc(sender: 0, sentOnTick: 100)), Is.True);
        }

        private static ElympicsRpcMessage CreateRpc(int sender, long sentOnTick) => new() { Sender = sender, SentOnTick = sentOnTick };

        private void SetHumanPlayers(params int[] playerIndices)
        {
            foreach (var index in playerIndices)
                _ = _server.HumanPlayers.Add(ElympicsPlayer.FromIndex(index));
            _server.InitializedUtc = DateTime.UtcNow;
        }

        // The connection and ready callbacks are queued to the main thread, so their effects are applied directly.
        private void MarkPlayerReady(int playerIndex) => _ = _server.ReadyPlayers.Add(ElympicsPlayer.FromIndex(playerIndex));

        private void Connect(params int[] playerIndices)
        {
            foreach (var index in playerIndices)
                _ = _server.ConnectedHumans.Add(ElympicsPlayer.FromIndex(index));
            _server.FirstHumanConnectedUtc ??= DateTime.UtcNow;
        }

        private void Disconnect(params int[] playerIndices)
        {
            foreach (var index in playerIndices)
                _ = _server.ConnectedHumans.Remove(ElympicsPlayer.FromIndex(index));
        }

        private void RunTicks(int count)
        {
            for (var i = 0; i < count; i++)
                _server.RunTick();
        }

        private void StartGameWithConnectedPlayers(TerminationOption option, float rejoinTimeoutSeconds)
        {
            SetConfigField("terminateWhenPlayersLeave", option);
            SetConfigField("rejoinTimeoutSeconds", rejoinTimeoutSeconds);
            SetHumanPlayers(0, 1);
            Connect(0, 1);
            MarkPlayerReady(0);
            MarkPlayerReady(1);
            for (var i = 0; i < 100 && !_server.GameStartReached; i++)
                _server.RunTick();
            Assert.That(_server.GameStartReached, Is.True);
        }

        private void SetConfigField(string name, object value) =>
            typeof(ElympicsGameConfig).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_server.Config, value);

        private class RecordingGameStartHandler : MonoBehaviour, IGameStartHandler
        {
            public List<long> StartTicks { get; } = new();
            public void OnGameStarted(long startTick) => StartTicks.Add(startTick);
        }

        private class ThrowingGameStartHandler : MonoBehaviour, IGameStartHandler
        {
            public void OnGameStarted(long startTick) => throw new InvalidOperationException("Handler failure expected by the test");
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
                NotifyGameStartedIfReached();
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
