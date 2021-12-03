using UnityEngine;

namespace Elympics
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Rigidbody))]
	public class ElympicsRigidBodySynchronizer : MonoBehaviour, IStateSerializationHandler, IInitializable
	{
		[SerializeField, ConfigForVar(nameof(_position))]
		private ElympicsVarConfig positionConfig = new ElympicsVarConfig();
		[SerializeField, ConfigForVar(nameof(_rotation))]
		private ElympicsVarConfig rotationConfig = new ElympicsVarConfig(tolerance: 1f);
		[SerializeField, ConfigForVar(nameof(_velocity))]
		private ElympicsVarConfig velocityConfig = new ElympicsVarConfig();
		[SerializeField, ConfigForVar(nameof(_angularVelocity))]
		private ElympicsVarConfig angularVelocityConfig = new ElympicsVarConfig(tolerance: 1f);
		[SerializeField, ConfigForVar(nameof(_mass))]
		private ElympicsVarConfig massConfig = new ElympicsVarConfig(false);
		[SerializeField, ConfigForVar(nameof(_drag))]
		private ElympicsVarConfig dragConfig = new ElympicsVarConfig(false);
		[SerializeField, ConfigForVar(nameof(_angularDrag))]
		private ElympicsVarConfig angularDragConfig = new ElympicsVarConfig(false);
		[SerializeField, ConfigForVar(nameof(_useGravity))]
		private ElympicsVarConfig useGravityConfig = new ElympicsVarConfig(false, 0f);

		private ElympicsVector3    _position;
		private ElympicsQuaternion _rotation;
		private ElympicsVector3    _velocity;
		private ElympicsVector3    _angularVelocity;
		private ElympicsFloat      _mass;
		private ElympicsFloat      _drag;
		private ElympicsFloat      _angularDrag;
		private ElympicsBool       _useGravity;

		private Rigidbody _rigidbody;
		private Rigidbody Rigidbody => _rigidbody ?? (_rigidbody = GetComponent<Rigidbody>());

		private bool _initialized;

		public void Initialize()
		{
			if (_initialized)
				return;

			_position = new ElympicsVector3(default, positionConfig);
			_rotation = new ElympicsQuaternion(default, rotationConfig);
			_velocity = new ElympicsVector3(default, velocityConfig);
			_angularVelocity = new ElympicsVector3(default, angularVelocityConfig);
			_mass = new ElympicsFloat(default, massConfig);
			_drag = new ElympicsFloat(default, dragConfig);
			_angularDrag = new ElympicsFloat(default, angularDragConfig);
			_useGravity = new ElympicsBool(default, useGravityConfig);

			_initialized = true;
		}

		public void OnPostStateDeserialize()
		{
			if (_position.EnabledSynchronization)
				Rigidbody.position = _position;
			if (_rotation.EnabledSynchronization)
				Rigidbody.rotation = _rotation;
			if (_velocity.EnabledSynchronization)
				Rigidbody.velocity = _velocity;
			if (_angularVelocity.EnabledSynchronization)
				Rigidbody.angularVelocity = _angularVelocity;
			if (_mass.EnabledSynchronization)
				Rigidbody.mass = _mass;
			if (_drag.EnabledSynchronization)
				Rigidbody.drag = _drag;
			if (_angularDrag.EnabledSynchronization)
				Rigidbody.angularDrag = _angularDrag;
			if (_useGravity.EnabledSynchronization)
				Rigidbody.useGravity = _useGravity;
		}

		public void OnPreStateSerialize()
		{
			if (_position.EnabledSynchronization)
				_position.Value = Rigidbody.position;
			if (_rotation.EnabledSynchronization)
				_rotation.Value = Rigidbody.rotation;
			if (_velocity.EnabledSynchronization)
				_velocity.Value = Rigidbody.velocity;
			if (_angularVelocity.EnabledSynchronization)
				_angularVelocity.Value = Rigidbody.angularVelocity;
			if (_mass.EnabledSynchronization)
				_mass.Value = Rigidbody.mass;
			if (_drag.EnabledSynchronization)
				_drag.Value = Rigidbody.drag;
			if (_angularDrag.EnabledSynchronization)
				_angularDrag.Value = Rigidbody.angularDrag;
			if (_useGravity.EnabledSynchronization)
				_useGravity.Value = Rigidbody.useGravity;
		}
	}
}
