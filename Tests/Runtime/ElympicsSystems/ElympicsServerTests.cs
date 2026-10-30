using System.Collections.Generic;
using Elympics.ElympicsSystems;
using Elympics.Replication;
using NUnit.Framework;
using UnityEngine;
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
        public void TearDown() => Object.DestroyImmediate(_serverObject);

        [Test]
        public void TicksAreFinalizedWhenScoreIsNeverEnabled()
        {
            for (var i = 0; i < 3; i++)
                Assert.DoesNotThrow(_server.RunTick);

            Assert.That(_updateLoop.FinalizedTicks, Is.EqualTo(3));
            Assert.That(_submittedPlayers, Is.Empty);
        }

        [Test]
        public void ScoreEnabledMidMatchIsSubmittedFromTheNextTick()
        {
            _server.RunTick();
            _server.Score.Enable();
            _server.Score[1] = 5f;

            _server.RunTick();

            Assert.That(_updateLoop.FinalizedTicks, Is.EqualTo(2));
            Assert.That(_submittedPlayers, Is.EqualTo(new[] { 1 }));
        }

        private class TickableElympicsServer : ElympicsServer
        {
            public void MarkInitialized() => SetInitialized();

            public void RunTick()
            {
                ElympicsFixedUpdate();
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
