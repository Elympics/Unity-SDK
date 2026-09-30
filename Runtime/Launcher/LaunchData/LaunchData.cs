#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Elympics
{
    /// <summary>
    /// What a match launched by <see cref="IElympicsMatchLauncher"/> needs, one subclass per match mode.
    /// The hierarchy is closed: only the SDK's subclasses exist.
    /// </summary>
    [PublicAPI]
    public abstract class LaunchData
    {
        private protected LaunchData()
        { }

        /// <summary>Name of the match mode, for logs.</summary>
        internal abstract string ModeName { get; }

        /// <summary>The match ID reported in launcher events. <see cref="Guid.Empty"/> for modes without one.</summary>
        internal virtual Guid EventMatchId => Guid.Empty;

        /// <summary>Checks the data against the game it launches and the platform. <paramref name="failure"/> is set if it returns false.</summary>
        internal abstract bool TryValidate(ElympicsGameConfig gameConfig, ElympicsLaunchPlatform platform, out ElympicsLaunchFailure? failure);

        private protected abstract GameSceneInitializer CreateInitializer(GameSceneLaunchContext context);

        /// <summary>Creates the initializer of this mode, hands it to <paramref name="context"/> for teardown, then initializes it.</summary>
        internal GameSceneInitializer Launch(GameSceneLaunchContext context)
        {
            var initializer = CreateInitializer(context);
            context.Adopt(initializer);
            initializer.Initialize(context.Client, context.Server, context.GameConfig, context.BehavioursManager);
            return initializer;
        }

        private protected static bool Fail(ElympicsLaunchErrorCode code, string message, out ElympicsLaunchFailure? failure)
        {
            failure = new ElympicsLaunchFailure(code, message);
            return false;
        }

        private protected static T[] Copy<T>(IEnumerable<T>? values) => values?.ToArray() ?? Array.Empty<T>();
    }
}
