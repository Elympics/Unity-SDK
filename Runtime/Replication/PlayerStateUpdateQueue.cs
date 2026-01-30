using System.Collections.Concurrent;

namespace Elympics.Replication
{
    /// <summary>
    /// Thread-safe queue for player state updates. Network threads enqueue updates;
    /// the simulation thread drains them at tick start before pipeline execution.
    /// This ensures pipeline reads see a consistent snapshot of player state
    /// with no mid-tick writes from the network thread.
    /// </summary>
    internal sealed class PlayerStateUpdateQueue
    {
        /// <summary>
        /// Represents a single player state update to be applied at tick start.
        /// </summary>
        private readonly struct Update
        {
            internal readonly int PlayerIndex;
            internal readonly long LastReceivedSnapshot;

            internal Update(int playerIndex, long lastReceivedSnapshot)
            {
                PlayerIndex = playerIndex;
                LastReceivedSnapshot = lastReceivedSnapshot;
            }
        }

        private readonly ConcurrentQueue<Update> _queue = new();

        /// <summary>
        /// Enqueues a player state update. Called from the network thread.
        /// </summary>
        internal void Enqueue(int playerIndex, long lastReceivedSnapshot) => _queue.Enqueue(new Update(playerIndex, lastReceivedSnapshot));

        /// <summary>
        /// Drains all queued updates into the given arrays, applying max-wins semantics
        /// to handle out-of-order network delivery.
        /// Called from the simulation thread at tick start, before pipeline execution.
        /// </summary>
        internal void DrainTo(long[] playerLastReceivedSnapshot)
        {
            while (_queue.TryDequeue(out var update))
            {
                if (update.LastReceivedSnapshot > playerLastReceivedSnapshot[update.PlayerIndex])
                    playerLastReceivedSnapshot[update.PlayerIndex] = update.LastReceivedSnapshot;
            }
        }

        /// <summary>
        /// Clears all pending updates without applying them.
        /// Used during client reconnect to discard stale queued data from the previous session.
        /// </summary>
        internal void Reset()
        {
            while (_queue.TryDequeue(out _))
            { }
        }
    }
}
