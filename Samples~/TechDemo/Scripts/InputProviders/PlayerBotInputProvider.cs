using UnityEngine;

namespace TechDemo
{
    [RequireComponent(typeof(PlayerBehaviour))]
    public class PlayerBotInputProvider : MonoBehaviour, IInputProvider
    {
        [SerializeField] private PlayerBehaviour followedPlayerBehaviour;

        private PlayerBehaviour _playerBehaviour;

        private void Awake()
        {
            _playerBehaviour = GetComponent<PlayerBehaviour>();
        }

        public void GetRawInput(out float forwardMovement, out float rightMovement, out bool fire)
        {
            var myPosition = _playerBehaviour.transform.position;
            var enemyPosition = followedPlayerBehaviour.transform.position;
            var positionDiff = enemyPosition - myPosition;
            positionDiff = positionDiff.normalized * (positionDiff.magnitude > 3 ? 1f : 0);

            forwardMovement = positionDiff.z;
            rightMovement = positionDiff.x;
            fire = false;
        }
    }
}
