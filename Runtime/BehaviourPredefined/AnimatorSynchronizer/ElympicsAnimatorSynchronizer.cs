using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elympics
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Animator))]
	public class ElympicsAnimatorSynchronizer : ElympicsMonoBehaviour, IInitializable, IStateSerializationHandler
	{
		[SerializeField, HideInInspector] private bool ShowLayerWeights = true;

		[SerializeField, HideInInspector] private bool ShowParameters = true;

		[SerializeField, HideInInspector] private List<ParameterSynchronizationStatus> _boolStatuses = new List<ParameterSynchronizationStatus>();

		[SerializeField, HideInInspector] private List<ParameterSynchronizationStatus> _intStatuses = new List<ParameterSynchronizationStatus>();

		[SerializeField, HideInInspector] private List<ParameterSynchronizationStatus> _floatStatuses = new List<ParameterSynchronizationStatus>();

		[SerializeField, HideInInspector] private List<ParameterSynchronizationStatus> _triggersStatuses = new List<ParameterSynchronizationStatus>();

		[SerializeField, HideInInspector] private List<LayerSynchronizationStatus> _layersStatuses = new List<LayerSynchronizationStatus>();

		private ElympicsArray<ElympicsBool>  _boolParams;
		private ElympicsArray<ElympicsInt>   _intParams;
		private ElympicsArray<ElympicsFloat> _floatParams;
		private ElympicsArray<ElympicsBool>  _triggerParams;
		private ElympicsArray<ElympicsFloat> _layersWeights;

		private readonly List<int> _triggerCache = new List<int>();

		private Animator _animator;
		private Animator Animator => _animator ?? (_animator = GetComponent<Animator>());

		public void Initialize()
		{
			_boolParams = new ElympicsArray<ElympicsBool>(_boolStatuses.Count, () => new ElympicsBool());
			_intParams = new ElympicsArray<ElympicsInt>(_intStatuses.Count, () => new ElympicsInt());
			_floatParams = new ElympicsArray<ElympicsFloat>(_floatStatuses.Count, () => new ElympicsFloat());
			_triggerParams = new ElympicsArray<ElympicsBool>(_triggersStatuses.Count, () => new ElympicsBool());
			_layersWeights = new ElympicsArray<ElympicsFloat>(_layersStatuses.Count, () => new ElympicsFloat());
		}

		public void PrepareStatusesToUpdate()
		{
			_boolStatuses.ForEach(r => r.Updated = false);
			_intStatuses.ForEach(r => r.Updated = false);
			_floatStatuses.ForEach(r => r.Updated = false);
			_triggersStatuses.ForEach(r => r.Updated = false);
			_layersStatuses.ForEach(r => r.Updated = false);
		}

		public void RemoveOutdatedStatuses()
		{
			_boolStatuses.RemoveAll(r => r.Updated == false);
			_intStatuses.RemoveAll(r => r.Updated == false);
			_floatStatuses.RemoveAll(r => r.Updated == false);
			_triggersStatuses.RemoveAll(r => r.Updated == false);
			_layersStatuses.RemoveAll(r => r.Updated == false);
		}

		public void SetLayer(int index, bool isEnabled) => _layersStatuses.First(r => r.Index == index).Enabled = isEnabled;

		public bool GetLayer(int layerIndex)
		{
			int index = _layersStatuses.FindIndex(r => r.Index == layerIndex);
			if (index == -1)
			{
				_layersStatuses.Add(new LayerSynchronizationStatus { Enabled = true, Index = layerIndex, Updated = true });
				return true;
			}

			_layersStatuses[index].Updated = true;
			return _layersStatuses[index].Enabled;
		}

		public void SetParameter(AnimatorControllerParameterType type, int hashName, bool isEnabled)
		{
			var statuses = GetListByParameterType(type);
			statuses.First(r => r.HashName == hashName).Enabled = isEnabled;
		}

		public bool GetParameter(AnimatorControllerParameterType type, int hashName)
		{
			var statuses = GetListByParameterType(type);
			int index = statuses.FindIndex(x => x.HashName == hashName);

			if (index == -1)
			{
				statuses.Add(new ParameterSynchronizationStatus { Enabled = true, HashName = hashName, Updated = true });
				return true;
			}

			statuses[index].Updated = true;
			return statuses[index].Enabled;
		}

		private List<ParameterSynchronizationStatus> GetListByParameterType(AnimatorControllerParameterType type)
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
					return _triggersStatuses;
				default:
					return null;
			}
		}

		private void Update()
		{
			for (int i = 0; i < _triggersStatuses.Count; i++)
			{
				var triggerKey = _triggersStatuses[i];
				if (Animator.GetBool(triggerKey.HashName) && !_triggerCache.Contains(triggerKey.HashName))
					_triggerCache.Add(triggerKey.HashName);
			}
		}

		public void OnPreStateSerialize()
		{
			for (int i = 0; i < _layersStatuses.Count; i++)
				_layersWeights.Values[i].Value = Animator.GetLayerWeight(i);

			for (int i = 0; i < _boolStatuses.Count; i++)
				_boolParams.Values[i].Value = Animator.GetBool(_boolStatuses[i].HashName);

			for (int i = 0; i < _floatStatuses.Count; i++)
				_floatParams.Values[i].Value = Animator.GetFloat(_floatStatuses[i].HashName);

			for (int i = 0; i < _intStatuses.Count; i++)
				_intParams.Values[i].Value = Animator.GetInteger(_intStatuses[i].HashName);

			for (int i = 0; i < _triggersStatuses.Count; i++)
				_triggerParams.Values[i].Value = _triggerCache.Contains(_triggersStatuses[i].HashName);

			_triggerCache.Clear();
		}

		public void OnPostStateDeserialize()
		{
			for (int i = 0; i < _layersStatuses.Count; i++)
				if (_layersStatuses[i].Enabled)
					Animator.SetLayerWeight(i, _layersWeights.Values[i]);

			for (int i = 0; i < _boolStatuses.Count; i++)
				if (_boolStatuses[i].Enabled)
					Animator.SetBool(_boolStatuses[i].HashName, _boolParams.Values[i].Value);

			for (int i = 0; i < _floatStatuses.Count; i++)
				if (_floatStatuses[i].Enabled)
					Animator.SetFloat(_floatStatuses[i].HashName, _floatParams.Values[i].Value);

			for (int i = 0; i < _intStatuses.Count; i++)
				if (_intStatuses[i].Enabled)
					Animator.SetInteger(_intStatuses[i].HashName, _intParams.Values[i].Value);

			for (int i = 0; i < _triggersStatuses.Count; i++)
				if (_triggerParams.Values[i].Value && _triggersStatuses[i].Enabled)
					Animator.SetTrigger(_triggersStatuses[i].HashName);
		}
	}
}