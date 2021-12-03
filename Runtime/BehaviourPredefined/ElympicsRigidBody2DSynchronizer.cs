using UnityEngine;

namespace Elympics
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Rigidbody2D))]
	public class ElympicsRigidBody2DSynchronizer : MonoBehaviour, IStateSerializationHandler, IInitializable
	{
		[SerializeField, ConfigForVar(nameof(_position))]
		private ElympicsVarConfig positionConfig = new ElympicsVarConfig();
		[SerializeField, ConfigForVar(nameof(_rotation))]
		private ElympicsVarConfig rotationConfig = new ElympicsVarConfig(tolerance: 1f);
		[SerializeField, ConfigForVar(nameof(_velocity))]
		private ElympicsVarConfig velocityConfig = new ElympicsVarConfig();
		[SerializeField, ConfigForVar(nameof(_angularVelocity))]
		private ElympicsVarConfig angularVelocityConfig = new ElympicsVarConfig(tolerance: 1f);
		[SerializeField, ConfigForVar(nameof(_drag))]
		private ElympicsVarConfig dragConfig = new ElympicsVarConfig(false);
		[SerializeField, ConfigForVar(nameof(_angularDrag))]
		private ElympicsVarConfig angularDragConfig = new ElympicsVarConfig(false);
		[SerializeField, ConfigForVar(nameof(_inertia))]
		private ElympicsVarConfig inertiaConfig = new ElympicsVarConfig(false);
		[SerializeField, ConfigForVar(nameof(_mass))]
		private ElympicsVarConfig massConfig = new ElympicsVarConfig(false);
		[SerializeField, ConfigForVar(nameof(_gravityScale))]
		private ElympicsVarConfig gravityScaleConfig = new ElympicsVarConfig(false);

		private ElympicsVector2 _position;
		private ElympicsFloat   _rotation;
		private ElympicsVector2 _velocity;
		private ElympicsFloat   _angularVelocity;
		private ElympicsFloat   _drag;
		private ElympicsFloat   _angularDrag;
		private ElympicsFloat   _inertia;
		private ElympicsFloat   _mass;
		private ElympicsFloat   _gravityScale;

		private bool SynchronizeMass => _mass.EnabledSynchronization && !Rigidbody2D.useAutoMass;

		private Rigidbody2D _rigidbody2D;
		private Rigidbody2D Rigidbody2D => _rigidbody2D ?? (_rigidbody2D = GetComponent<Rigidbody2D>());

		private bool _initialized;

		public void Initialize()
		{
			if (_initialized)
				return;

			_position = new ElympicsVector2(default, positionConfig);
			_rotation = new ElympicsFloat(default, rotationConfig);
			_velocity = new ElympicsVector2(default, velocityConfig);
			_angularVelocity = new ElympicsFloat(default, angularVelocityConfig);
			_drag = new ElympicsFloat(default, dragConfig);
			_angularDrag = new ElympicsFloat(default, angularDragConfig);
			_inertia = new ElympicsFloat(default, inertiaConfig);
			_mass = new ElympicsFloat(default, massConfig);
			_gravityScale = new ElympicsFloat(default, gravityScaleConfig);

			_initialized = true;
		}

		public void OnPostStateDeserialize()
		{
			if (_position.EnabledSynchronization)
				Rigidbody2D.position = _position;
			if (_rotation.EnabledSynchronization)
				Rigidbody2D.rotation = _rotation;
			if (_velocity.EnabledSynchronization)
				Rigidbody2D.velocity = _velocity;
			if (_angularVelocity.EnabledSynchronization)
				Rigidbody2D.angularVelocity = _angularVelocity;
			if (_drag.EnabledSynchronization)
				Rigidbody2D.drag = _drag;
			if (_angularDrag.EnabledSynchronization)
				Rigidbody2D.angularDrag = _angularDrag;
			if (_inertia.EnabledSynchronization)
				Rigidbody2D.inertia = _inertia;
			if (SynchronizeMass)
				Rigidbody2D.mass = _mass;
			if (_gravityScale.EnabledSynchronization)
				Rigidbody2D.gravityScale = _gravityScale;
		}

		public void OnPreStateSerialize()
		{
			if (_position.EnabledSynchronization)
				_position.Value = Rigidbody2D.position;
			if (_rotation.EnabledSynchronization)
				_rotation.Value = Rigidbody2D.rotation;
			if (_velocity.EnabledSynchronization)
				_velocity.Value = Rigidbody2D.velocity;
			if (_angularVelocity.EnabledSynchronization)
				_angularVelocity.Value = Rigidbody2D.angularVelocity;
			if (_drag.EnabledSynchronization)
				_drag.Value = Rigidbody2D.drag;
			if (_angularDrag.EnabledSynchronization)
				_angularDrag.Value = Rigidbody2D.angularDrag;
			if (_inertia.EnabledSynchronization)
				_inertia.Value = Rigidbody2D.inertia;
			if (SynchronizeMass)
				_mass.Value = Rigidbody2D.mass;
			if (_gravityScale.EnabledSynchronization)
				_gravityScale.Value = Rigidbody2D.gravityScale;
		}
	}
}
