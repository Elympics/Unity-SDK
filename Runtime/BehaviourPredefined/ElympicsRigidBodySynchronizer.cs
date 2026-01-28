using UnityEngine;

namespace Elympics
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class ElympicsRigidBodySynchronizer : MonoBehaviour, IStateSerializationHandler, IInitializable
    {
        [SerializeField, ConfigForVar(nameof(_position))]
        private ElympicsVarConfig positionConfig = new();
        [SerializeField, ConfigForVar(nameof(_rotation))]
        private ElympicsVarConfig rotationConfig = new(tolerance: 1f);
        [SerializeField, ConfigForVar(nameof(_velocity))]
        private ElympicsVarConfig velocityConfig = new();
        [SerializeField, ConfigForVar(nameof(_angularVelocity))]
        private ElympicsVarConfig angularVelocityConfig = new(tolerance: 1f);
        [SerializeField, ConfigForVar(nameof(_mass))]
        private ElympicsVarConfig massConfig = new(false);
        [SerializeField, ConfigForVar(nameof(_drag))]
        private ElympicsVarConfig dragConfig = new(false);
        [SerializeField, ConfigForVar(nameof(_angularDrag))]
        private ElympicsVarConfig angularDragConfig = new(false);
        [SerializeField, ConfigForVar(nameof(_useGravity))]
        private ElympicsVarConfig useGravityConfig = new(false, 0f);
        [SerializeField, ConfigForVar(nameof(_isKinematic))]
        private ElympicsVarConfig isKinematicConfig = new(false, 0f);

        private ElympicsVector3 _position;
        private ElympicsQuaternion _rotation;
        private ElympicsVector3 _velocity;
        private ElympicsVector3 _angularVelocity;
        private ElympicsFloat _mass;
        private ElympicsFloat _drag;
        private ElympicsFloat _angularDrag;
        private ElympicsBool _useGravity;
        private ElympicsBool _isKinematic;

        private bool _hasPendingUpdate;

        private Rigidbody Rigidbody { get; set; }

        public void Initialize()
        {
            Rigidbody = GetComponent<Rigidbody>();

            _position = new ElympicsVector3(default, positionConfig);
            _rotation = new ElympicsQuaternion(default, rotationConfig);
            _velocity = new ElympicsVector3(default, velocityConfig);
            _angularVelocity = new ElympicsVector3(default, angularVelocityConfig);
            _mass = new ElympicsFloat(default, massConfig);
            _drag = new ElympicsFloat(default, dragConfig);
            _angularDrag = new ElympicsFloat(default, angularDragConfig);
            _useGravity = new ElympicsBool(default, useGravityConfig);
            _isKinematic = new ElympicsBool(default, isKinematicConfig);
        }

        public void OnPostStateDeserialize()
        {
            //If Rigidbody is disabled, changes to some of its properties are ignored, so we have to wait and only apply them once the object is enabled
            if (isActiveAndEnabled)
                ApplyValues();
            else
                _hasPendingUpdate = true;
        }

        private void OnEnable()
        {
            if (_hasPendingUpdate)
                ApplyValues();
        }

        private void ApplyValues()
        {
            if (_position.EnabledSynchronization)
                Rigidbody.position = _position.Value;
            if (_rotation.EnabledSynchronization)
                Rigidbody.rotation = _rotation.Value;
            if (_velocity.EnabledSynchronization)
                Rigidbody.velocity = _velocity.Value;
            if (_angularVelocity.EnabledSynchronization)
                Rigidbody.angularVelocity = _angularVelocity.Value;
            if (_mass.EnabledSynchronization)
                Rigidbody.mass = _mass.Value;
            if (_drag.EnabledSynchronization)
                Rigidbody.drag = _drag.Value;
            if (_angularDrag.EnabledSynchronization)
                Rigidbody.angularDrag = _angularDrag.Value;
            if (_useGravity.EnabledSynchronization)
                Rigidbody.useGravity = _useGravity.Value;
            if (_isKinematic.EnabledSynchronization)
                Rigidbody.isKinematic = _isKinematic.Value;
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
            if (_isKinematic.EnabledSynchronization)
                _isKinematic.Value = Rigidbody.isKinematic;
        }
    }
}
