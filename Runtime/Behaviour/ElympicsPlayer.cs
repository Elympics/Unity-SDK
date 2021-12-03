using System;
using UnityEngine;

namespace Elympics
{
	[Serializable]
	public struct ElympicsPlayer : IEquatable<ElympicsPlayer>, IComparable<ElympicsPlayer>
	{
		private const int AllValue     = -3;
		private const int WorldValue   = -2;
		private const int InvalidValue = -1;

		private const int NetworkIdRange = 10000000;
		internal      int StartNetworkId => (playerIndex + 4) * NetworkIdRange;

		public static readonly ElympicsPlayer All     = new ElympicsPlayer {playerIndex = AllValue};
		public static readonly ElympicsPlayer World   = new ElympicsPlayer {playerIndex = WorldValue};
		public static readonly ElympicsPlayer Invalid = new ElympicsPlayer {playerIndex = InvalidValue};

		[SerializeField] internal int playerIndex;

		private ElympicsPlayer(int playerIndex)
		{
			this.playerIndex = playerIndex;
		}

		public static ElympicsPlayer FromIndex(int playerIndex)
		{
			if (playerIndex < 0)
				throw new ArgumentException("Player index should be greater or equal than 0");
			return new ElympicsPlayer(playerIndex);
		}

		public static ElympicsPlayer FromIndexExtended(int playerIndex)
		{
			if (playerIndex < -3)
				throw new ArgumentException("Player index should be greater or equal -3");
			return new ElympicsPlayer(playerIndex);
		}

		public static explicit operator int(ElympicsPlayer elympicsPlayer) => elympicsPlayer.playerIndex;

		public override string ToString() => playerIndex.ToString();

		public          bool Equals(ElympicsPlayer other) => playerIndex == other.playerIndex;
		public override bool Equals(object obj)           => obj is ElympicsPlayer other && Equals(other);

		public static bool operator ==(ElympicsPlayer lhs, ElympicsPlayer rhs) => lhs.Equals(rhs);
		public static bool operator !=(ElympicsPlayer lhs, ElympicsPlayer rhs) => !(lhs == rhs);

		public override int GetHashCode() => playerIndex;

		public int CompareTo(ElympicsPlayer other) => playerIndex.CompareTo(other.playerIndex);
	}
}
