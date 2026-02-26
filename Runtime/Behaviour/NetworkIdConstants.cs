namespace Elympics
{
    /// <summary>
    /// Central configuration for NetworkId allocation system.
    /// Used by NetworkIdEnumerator, ElympicsBehavioursManager, and related classes.
    /// </summary>
    public static class NetworkIdConstants
    {
        /// <summary>
        /// Bit shift for generation component in NetworkId encoding.
        /// NetworkId = (generation &lt;&lt; GenerationShift) | index
        /// </summary>
        public const int GenerationShift = 16;

        /// <summary>
        /// Mask to extract index component from NetworkId.
        /// </summary>
        public const int IndexMask = 0xFFFF;

        /// <summary>
        /// Maximum value for index component (16-bit).
        /// </summary>
        public const int MaxIndex = 0xFFFF;

        /// <summary>
        /// Maximum value for generation component (16-bit).
        /// Generation 0 is reserved for scene objects.
        /// </summary>
        public const int MaxGeneration = 0xFFFF;

        /// <summary>
        /// Maximum concurrent networked objects supported (16-bit index limit).
        /// </summary>
        public const int MaxNetworkObjects = MaxIndex;

        internal const int PhysicsSimulatorNetworkId = 0;
        internal const int ServerLogNetworkId = 1;
        internal const int DefaultServerHandlerNetworkId = 2;
        internal const int PredefinedBehaviourCount = 3;

        /// <summary>
        /// Number of indices reserved by Elympics for auto-assigned scene objects (indices 0 to ReservedPerRange-1).
        /// </summary>
        public const int DefaultSceneObjectsReserved = 1000;

        /// <summary>
        /// Start of the range for developer manual NetworkId assignment.
        /// </summary>
        public const int ManualIdMin = DefaultSceneObjectsReserved;

        /// <summary>
        /// End of the range for developer manual NetworkId assignment (inclusive).
        /// </summary>
        public const int ManualIdMax = ManualIdMin + DefaultSceneObjectsReserved - 1;

        /// <summary>
        /// Total indices reserved for all scene objects (auto-assigned + manual).
        /// Dynamic runtime objects start after this.
        /// </summary>
        public const int TotalSceneObjectsReserved = ManualIdMax + 1;

        /// <summary>
        /// Extracts the index component from a generational NetworkId.
        /// </summary>
        public static int ExtractIndex(int networkId) => networkId & IndexMask;

        /// <summary>
        /// Extracts the generation component from a generational NetworkId.
        /// </summary>
        public static int ExtractGeneration(int networkId) => (networkId >> GenerationShift) & IndexMask;

        /// <summary>
        /// Encodes a generation and index into a NetworkId.
        /// </summary>
        public static int EncodeNetworkId(int generation, int index) => (generation << GenerationShift) | index;

        /// <summary>
        /// Calculates the number of indices available per player slot.
        /// </summary>
        /// <param name="sceneObjectsReserved">Number of indices reserved for scene objects.</param>
        /// <param name="playerCount">Number of regular players.</param>
        /// <param name="spawnableSpecialPlayerCount">Number of special spawnable players (All, World, etc.).</param>
        public static int CalculateIndicesPerPlayer(int sceneObjectsReserved, int playerCount, int spawnableSpecialPlayerCount)
        {
            var dynamicCapacity = MaxNetworkObjects - sceneObjectsReserved;
            var totalSlots = playerCount + spawnableSpecialPlayerCount;
            return dynamicCapacity / totalSlots;
        }

        /// <summary>
        /// Calculates the slot number for a given player index.
        /// </summary>
        /// <param name="playerIndex">The player index (-3 for All, -2 for World, 0+ for regular players).</param>
        /// <param name="spawnableSpecialPlayerIndices">Array of special player indices that can spawn objects.</param>
        /// <returns>Slot number for NetworkId range allocation.</returns>
        public static int GetSlotForPlayer(int playerIndex, int[] spawnableSpecialPlayerIndices)
        {
            // Check if it's a spawnable special player
            for (var i = 0; i < spawnableSpecialPlayerIndices.Length; i++)
            {
                if (spawnableSpecialPlayerIndices[i] == playerIndex)
                    return i;
            }

            // Regular player - slot starts after all special players
            if (playerIndex >= 0)
                return playerIndex + spawnableSpecialPlayerIndices.Length;

            throw new System.ArgumentException(
                $"Player index {playerIndex} is not valid for NetworkId allocation. " +
                $"Must be a spawnable special player or >= 0.");
        }

        /// <summary>
        /// Calculates the start index for a player's NetworkId allocation range.
        /// </summary>
        public static int GetStartIndexForPlayer(int playerIndex, int sceneObjectsMaxIndex, int indicesPerPlayer, int[] spawnableSpecialPlayerIndices)
        {
            var slot = GetSlotForPlayer(playerIndex, spawnableSpecialPlayerIndices);
            return sceneObjectsMaxIndex + 1 + slot * indicesPerPlayer;
        }

        /// <summary>
        /// Calculates the end index for a player's NetworkId allocation range.
        /// </summary>
        public static int GetEndIndexForPlayer(int playerIndex, int sceneObjectsMaxIndex, int indicesPerPlayer, int[] spawnableSpecialPlayerIndices)
        {
            var startIndex = GetStartIndexForPlayer(playerIndex, sceneObjectsMaxIndex, indicesPerPlayer, spawnableSpecialPlayerIndices);
            return startIndex + indicesPerPlayer - 1;
        }
    }
}
