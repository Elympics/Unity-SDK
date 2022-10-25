namespace Elympics
{
	internal partial class ElympicsBehaviourEditor
	{
		private const string Label_BehaviourNotModifiable = "This behaviour is not modifiable";
		private const string Label_ObservedMonoBehaviours = "Observed MonoBehaviours:";

		private const string Label_TransformExistRigidbodyWarning = "<color=yellow>Warning! Behaviour contains Transform Synchronizer. Adding Rigidbody synchronizer may cause performance and synchronization issues.</color> <a href=\"https://docs.elympics.cc/guide/state/#elympicstransformsynchronizer\"></a>";
		private const string Label_RigidBodyExistExistTransformWarning = "<color=yellow>Warning! Behaviour contains RigidBody Synchronizer. Adding Transform synchronizer may cause performance and synchronization issues.</color> <a href=\"https://docs.elympics.cc/guide/state/#elympicsrigidbodysynchronizer\"></a>";
		private const string Label_TransformAndRigidBodyExistWarning = "<color=yellow>Warning! Behaviour contains both Transform and RigidBody Synchronizer. Having both may cause performance and synchronization issues.</color> <a href=\"https://docs.elympics.cc/guide/state/#elympicsrigidbodysynchronizer\"></a>";

		/* todo add LINK text in href RichText after after migrating to 2021.3 where Unity GUI is able to detect hyperlink text click ~kpieta 25.10.2022 https://docs.unity3d.com/2021.3/Documentation/ScriptReference/EditorGUI-hyperLinkClicked.html.*/

		private const string Label_NetworkId = "Network ID:";
		private const string Label_AutoId = "Auto assign network ID:";

		private const string Label_AutoIdSummary = "Network ids determine the order in which objects are updated";

		private const string Label_AutoIdTooltip = "Enable or disable auto id assignement for this object";

		private const string Label_PredictableFor = "Predictable for: ";

		// TODO: link to docs
		private const string Label_PredictabilitySummary = "Prediction can compensate for network latency and make the game experience smoother.";

		private const string Label_PredictabilityTooltip = "Choose for which players this object will be predicted. Other players will only see updates coming from the server.";

		private const string Label_UpdatableForNonOwners = "Updatable for others: ";

		private const string Label_UpdatableForNonOwnersTooltip = "(Advanced) Run prediction even for clients that don't own the behaviour.";

		private const string Label_VisibleFor = "Visible for: ";

		// TODO: link to docs
		private const string Label_VisibilitySummary = "Limiting visibility allows you to synchronise data that should be hidden from other players";

		private const string Label_VisibilityTooltip = "Choose which players will receive data about this object";

		private const string Label_StateUpdateFrequency = "State update frequency: ";

		private const string Label_StateUpdateFrequencySummary = "Decide how often object state will be send in next snapshots";

		private const string Label_StateUpdateFrequencyTooltip = "For the next X miliseconds, state will be updated once per Y miliseconds";
	}
}
