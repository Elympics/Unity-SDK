using System;
using MessagePack;
using UnityEngine;

namespace Elympics
{
    [Serializable]
    [MessagePackObject]
    public struct ElympicsPlayer : IEquatable<ElympicsPlayer>, IComparable<ElympicsPlayer>
    {
        /// <summary>
        /// Special player indices that can spawn/own networked objects.
        /// Order determines slot assignment for NetworkId allocation:
        /// - Index 0 in array → slot 0
        /// - Index 1 in array → slot 1
        /// - Regular players (0, 1, 2, ...) → slots starting at SpawnableSpecialPlayerIndices.Length
        /// </summary>
        public static readonly int[] SpawnableSpecialPlayerIndices = { -3, -2 }; // All, World

        /// <summary>
        /// Invalid player index - cannot spawn objects, not included in slot allocation.
        /// </summary>
        private const int InvalidPlayerIndex = -1;

        public static readonly ElympicsPlayer All = new() { playerIndex = SpawnableSpecialPlayerIndices[0] };
        public static readonly ElympicsPlayer World = new() { playerIndex = SpawnableSpecialPlayerIndices[1] };
        public static readonly ElympicsPlayer Invalid = new() { playerIndex = InvalidPlayerIndex };

        [SerializeField]
        [Key(0)] internal int playerIndex;

        private ElympicsPlayer(int playerIndex)
        {
            this.playerIndex = playerIndex;
        }

        public static ElympicsPlayer FromIndex(int playerIndex)
        {
            return playerIndex >= 0
                ? new ElympicsPlayer(playerIndex)
                : throw new ArgumentException("Player index should be greater or equal than 0");
        }

        public static ElympicsPlayer FromIndexExtended(int playerIndex)
        {
            return playerIndex >= -3
                ? new ElympicsPlayer(playerIndex)
                : throw new ArgumentException("Player index should be greater or equal -3");
        }

        public static explicit operator int(ElympicsPlayer elympicsPlayer) => elympicsPlayer.playerIndex;

        public override string ToString()
        {
            if (playerIndex == SpawnableSpecialPlayerIndices[0])
                return nameof(All);
            if (playerIndex == SpawnableSpecialPlayerIndices[1])
                return nameof(World);
            if (playerIndex == InvalidPlayerIndex)
                return nameof(Invalid);
            return playerIndex.ToString();
        }

        public bool Equals(ElympicsPlayer other) => playerIndex == other.playerIndex;
        public override bool Equals(object obj) => obj is ElympicsPlayer other && Equals(other);

        public static bool operator ==(ElympicsPlayer lhs, ElympicsPlayer rhs) => lhs.Equals(rhs);
        public static bool operator !=(ElympicsPlayer lhs, ElympicsPlayer rhs) => !(lhs == rhs);

        public override int GetHashCode() => playerIndex;

        public int CompareTo(ElympicsPlayer other) => playerIndex.CompareTo(other.playerIndex);
    }
}
