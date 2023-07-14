using Elympics;
using UnityEngine;

namespace TechDemo
{
    public class PlayerBehaviour : ElympicsMonoBehaviour, IUpdatable
    {
        private readonly int _fireAnimatorTrigger = Animator.StringToHash("Fire");
        private readonly int _movingAnimatorBool = Animator.StringToHash("IsMoving");

        [SerializeField] private Animator characterAnimator;
        [SerializeField] private float speed = 2;
        [SerializeField] private float force = 100;
        [SerializeField] private int fireDelay = 20;
        [SerializeField] private string ballPrefabName = "Ball";
        [SerializeField] private Transform ballAnchor;

        private readonly ElympicsInt _timerForDelay = new();
        private readonly ElympicsInt _timerForFiring = new();

        private Rigidbody _rigidbody;

        private bool IsFiring => _timerForDelay.Value > 0 || _timerForFiring.Value > 0;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _timerForDelay.ValueChanged += HandleFireDelayTimerChanged;
        }

        private void HandleFireDelayTimerChanged(int lastValue, int newValue)
        {
            if (lastValue <= 0 && newValue > 0)
                characterAnimator.SetTrigger(_fireAnimatorTrigger);
        }

        public void Move(float forwardAxis, float rightAxis)
        {
            if (IsFiring)
                return;
            var direction = new Vector3(rightAxis, 0, forwardAxis).normalized;
            var velocity = direction * speed;
            transform.LookAt(transform.position + velocity);
            _rigidbody.velocity = velocity;
            characterAnimator.SetBool(_movingAnimatorBool, _rigidbody.velocity.sqrMagnitude > 0.5f);
        }

        public void Fire()
        {
            if (IsFiring)
                return;
            _timerForDelay.Value = fireDelay;
            _rigidbody.velocity = Vector3.zero;
        }

        public void ElympicsUpdate()
        {
            if (_timerForDelay.Value > 0)
                DecreaseDelayTimer();
            if (_timerForFiring.Value > 0)
                DecreaseFiringTimer();
        }

        private void DecreaseDelayTimer()
        {
            _timerForDelay.Value--;
            if (_timerForDelay.Value <= 0)
            {
                SpawnBall();
                _timerForFiring.Value = fireDelay;
            }
        }

        private void DecreaseFiringTimer()
        {
            _timerForFiring.Value--;
        }

        private void SpawnBall()
        {
            var predictableFor = PredictableFor;
            var newBall = ElympicsInstantiate(ballPrefabName, predictableFor);
            newBall.transform.SetPositionAndRotation(ballAnchor.transform.position, ballAnchor.transform.rotation);
            newBall.GetComponent<Rigidbody>().AddForce((newBall.transform.forward + newBall.transform.up) * force);
        }
    }
}
