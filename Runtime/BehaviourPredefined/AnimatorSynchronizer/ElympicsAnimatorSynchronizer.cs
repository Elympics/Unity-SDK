using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elympics
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Animator))]
	public class ElympicsAnimatorSynchronizer : ElympicsMonoBehaviour, IInitializable, IStateSerializationHandler
	{
		[SerializeField, HideInInspector] private List<string> disabledParameters = new List<string>();
		[SerializeField, HideInInspector] private List<string> disabledLayers     = new List<string>();

		private List<ParameterSynchronizationStatus> _boolStatuses;
		private List<ParameterSynchronizationStatus> _intStatuses;
		private List<ParameterSynchronizationStatus> _floatStatuses;
		private List<ParameterSynchronizationStatus> _triggerStatuses;
		private LayerSynchronizationStatus[]         _layerStatuses;

		private ElympicsArray<ElympicsBool>  _boolParams;
		private ElympicsArray<ElympicsInt>   _intParams;
		private ElympicsArray<ElympicsFloat> _floatParams;
		private ElympicsArray<ElympicsBool>  _triggerParams;
		private ElympicsArray<ElympicsFloat> _layerWeights;

		private readonly List<int> _triggerCache = new List<int>();

		private Animator Animator { get; set; }

		private bool _initialized;

		public void Initialize()
		{
			Animator = GetComponent<Animator>();

			var parameters = Animator.parameters;
			var layerNames = new string[Animator.layerCount];
			for (var i = 0; i < layerNames.Length; i++)
				layerNames[i] = Animator.GetLayerName(i);
			_boolStatuses = new List<ParameterSynchronizationStatus>(parameters.Count(x => x.type == AnimatorControllerParameterType.Bool));
			_intStatuses = new List<ParameterSynchronizationStatus>(parameters.Count(x => x.type == AnimatorControllerParameterType.Int));
			_floatStatuses = new List<ParameterSynchronizationStatus>(parameters.Count(x => x.type == AnimatorControllerParameterType.Float));
			_triggerStatuses = new List<ParameterSynchronizationStatus>(parameters.Count(x => x.type == AnimatorControllerParameterType.Trigger));
			_layerStatuses = new LayerSynchronizationStatus[Animator.layerCount];

			InitializeDeserializedStatuses(layerNames, parameters);

			_boolParams = new ElympicsArray<ElympicsBool>(_boolStatuses.Count, () => new ElympicsBool());
			_intParams = new ElympicsArray<ElympicsInt>(_intStatuses.Count, () => new ElympicsInt());
			_floatParams = new ElympicsArray<ElympicsFloat>(_floatStatuses.Count, () => new ElympicsFloat());
			_triggerParams = new ElympicsArray<ElympicsBool>(_triggerStatuses.Count, () => new ElympicsBool());
			_layerWeights = new ElympicsArray<ElympicsFloat>(_layerStatuses.Length, () => new ElympicsFloat());

			_initialized = true;
		}

		private void Update()
		{
			if (!_initialized)
				return;

			foreach (var triggerKey in _triggerStatuses)
				if (Animator.GetBool(triggerKey.HashName) && !_triggerCache.Contains(triggerKey.HashName))
					_triggerCache.Add(triggerKey.HashName);
		}

		public void OnPreStateSerialize()
		{
			for (var i = 0; i < _layerStatuses.Length; i++)
				_layerWeights.Values[i].Value = Animator.GetLayerWeight(i);

			for (var i = 0; i < _boolStatuses.Count; i++)
				_boolParams.Values[i].Value = Animator.GetBool(_boolStatuses[i].HashName);

			for (var i = 0; i < _floatStatuses.Count; i++)
				_floatParams.Values[i].Value = Animator.GetFloat(_floatStatuses[i].HashName);

			for (var i = 0; i < _intStatuses.Count; i++)
				_intParams.Values[i].Value = Animator.GetInteger(_intStatuses[i].HashName);

			for (var i = 0; i < _triggerStatuses.Count; i++)
				_triggerParams.Values[i].Value = _triggerCache.Contains(_triggerStatuses[i].HashName);

			_triggerCache.Clear();
		}

		public void OnPostStateDeserialize()
		{
			for (var i = 0; i < _layerStatuses.Length; i++)
				if (_layerStatuses[i].Enabled)
					Animator.SetLayerWeight(i, _layerWeights.Values[i]);

			for (var i = 0; i < _boolStatuses.Count; i++)
				if (_boolStatuses[i].Enabled)
					Animator.SetBool(_boolStatuses[i].HashName, _boolParams.Values[i].Value);

			for (var i = 0; i < _floatStatuses.Count; i++)
				if (_floatStatuses[i].Enabled)
					Animator.SetFloat(_floatStatuses[i].HashName, _floatParams.Values[i].Value);

			for (var i = 0; i < _intStatuses.Count; i++)
				if (_intStatuses[i].Enabled)
					Animator.SetInteger(_intStatuses[i].HashName, _intParams.Values[i].Value);

			for (var i = 0; i < _triggerStatuses.Count; i++)
				if (_triggerParams.Values[i].Value && _triggerStatuses[i].Enabled)
					Animator.SetTrigger(_triggerStatuses[i].HashName);
		}

		private void InitializeDeserializedStatuses(string[] layerNames, AnimatorControllerParameter[] parameters)
		{
			var disabledLayersSet = new HashSet<string>(disabledLayers);
			var disabledParametersSet = new HashSet<string>(disabledParameters);

			for (var i = 0; i < layerNames.Length; i++)
				_layerStatuses[i] = new LayerSynchronizationStatus { Name = layerNames[i], Enabled = !disabledLayersSet.Contains(layerNames[i]) };

			_boolStatuses.Clear();
			_intStatuses.Clear();
			_floatStatuses.Clear();
			_triggerStatuses.Clear();
			foreach (var parameter in parameters)
			{
				var mappedParameter = new ParameterSynchronizationStatus
				{
					Name = parameter.name,
					HashName = parameter.nameHash,
					Enabled = !disabledParametersSet.Contains(parameter.name)
				};
				GetStatusesForType(parameter.type).Add(mappedParameter);
			}
		}

		private List<ParameterSynchronizationStatus> GetStatusesForType(AnimatorControllerParameterType type)
		{
			switch (type)
			{
				case AnimatorControllerParameterType.Bool:
					return _boolStatuses;
				case AnimatorControllerParameterType.Int:
					return _intStatuses;
				case AnimatorControllerParameterType.Float:
					return _floatStatuses;
				case AnimatorControllerParameterType.Trigger:
					return _triggerStatuses;
				default:
					return null;
			}
		}
	}
}
