namespace Elympics
{
    /// <summary>
    /// Determines the relative importance of an entity for bandwidth scheduling in <see cref="Replication.BandwidthSchedulingSystem"/>.
    /// Higher-priority entities are sent first when the bandwidth budget is limited.
    /// Does NOT control re-send frequency — use <see cref="ElympicsBehaviour.netUpdateIntervalInTicks"/> for that.
    /// </summary>
    public enum ReplicationPriority
    {
        Critical,
        High,
        Normal,
        Low,
        VeryLow,
    }
}
