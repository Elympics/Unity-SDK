namespace Elympics
{
	internal partial class ElympicsAnimatorSynchronizerEditor
	{
		private const string Label_GameObjectNullWarning = "GameObject doesn't have an Animator component to synchronize";
		private const string Label_NoAnimatorLayersWarning = "Animator doesn't have any layers to synchronize";
		private const string Label_NoAnimatorParametersWarning = "Animator doesn't have any parameters to synchronize";

		private const string Label_Layers = "Synchronized Layers";
		private const string Label_LayersTooltip = "Choose which layers should be updated";

		private const string Label_Parameters = "Synchronized Parameters";
		private const string Label_ParametersTooltip = "Choose which parameters should be updated";

		private const string ShowLayersWeightPropertyName = "ShowLayerWeights";
		private const string ShowParametersPropertyName = "ShowParameters";
	}
}
