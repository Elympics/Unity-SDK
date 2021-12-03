using UnityEngine;

namespace Elympics
{
	[DisallowMultipleComponent]
	public class ElympicsTransformSynchronizer : MonoBehaviour, IStateSerializationHandler, IInitializable
	{
		[SerializeField, ConfigForVar(nameof(_localPosition))]
		private ElympicsVarConfig localPositionConfig = new ElympicsVarConfig();
		[SerializeField, ConfigForVar(nameof(_localScale))]
		private ElympicsVarConfig localScaleConfig = new ElympicsVarConfig();
		[SerializeField, ConfigForVar(nameof(_localRotation))]
		private ElympicsVarConfig localRotationConfig = new ElympicsVarConfig(tolerance: 1f);

		private ElympicsVector3    _localPosition;
		private ElympicsVector3    _localScale;
		private ElympicsQuaternion _localRotation;

		private bool _initialized;

		public void Initialize()
		{
			if (_initialized)
				return;

			_localPosition = new ElympicsVector3(default, localPositionConfig);
			_localScale = new ElympicsVector3(default, localScaleConfig);
			_localRotation = new ElympicsQuaternion(default, localRotationConfig);

			_initialized = true;
		}

		public void OnPostStateDeserialize()
		{
			var cachedTransform = transform;
			if (_localPosition.EnabledSynchronization)
				cachedTransform.localPosition = _localPosition;
			if (_localScale.EnabledSynchronization)
				cachedTransform.localScale = _localScale;
			if (_localRotation.EnabledSynchronization)
				cachedTransform.localRotation = _localRotation;
		}

		public void OnPreStateSerialize()
		{
			var cachedTransform = transform;
			if (_localPosition.EnabledSynchronization)
				_localPosition.Value = cachedTransform.localPosition;
			if (_localScale.EnabledSynchronization)
				_localScale.Value = cachedTransform.localScale;
			if (_localRotation.EnabledSynchronization)
				_localRotation.Value = cachedTransform.localRotation;
		}
	}
}
