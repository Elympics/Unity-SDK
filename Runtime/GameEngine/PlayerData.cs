namespace Elympics
{
    internal struct PlayerData
    {
        public readonly ElympicsPlayer Player;
        /// <summary>Number of the last tick for which player received a snapshot from the server or -1 if client didn't receive any snapshots.</summary>
        public long LastReceivedSnapshot;

        public PlayerData(ElympicsPlayer player)
        {
            Player = player;
            LastReceivedSnapshot = -1;
        }
    }
}
