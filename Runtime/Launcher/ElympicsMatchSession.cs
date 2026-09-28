#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Elympics.Core.Logger;

namespace Elympics
{
    /// <summary>
    /// Everything the launcher knows about one launch. Created in <see cref="MatchLauncherState.Loading"/> and detached
    /// in teardown, after which reports from the (dying) gameplay scene are ignored.
    /// </summary>
    internal sealed class ElympicsMatchSession : IElympicsMatchLifecycleSink
    {
        public ElympicsLaunch Launch { get; }
        /// <summary>Cancelled when the match is torn down.</summary>
        public CancellationToken Token { get; }
        public bool SceneInitialized { get; private set; }
        public bool Detached { get; private set; }

        private readonly CancellationTokenSource _cts = new();
        private readonly IElympicsMatchLifecycleSink _owner;
        private readonly List<(string Name, Action Step)> _teardownSteps = new();
        private readonly List<WeakReference> _collectable = new();

        /// <summary>Objects that must be garbage-collectable after teardown. Checked in development builds.</summary>
        public IReadOnlyList<WeakReference> Collectable => _collectable;

        public ElympicsMatchSession(ElympicsLaunch launch, IElympicsMatchLifecycleSink owner)
        {
            Launch = launch;
            _owner = owner;
            // Cached: reading CancellationTokenSource.Token after Dispose throws, and late readers are expected.
            Token = _cts.Token;
        }

        /// <summary>Called by the gameplay scene when it picks the session up. Returns false if it was already picked up.</summary>
        public bool TryAttachScene()
        {
            if (Detached || SceneInitialized)
                return false;
            SceneInitialized = true;
            return true;
        }

        /// <summary>Registers a step run by teardown, before the boot scene is loaded. Steps run in registration order.</summary>
        public void RegisterTeardown(string name, Action step)
        {
            if (!Detached)
                _teardownSteps.Add((name, step));
        }

        /// <summary>Registers an object that must not outlive the match.</summary>
        public void TrackCollectable(object target) => _collectable.Add(new WeakReference(target));

        /// <summary>Cancels <see cref="Token"/>, runs the teardown steps and stops forwarding reports. Never throws. Idempotent.</summary>
        public void Detach()
        {
            if (Detached)
                return;
            Detached = true;
            Try("cancel match token", _cts.Cancel);
            foreach (var (name, step) in _teardownSteps)
                Try(name, step);
            _teardownSteps.Clear();
            Try("dispose match token", _cts.Dispose);
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

        void IElympicsMatchLifecycleSink.SceneInitializationFailed(ElympicsLaunchErrorCode code, string message)
        {
            if (!Detached)
                _owner.SceneInitializationFailed(code, message);
        }

        void IElympicsMatchLifecycleSink.MatchJoined()
        {
            if (!Detached)
                _owner.MatchJoined();
        }

        void IElympicsMatchLifecycleSink.JoinFailed(string message)
        {
            if (!Detached)
                _owner.JoinFailed(message);
        }

        void IElympicsMatchLifecycleSink.MatchEnded(Guid matchId)
        {
            if (!Detached)
                _owner.MatchEnded(matchId);
        }

        void IElympicsMatchLifecycleSink.Disconnected(bool byServer)
        {
            if (!Detached)
                _owner.Disconnected(byServer);
        }
    }
}
