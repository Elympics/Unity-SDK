#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elympics.Core.Utils;
using JetBrains.Annotations;

namespace Elympics.Core
{
    public class ElympicsScore
    {
        public delegate TimeSpan GetGameplayTimeDelegate(int playerIndex, DateTime currentTime);

        internal delegate void PlayerScoreUpdatedDelegate(int playerIndex, float score, string metadataJson, DateTime utcTime, TimeSpan gameplayTime);
        internal event PlayerScoreUpdatedDelegate? PlayerScoreUpdated;

        private bool _enabled;
        private GetGameplayTimeDelegate _getGameplayTime;
        private readonly HashSet<string> _metadataKeys;

        private readonly int _playerCount;
        private readonly DateTime?[] _startingScoreUtcTime;
        private readonly float[] _score;
        private readonly Dictionary<string, JsonValue>[] _metadata;
        private readonly MetadataWrapper[] _metadataWrappers;
        private readonly DateTime[] _utcTime;
        private readonly TimeSpan[] _gameplayTime;
        private readonly bool[] _changed;

        private readonly StringBuilder _sb = new();
        private static readonly object SbLock = new();

        internal ElympicsScore(int playerCount)
        {
            _playerCount = playerCount;
            _score = new float[playerCount];
            _metadata = Enumerable.Range(0, playerCount).Select(_ => new Dictionary<string, JsonValue>()).ToArray();
            _metadataWrappers = Enumerable.Range(0, playerCount).Select(i => new MetadataWrapper(this, i)).ToArray();
            _utcTime = new DateTime[playerCount];
            _gameplayTime = new TimeSpan[playerCount];
            _changed = new bool[playerCount];
            _startingScoreUtcTime = new DateTime?[playerCount];
            _getGameplayTime = GetGameplayTimeDefault;
            _metadataKeys = new HashSet<string>();
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
        /// <param name="metadataKeys">
        /// The list of supported metadata keys. Assigning values to keys outside the list
        /// will result in <see cref="ArgumentOutOfRangeException"/>.
        /// </param>
        /// <exception cref="InvalidOperationException">When <see cref="Enable"/> has been called before.</exception>
        [PublicAPI]
        public void Enable(GetGameplayTimeDelegate? getGameplayTime = null, IEnumerable<string>? metadataKeys = null)
        {
            if (_enabled)
                throw new InvalidOperationException("Already enabled");
            if (getGameplayTime != null)
                _getGameplayTime = getGameplayTime;
            if (metadataKeys is not null)
                _metadataKeys.UnionWith(metadataKeys);
            foreach (var metadata in _metadata)
            {
                metadata.Clear();
                _ = metadata.EnsureCapacity(_metadataKeys.Count);
                foreach (var key in _metadataKeys)
                    metadata.Add(key, default);
            }
            _enabled = true;
        }

        /// <param name="playerIndex">0-based index of a player for whom the time is retrieved.</param>
        /// <returns>UTC time of the first score registered for a player.</returns>
        [PublicAPI] public DateTime? GetStartingScoreUtcTime(int playerIndex) => _startingScoreUtcTime[playerIndex];

        /// <param name="playerIndex">0-based index of a player for whom the score is retrieved.</param>
        /// <returns>The current (last) score registered for a player.</returns>
        [PublicAPI] public DateTime GetCurrentScoreUtcTime(int playerIndex) => _utcTime[playerIndex];

        /// <param name="playerIndex">0-based index of a player for whom the time is retrieved.</param>
        /// <returns>Gameplay time (see <see cref="Enable"/>) of the first score registered for a player.</returns>
        [PublicAPI] public TimeSpan GetCurrentScoreGameplayTime(int playerIndex) => _gameplayTime[playerIndex];

        /// <summary>
        /// A helper class for accessing metadata.
        /// Ensures no key outside the ones defined in <see cref="Enable"/> is used.
        /// </summary>
        public class MetadataWrapper
        {
            private readonly ElympicsScore? _parent;
            private readonly int _playerIndex;

            internal static readonly MetadataWrapper Empty = new();

            internal IReadOnlyCollection<string> Keys => (IReadOnlyCollection<string>?)_parent?._metadataKeys ?? Array.Empty<string>();

            private MetadataWrapper() { }
            internal MetadataWrapper(ElympicsScore parent, int playerIndex) => (_parent, _playerIndex) = (parent, playerIndex);

            [PublicAPI]
            public JsonValue this[string key]
            {
                get => _parent?.GetMetadata(_playerIndex, key) ?? throw new ArgumentOutOfRangeException(nameof(key), key, UnknownKeyMessage);
                set => (_parent ?? throw new ArgumentOutOfRangeException(nameof(key), key, UnknownKeyMessage)).SetMetadata(_playerIndex, key, value);
            }
        }

        /// <summary>
        /// A helper structure for transferring both score and metadata accessors.
        /// Allows for syntax sugar for assigning/reading a float score directly using <see cref="this[int]"/>,
        /// e.g. <c>Elympics.Score[0] = 12.0f;</c>
        /// </summary>
        public readonly struct ScoreWithMetadata
        {
            private const string IncorrectlyInitializedMessage = "The structure has not been initialized correctly";

            private readonly bool _isInitialized;
            private readonly ElympicsScore? _parent;
            private readonly int _playerIndex;
            private readonly MetadataWrapper _metadata;
            private readonly float? _readonlyScore;

            /// <summary>
            /// Score accessor.
            /// </summary>
            /// <exception cref="InvalidOperationException">When the default constructor has been used.</exception>
            /// <exception cref="NotSupportedException">When the setter is used for a structure cast from a float score.</exception>
            [PublicAPI]
            public float Score
            {
                get
                {
                    if (!_isInitialized)
                        throw new InvalidOperationException(IncorrectlyInitializedMessage);
                    return _readonlyScore ?? _parent!.GetScore(_playerIndex);
                }
                set
                {
                    if (!_isInitialized)
                        throw new InvalidOperationException(IncorrectlyInitializedMessage);
                    if (_readonlyScore.HasValue)
                        throw new NotSupportedException();
                    _parent!.SetScore(_playerIndex, value);
                }
            }

            /// <summary>
            /// Metadata accessor.
            /// </summary>
            /// <remarks>
            /// Null when cast from a float score.
            /// </remarks>
            /// <exception cref="InvalidOperationException">When the default constructor has been used.</exception>
            [PublicAPI]
            public MetadataWrapper Metadata => _isInitialized
                ? _metadata
                : throw new InvalidOperationException(IncorrectlyInitializedMessage);

            internal ScoreWithMetadata(ElympicsScore parent, int playerIndex)
            {
                _parent = parent;
                _playerIndex = playerIndex;
                _metadata = parent._metadataWrappers[playerIndex];
                _readonlyScore = null;
                _isInitialized = true;
            }

            private ScoreWithMetadata(float readonlyScore)
            {
                _parent = null;
                _playerIndex = 0;
                _metadata = MetadataWrapper.Empty;
                _readonlyScore = readonlyScore;
                _isInitialized = true;
            }

            public static implicit operator ScoreWithMetadata(float score) => new(score);
            public static implicit operator float(ScoreWithMetadata scoreWithMetadata) => scoreWithMetadata.Score;
        }

        [PublicAPI]
        public float GetScore(int playerIndex) => _enabled
            ? _score[playerIndex]
            : throw new InvalidOperationException($"{nameof(ElympicsScore)} has not been enabled. Use the {nameof(Enable)} method.");

        [PublicAPI]
        public void SetScore(int playerIndex, float score)
        {
            if (!_enabled)
                throw new InvalidOperationException($"{nameof(ElympicsScore)} has not been enabled. Use the {nameof(Enable)} method.");
            _score[playerIndex] = score;
            _changed[playerIndex] = true;
        }

        private const string UnknownKeyMessage = "Metadata key not defined in " + nameof(ElympicsScore) + "." + nameof(Enable);

        [PublicAPI]
        public JsonValue GetMetadata(int playerIndex, string key)
        {
            if (!_enabled)
                throw new InvalidOperationException($"{nameof(ElympicsScore)} has not been enabled. Use the {nameof(Enable)} method.");
            return _metadata[playerIndex].ContainsKey(key)
                ? _metadata[playerIndex][key]
                : throw new ArgumentOutOfRangeException(nameof(key), key, UnknownKeyMessage);
        }

        [PublicAPI]
        public void SetMetadata(int playerIndex, string key, JsonValue value)
        {
            if (!_enabled)
                throw new InvalidOperationException($"{nameof(ElympicsScore)} has not been enabled. Use the {nameof(Enable)} method.");
            if (!_metadata[playerIndex].ContainsKey(key))
                throw new ArgumentOutOfRangeException(nameof(key), key, UnknownKeyMessage);
            _metadata[playerIndex][key] = value;
            _changed[playerIndex] = true;
        }

        /// <summary>
        /// Returns score for player index.
        /// The first call to the setter saves starting score time which can be accessed using <see cref="GetStartingScoreUtcTime"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="Enable"/> must be called before getting/setting any score values.
        /// </remarks>
        /// <param name="playerIndex">0-based index of a player for whom the score is accessed.</param>
        [PublicAPI]
        public ScoreWithMetadata this[int playerIndex]
        {
            get => _enabled
                ? new ScoreWithMetadata(this, playerIndex)
                : throw new InvalidOperationException($"{nameof(ElympicsScore)} has not been enabled. Use the {nameof(Enable)} method.");
            set
            {
                SetScore(playerIndex, value.Score);
                foreach (var key in value.Metadata.Keys)
                    SetMetadata(playerIndex, key, value.Metadata[key]);
            }
        }

        private TimeSpan GetGameplayTimeDefault(int playerIndex, DateTime currentTime)
        {
            if (!_startingScoreUtcTime[playerIndex].HasValue)
                throw new InvalidOperationException($"The score of player {playerIndex} has not been initialized yet");
            return currentTime - _startingScoreUtcTime[playerIndex]!.Value;
        }

        /// <summary>
        /// Submits score and metadata, sending it to the backend.
        /// By default, only sends data for a player if their score or metadata changed since last submission.
        /// </summary>
        /// <remarks>
        /// <see cref="Enable"/> must be called first.
        /// Automatic sending is done at the end of each tick.
        /// </remarks>
        /// <param name="forceSend">Force sending the data for all players even if there were no changes.</param>
        [PublicAPI]
        public void Submit(bool forceSend = false)
        {
            if (!_enabled)
                throw new InvalidOperationException($"{nameof(ElympicsScore)} has not been enabled. Use the {nameof(Enable)} method.");
            var utcTime = DateTime.UtcNow;
            for (var i = 0; i < _playerCount; i++)
            {
                if (!_changed[i] && !forceSend)
                    continue;
                _changed[i] = false;
                _startingScoreUtcTime[i] ??= utcTime;
                _utcTime[i] = utcTime;
                _gameplayTime[i] = _getGameplayTime(i, utcTime);
                string metadata;
                lock (SbLock)
                {
                    _ = _sb.Clear()
                        .Append('{');
                    var isFirst = true;
                    foreach (var key in _metadataKeys)
                    {
                        _ = _metadata[i][key].AppendAsJsonProperty(_sb, key, isFirst);
                        isFirst = false;
                    }
                    metadata = _sb.Append('}')
                        .ToString();
                }
                PlayerScoreUpdated?.Invoke(i, _score[i], metadata, _utcTime[i], _gameplayTime[i]);
            }
        }
    }
}
