#nullable enable

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Elympics
{
    /// <summary>The local player is at index 0, followed by the bots from <see cref="ElympicsGameConfig.TestPlayers"/>.</summary>
    [PublicAPI]
    public sealed class SinglePlayerLaunchData : LaunchData
    {
        public const string DefaultQueueName = "Solo";

        /// <summary>Null means <see cref="Guid.Empty"/>.</summary>
        public Guid? MatchId { get; }
        /// <summary>Null means <see cref="Guid.Empty"/>.</summary>
        public Guid? UserId { get; }
        /// <summary>Null means <see cref="DefaultQueueName"/>.</summary>
        public string? QueueName { get; }
        public string? RegionName { get; }
        public IReadOnlyList<byte> GameEngineData { get; }
        public IReadOnlyList<float> MatchmakerData { get; }

        public SinglePlayerLaunchData(Guid? matchId = null, Guid? userId = null, string? queueName = null, string? regionName = null,
            IEnumerable<byte>? gameEngineData = null, IEnumerable<float>? matchmakerData = null)
        {
            MatchId = matchId;
            UserId = userId;
            QueueName = queueName;
            RegionName = regionName;
            GameEngineData = Copy(gameEngineData);
            MatchmakerData = Copy(matchmakerData);
        }

        internal override string ModeName => "SinglePlayer";
        internal override Guid EventMatchId => MatchId ?? Guid.Empty;
        internal override Guid? EventUserId => UserId;

        internal override bool TryValidate(ElympicsGameConfig gameConfig, ElympicsLaunchPlatform platform, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            return true;
        }

        private protected override GameSceneInitializer CreateInitializer(GameSceneLaunchContext context) => new SinglePlayerGameInitializer(this);
    }
}
