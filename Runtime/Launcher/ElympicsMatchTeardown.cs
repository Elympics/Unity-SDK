#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Elympics.Core.Logger;
using Elympics.GameEngine.Libraries.WebRtc;
using Elympics.Replication;
using UnityEngine;

namespace Elympics
{
    /// <summary>
    /// Process-wide settings a gameplay scene changes (tick-driven physics and time step, server frame rate).
    /// Captured before the gameplay scene loads and restored in teardown, so the boot scene gets them back.
    /// </summary>
    internal readonly struct ElympicsProcessGlobals
    {
        private readonly float _fixedDeltaTime;
        private readonly float _maximumDeltaTime;
        private readonly int _targetFrameRate;
        private readonly bool _physicsAutoSimulation;
        private readonly SimulationMode2D _physics2DSimulationMode;

        private ElympicsProcessGlobals(float fixedDeltaTime, float maximumDeltaTime, int targetFrameRate, bool physicsAutoSimulation, SimulationMode2D physics2DSimulationMode)
        {
            _fixedDeltaTime = fixedDeltaTime;
            _maximumDeltaTime = maximumDeltaTime;
            _targetFrameRate = targetFrameRate;
            _physicsAutoSimulation = physicsAutoSimulation;
            _physics2DSimulationMode = physics2DSimulationMode;
        }

        public static ElympicsProcessGlobals Capture() =>
            new(Time.fixedDeltaTime, Time.maximumDeltaTime, Application.targetFrameRate, Physics.autoSimulation, Physics2D.simulationMode);

        public void Restore()
        {
            Time.fixedDeltaTime = _fixedDeltaTime;
            Time.maximumDeltaTime = _maximumDeltaTime;
            Application.targetFrameRate = _targetFrameRate;
            Physics.autoSimulation = _physicsAutoSimulation;
            Physics2D.simulationMode = _physics2DSimulationMode;
        }
    }

    /// <summary>The process-wide part of match teardown. Per-scene steps are registered on <see cref="ElympicsMatchSession"/>.</summary>
    internal static class ElympicsMatchTeardown
    {
        /// <summary>
        /// Frames yielded around releasing memory. On WebGL the garbage collector only runs with an empty stack,
        /// i.e. at the end of a frame — collecting (or checking collectability) in the teardown frame achieves nothing there.
        /// </summary>
        internal const int CollectionFrames = 2;

        /// <summary>
        /// Releases the static state of the torn-down match. Runs after the session is detached and before the boot scene loads.
        /// Never throws.
        /// </summary>
        public static void ReleaseStatics()
        {
            Try("dispose replication pipeline", () => ReplicationPipeline.Current?.Dispose());
            Try("dispose replication world", () =>
            {
                var world = ElympicsWorld.Current;
                ElympicsWorld.Current = null;
                world?.Dispose();
            });
            // Before the boot scene loads: the next gameplay scene's dispatcher claims Instance only if it is empty.
            Try("release async events dispatcher", AsyncEventsDispatcher.ReleaseForTeardown);
            Try("reset behaviours manager statics", ElympicsBehavioursManager.ResetStatics);
            Try("clear logger match state", () =>
            {
                ElympicsLogger.State.ClearRoom();
                ElympicsLogger.State.ClearGameServer();
            });
        }

        /// <summary>
        /// Lets the frame-end collector run, unloads unused assets, and collects again. <see cref="GC.Collect()"/> is effective
        /// on desktop and iOS; on WebGL it is a no-op and the yields do the work.
        /// </summary>
        public static async UniTask ReleaseMemoryAsync()
        {
            await UniTask.DelayFrame(CollectionFrames);
            await Resources.UnloadUnusedAssets();
            await UniTask.DelayFrame(CollectionFrames);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>
        /// Post-teardown checks (<c>UNITY_ASSERTIONS</c>, i.e. Editor and development builds) and the per-cycle memory sample
        /// used by soak tests. Call after <see cref="ReleaseMemoryAsync"/>, never in the teardown frame.
        /// </summary>
        public static void AssertReleased(IReadOnlyList<WeakReference> collectable, int cycle)
        {
#if UNITY_ASSERTIONS
            var failures = new List<string>();
            if (ElympicsWorld.Current != null)
                failures.Add($"{nameof(ElympicsWorld)}.{nameof(ElympicsWorld.Current)} is not null");
            if (ReplicationPipeline.Current != null)
                failures.Add($"{nameof(ReplicationPipeline)}.{nameof(ReplicationPipeline.Current)} is not null");
            if (AsyncEventsDispatcher.Instance is not null)
                failures.Add($"{nameof(AsyncEventsDispatcher)}.{nameof(AsyncEventsDispatcher.Instance)} is not null");
            var liveWebRtcClients = WebRtcFactory.LiveClientCount;
            if (liveWebRtcClients != 0)
                failures.Add($"{liveWebRtcClients} WebRTC clients were not disposed");
            foreach (var failure in failures)
                ElympicsLogger.LogError($"Match teardown left state behind: {failure}.");

            var pinned = collectable.Where(reference => reference.IsAlive).Select(reference => reference.Target?.GetType().Name ?? "?").ToArray();
            if (pinned.Length > 0)
            {
                var message = $"Match teardown left objects alive: {string.Join(", ", pinned)}.";
#if UNITY_WEBGL && !UNITY_EDITOR
                // Warn-only on WebGL: Boehm scans conservatively, so any stale word in scanned memory can pin an object
                // that is otherwise unreachable. A flaky gate is worse than a soft one; desktop and iOS keep it hard.
                ElympicsLogger.LogWarning(message);
#else
                ElympicsLogger.LogError(message);
#endif
            }

            ElympicsLogger.LogInfo(DescribeMemory(cycle));
#endif
        }

        /// <summary>One line per cycle for soak tests. The heap size is coarse (it grows in big steps and never shrinks): it can falsify but never confirm.</summary>
        internal static string DescribeMemory(int cycle)
        {
            var sb = new StringBuilder($"Match cycle {cycle} memory: ");
            _ = sb.Append($"behaviours={Resources.FindObjectsOfTypeAll<ElympicsBehaviour>().Length}, ");
            _ = sb.Append($"webRtcClients={WebRtcFactory.LiveClientCount}, ");
            _ = sb.Append($"jsWebRtcInstances={WebRtcFactory.JsInstanceCount}, ");
            _ = sb.Append($"wasmHeapBytes={WebRtcFactory.WasmHeapSize}");
            return sb.ToString();
        }

        private static void Try(string name, Action step)
        {
            try
            {
                step();
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(new ElympicsException($"Match teardown step \"{name}\" failed.", e));
            }
        }
    }
}
