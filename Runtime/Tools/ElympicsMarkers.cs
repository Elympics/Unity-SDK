using Unity.Profiling;

public static class ElympicsMarkers
{
    #region Markers

    public static readonly ProfilerMarker Elympics_ReconcileLoopMarker = new("Elympics_Reconciliation");
    public static readonly ProfilerMarker Elympics_ElympicsUpdateMarker = new("Elympics_ElympicsUpdate");
    public static readonly ProfilerMarker Elympics_ProcessingInputMarker = new("Elympics_ProcessingInput");
    public static readonly ProfilerMarker Elympics_GatheringClientInputMarker = new("Elympics_GatheringClientInput");
    public static readonly ProfilerMarker Elympics_ResimulationkMarker = new("Elympics_Resimulation");
    public static readonly ProfilerMarker Elympics_ProcessSnapshotMarker = new("Elympics_ProcessSnapshot");
    public static readonly ProfilerMarker Elympics_ApplyingInputMarker = new("Elympics_ApplyingInput");
    public static readonly ProfilerMarker Elympics_ApplyUnpredictablePartOfSnapshotMarker = new("Elympics_ApplyUnpredictablePartOfSnapshot");
    public static readonly ProfilerMarker Elympics_PredictionMarker = new("Elympics_Prediction");
    public static readonly ProfilerMarker Elympics_SnapshotCollector = new("Elympics_SnapshotCollector");
    public static readonly ProfilerMarker Elympics_SnapshotCollector_BufferStore = new("Elympics_SnapshotCollector_BufferStore");
    public static readonly ProfilerMarker Elympics_SnapshotCollector_OnBufferLimit = new("Elympics_SnapshotCollector_OnBufferLimit");

    #endregion
}
