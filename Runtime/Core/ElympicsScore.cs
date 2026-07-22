#nullable enable

using System;
using JetBrains.Annotations;

namespace Elympics.Core
{
    public class ElympicsScore
    {
        public delegate TimeSpan GetGameplayTimeDelegate(int playerIndex, DateTime currentTime);

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

        /// <summary>
        /// Enables the score tracking mechanism by providing a function for calculating the gameplay time of each score.
        /// </summary>
        /// <remarks>Each score update must be registered manually using the <see cref="this[int]"/> setter.</remarks>
        /// <param name="getGameplayTime">
        /// Function for calculating the gameplay time of each score.
        /// The gameplay time should start with the beginning of a game or match and then be steadily increased,
        /// possibly except for times when a player was disconnected.
        /// </param>
        [PublicAPI]
        public void Enable(GetGameplayTimeDelegate? getGameplayTime = null)
        {
            if (getGameplayTime != null)
                _getGameplayTime = getGameplayTime;

            _enabled = true;
        }

        /// <param name="playerIndex">0-based index of a player for whom the time is retrieved.</param>
        /// <returns>UTC time of the first score registered for a player.</returns>
        [PublicAPI] public DateTime? GetStartingScoreUtcTime(int playerIndex) => _startingScoreUtcTime[playerIndex];

        /// <param name="playerIndex">0-based index of a player for whom the score is retrieved.</param>
        /// <returns>The current (last) score registered for a player.</returns>
        [PublicAPI] public DateTime GetCurrentScoreUtcTime(int playerIndex) => _score[playerIndex].UtcTime;

        /// <param name="playerIndex">0-based index of a player for whom the time is retrieved.</param>
        /// <returns>Gameplay time (see <see cref="Enable"/>) of the first score registered for a player.</returns>
        [PublicAPI] public TimeSpan GetCurrentScoreGameplayTime(int playerIndex) => _score[playerIndex].GameplayTime;

        /// <summary>
        /// Returns score for player index.
        /// The first call to the setter saves starting score time which can be accessed using <see cref="GetStartingScoreUtcTime"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="Enable"/> must be called before getting/setting any score values.
        /// </remarks>
        /// <param name="playerIndex">0-based index of a player for whom the score is accessed.</param>
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
                var gameplayTime = _getGameplayTime(playerIndex, utcTime);
                _score[playerIndex] = (score, utcTime, gameplayTime);
                PlayerScoreUpdated?.Invoke(playerIndex, score, utcTime, gameplayTime);
            }
        }

        private TimeSpan GetGameplayTimeDefault(int playerIndex, DateTime currentTime)
        {
            if (!_startingScoreUtcTime[playerIndex].HasValue)
                throw new InvalidOperationException($"The score of player {playerIndex} has not been initialized yet");
            return currentTime - _startingScoreUtcTime[playerIndex]!.Value;
        }
    }
}
