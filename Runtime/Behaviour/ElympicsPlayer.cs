using System;
using MessagePack;
using UnityEngine;

namespace Elympics
{
    [Serializable]
    [MessagePackObject]
    public struct ElympicsPlayer : IEquatable<ElympicsPlayer>, IComparable<ElympicsPlayer>
    {
        private const int AllValue = -3;
        private const int WorldValue = -2;
        private const int InvalidValue = -1;
        private const int NetworkIdOffset = 4;

        internal int StartNetworkId => (playerIndex + NetworkIdOffset) * ElympicsBehavioursManager.NetworkIdRange;
        internal int EndNetworkId => StartNetworkId - 1 + ElympicsBehavioursManager.NetworkIdRange;

        public static readonly ElympicsPlayer All = new() { playerIndex = AllValue };
        public static readonly ElympicsPlayer World = new() { playerIndex = WorldValue };
        public static readonly ElympicsPlayer Invalid = new() { playerIndex = InvalidValue };

        [SerializeField][Key(0)] internal int playerIndex;

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

        public override string ToString() => playerIndex.ToString();

        public bool Equals(ElympicsPlayer other) => playerIndex == other.playerIndex;
        public override bool Equals(object obj) => obj is ElympicsPlayer other && Equals(other);

        public static bool operator ==(ElympicsPlayer lhs, ElympicsPlayer rhs) => lhs.Equals(rhs);
        public static bool operator !=(ElympicsPlayer lhs, ElympicsPlayer rhs) => !(lhs == rhs);

        public override int GetHashCode() => playerIndex;

        public int CompareTo(ElympicsPlayer other) => playerIndex.CompareTo(other.playerIndex);
    }
}
