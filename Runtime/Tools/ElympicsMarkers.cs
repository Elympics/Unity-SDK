using Unity.Profiling;

public static class ElympicsMarkers
{
	#region Markers
	public static readonly ProfilerMarker Elympics_ReconcileLoopMarker = new ProfilerMarker("Elympics_Reconciliation");
	public static readonly ProfilerMarker Elympics_ElympicsUpdateMarker = new ProfilerMarker("Elympics_ElympicsUpdate");
	public static readonly ProfilerMarker Elympics_ProcessingInputMarker = new ProfilerMarker("Elympics_ProcessingInput");
	public static readonly ProfilerMarker Elympics_GatheringClientInputMarker = new ProfilerMarker("Elympics_GatheringClientInput");
	public static readonly ProfilerMarker Elympics_ResimulationkMarker = new ProfilerMarker("Elympics_Resimulation");
	public static readonly ProfilerMarker Elympics_ProcessSnapshotMarker = new ProfilerMarker("Elympics_ProcessSnapshot");
	public static readonly ProfilerMarker Elympics_ApplyingInputMarker = new ProfilerMarker("Elympics_ApplyingInput");
	public static readonly ProfilerMarker Elympics_ApplyUnpredictablePartOfSnapshotMarker = new ProfilerMarker("Elympics_ApplyUnpredictablePartOfSnapshot");
	public static readonly ProfilerMarker Elympics_PredictionMarker = new ProfilerMarker("Elympics_Prediction");
	#endregion
}