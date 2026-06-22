#nullable enable

using System;
using JetBrains.Annotations;

namespace Elympics.Core
{
    public class ElympicsScore
    {
        public delegate TimeSpan GetGameplayTimeDelegate(int playerIndex);

        internal delegate void PlayerScoreUpdatedDelegate(int playerIndex, float score, DateTime utcTime, TimeSpan gameplayTime);
        internal event PlayerScoreUpdatedDelegate? PlayerScoreUpdated;

        private bool _enabled;
        private GetGameplayTimeDelegate _getGameplayTime;
        private readonly DateTime?[] _startingScoreUtcTime;
        private readonly (float Score, DateTime UtcTime, TimeSpan GameplayTime)[] _score;

        [PublicAPI]
        internal ElympicsScore(int playerCount)
        {
            _score = new (float Score, DateTime UtcTime, TimeSpan GameplayTime)[playerCount];
            _startingScoreUtcTime = new DateTime?[playerCount];
            _getGameplayTime = GetGameplayTimeDefault;
        }

        [PublicAPI]
        public void Enable(GetGameplayTimeDelegate? getGameplayTime = null)
        {
            if (getGameplayTime != null)
                _getGameplayTime = getGameplayTime;
        }

        [PublicAPI] public DateTime? GetStartingScoreUtcTime(int playerIndex) => _startingScoreUtcTime[playerIndex];
        [PublicAPI] public DateTime GetCurrentScoreUtcTime(int playerIndex) => _score[playerIndex].UtcTime;
        [PublicAPI] public TimeSpan GetCurrentScoreGameplayTime(int playerIndex) => _score[playerIndex].GameplayTime;

        /// <summary>
        /// Return score for player index
        /// </summary>
        /// <param name="playerIndex"></param>
        [PublicAPI]
        public float this[int playerIndex]
        {
            get => _enabled
                ? _score[playerIndex].Score
                : throw new InvalidOperationException($"{nameof(ElympicsScore)} has not been enabled. Use the {nameof(Enable)} method.");
            set
            {
                if (!_enabled)
                    throw new InvalidOperationException($"{nameof(ElympicsScore)} has not been enabled. Use the {nameof(Enable)} method.");
                var score = value;
                var utcTime = DateTime.UtcNow;
                _startingScoreUtcTime[playerIndex] ??= utcTime;
                var gameplayTime = _getGameplayTime(playerIndex);
                _score[playerIndex] = (score, utcTime, gameplayTime);
                PlayerScoreUpdated?.Invoke(playerIndex, score, utcTime, gameplayTime);
            }
        }

        private TimeSpan GetGameplayTimeDefault(int playerIndex)
        {
            if (!_startingScoreUtcTime[playerIndex].HasValue)
                throw new InvalidOperationException($"The score of player {playerIndex} has not been initialized yet");
            return DateTime.UtcNow - _startingScoreUtcTime[playerIndex]!.Value;
        }
    }
}
