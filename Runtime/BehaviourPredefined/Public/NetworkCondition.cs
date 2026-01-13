namespace Elympics.Public
{
    public struct NetworkCondition
    {
        public double ElympicsUpdateTickRate;
        /// <summary>
        /// Tick calculated based on timing and network conditions to maintain offset from the server.
        /// </summary>
        public double ExactTickCalculated;
        /// <summary>
        /// Value taken from game config.
        /// </summary>
        public int InputLagTicks;
        /// <summary>
        /// Previous tick that was selected for further simulation.
        /// </summary>
        public long PreviousTick;
        /// <summary>
        /// Latest tick received from the server.
        /// </summary>
        public long LastReceivedTick;
        /// <summary>
        /// Weighted Local Clock Offset (LCO) in game ticks.
        ///
        /// This represents the time difference between the client's local clock and the server's clock,
        /// smoothed using a running median filter and converted to game ticks.
        /// </summary>
        public double LcoTicks;
        public bool ReconciliationPerformed;
        public double RttTicks;
        public bool WasTickJumpForced;
        public long PredictionLimit;
        public int DefaultTickRate;
        /// <summary>
        /// How many ticks the client had to catch up when a tick jump was forced.
        /// </summary>
        public double TicksToCatchup;
        /// <summary>
        /// Tick that was selected for further simulation.
        /// </summary>
        public long NewTickFromCalculations;
    }
}
