using UnityEngine;

namespace Elympics
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class ElympicsRigidBody2DSynchronizer : MonoBehaviour, IStateSerializationHandler, IInitializable
    {
        [SerializeField, ConfigForVar(nameof(_position))]
        private ElympicsVarConfig positionConfig = new();
        [SerializeField, ConfigForVar(nameof(_rotation))]
        private ElympicsVarConfig rotationConfig = new(tolerance: 1f);
        [SerializeField, ConfigForVar(nameof(_velocity))]
        private ElympicsVarConfig velocityConfig = new();
        [SerializeField, ConfigForVar(nameof(_angularVelocity))]
        private ElympicsVarConfig angularVelocityConfig = new(tolerance: 1f);
        [SerializeField, ConfigForVar(nameof(_drag))]
        private ElympicsVarConfig dragConfig = new(false);
        [SerializeField, ConfigForVar(nameof(_angularDrag))]
        private ElympicsVarConfig angularDragConfig = new(false);
        [SerializeField, ConfigForVar(nameof(_inertia))]
        private ElympicsVarConfig inertiaConfig = new(false);
        [SerializeField, ConfigForVar(nameof(_mass))]
        private ElympicsVarConfig massConfig = new(false);
        [SerializeField, ConfigForVar(nameof(_gravityScale))]
        private ElympicsVarConfig gravityScaleConfig = new(false);
        [SerializeField, ConfigForVar(nameof(_isKinematic))]
        private ElympicsVarConfig isKinematicConfig = new(false, 0f);

        private ElympicsVector2 _position;
        private ElympicsFloat _rotation;
        private ElympicsVector2 _velocity;
        private ElympicsFloat _angularVelocity;
        private ElympicsFloat _drag;
        private ElympicsFloat _angularDrag;
        private ElympicsFloat _inertia;
        private ElympicsFloat _mass;
        private ElympicsFloat _gravityScale;
        private ElympicsBool _isKinematic;

        private bool SynchronizeMass => _mass.EnabledSynchronization && !Rigidbody2D.useAutoMass;

        private Rigidbody2D Rigidbody2D { get; set; }

        public void Initialize()
        {
            Rigidbody2D = GetComponent<Rigidbody2D>();

            _position = new ElympicsVector2(default, positionConfig);
            _rotation = new ElympicsFloat(default, rotationConfig);
            _velocity = new ElympicsVector2(default, velocityConfig);
            _angularVelocity = new ElympicsFloat(default, angularVelocityConfig);
            _drag = new ElympicsFloat(default, dragConfig);
            _angularDrag = new ElympicsFloat(default, angularDragConfig);
            _inertia = new ElympicsFloat(default, inertiaConfig);
            _mass = new ElympicsFloat(default, massConfig);
            _gravityScale = new ElympicsFloat(default, gravityScaleConfig);
            _isKinematic = new ElympicsBool(default, isKinematicConfig);
        }

        public void OnPostStateDeserialize()
        {
            if (_position.EnabledSynchronization)
                Rigidbody2D.position = _position.Value;
            if (_rotation.EnabledSynchronization)
                Rigidbody2D.rotation = _rotation.Value;
            if (_velocity.EnabledSynchronization)
                Rigidbody2D.velocity = _velocity.Value;
            if (_angularVelocity.EnabledSynchronization)
                Rigidbody2D.angularVelocity = _angularVelocity.Value;
            if (_drag.EnabledSynchronization)
                Rigidbody2D.drag = _drag.Value;
            if (_angularDrag.EnabledSynchronization)
                Rigidbody2D.angularDrag = _angularDrag.Value;
            if (_inertia.EnabledSynchronization)
                Rigidbody2D.inertia = _inertia.Value;
            if (SynchronizeMass)
                Rigidbody2D.mass = _mass.Value;
            if (_gravityScale.EnabledSynchronization)
                Rigidbody2D.gravityScale = _gravityScale.Value;
            if (_isKinematic.EnabledSynchronization)
                Rigidbody2D.isKinematic = _isKinematic.Value;
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
            if (_isKinematic.EnabledSynchronization)
                _isKinematic.Value = Rigidbody2D.isKinematic;
        }
    }
}
