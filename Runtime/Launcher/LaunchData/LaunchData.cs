#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Elympics
{
    [PublicAPI]
    public abstract class LaunchData
    {
        private protected LaunchData()
        { }

        internal abstract string ModeName { get; }

        internal virtual Guid EventMatchId => Guid.Empty;

        internal abstract bool TryValidate(ElympicsGameConfig gameConfig, ElympicsLaunchPlatform platform, out ElympicsLaunchFailure? failure);

        private protected abstract GameSceneInitializer CreateInitializer(GameSceneLaunchContext context);

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
