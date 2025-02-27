#nullable enable

using System.Collections.Generic;
using System.Linq;
using Elympics.SnapshotAnalysis;
using UnityEngine;

namespace Elympics.ElympicsSystems
{
    internal class SnapshotReplayElympicsUpdateLoop : IServerElympicsUpdateLoop, IReplayManipulatorClient
    {
        private readonly ElympicsBehavioursManager _behavioursManager;
        private readonly Dictionary<long, ElympicsSnapshotWithMetadata> _savedSnapshots;
        private readonly IReplayManipulator _replayManipulator;
        private readonly PhysicsScene _physicsScene;
        private readonly PhysicsScene2D _physicsScene2D;
        private readonly long _endTick;

        private bool _isPlaying = true;

        public long Tick { get; private set; }

        public SnapshotReplayElympicsUpdateLoop(ElympicsBehavioursManager behavioursManager, Dictionary<long, ElympicsSnapshotWithMetadata> savedSnapshots, IReplayManipulator replayManipulator)
        {
            _behavioursManager = behavioursManager;
            _savedSnapshots = savedSnapshots;
            _replayManipulator = replayManipulator;
            Tick = _savedSnapshots.Keys.Min();
            _endTick = _savedSnapshots.Keys.Max();
            _physicsScene = behavioursManager.gameObject.scene.GetPhysicsScene();
            _physicsScene2D = behavioursManager.gameObject.scene.GetPhysicsScene2D();
        }

        public ElympicsSnapshot GenerateSnapshot()
        {
            var snapshot = _savedSnapshots[Tick];

            _behavioursManager.ApplySnapshot(snapshot, ignoreTolerance: true);
            _behavioursManager.CommitVars();
            _physicsScene.Simulate(float.Epsilon);
            _ = _physicsScene2D.Simulate(float.Epsilon);
            _replayManipulator.SetCurrentTick(Tick);

            return snapshot;
        }

        public void FinalizeTick(ElympicsSnapshot snapshot)
        {
            if (_isPlaying && Tick < _endTick)
                Tick++;
        }
        public void HandleRenderFrame(in RenderData renderData) => _behavioursManager.Render(renderData);
        public void SetIsPlaying(bool isPlaying) => _isPlaying = isPlaying;

        public void JumpToTick(long tick) => Tick = tick;
    }
}
