using UnityEngine;
using Elympics;

namespace GameLogic.NewTicTacToe
{
	[RequireComponent(typeof(Rigidbody))]
	public class RealtimeKinematicCubeController : MonoBehaviour, IUpdatable
	{
		private bool      _directionRight;
		private Rigidbody _playerRigidbody;

		private void Start()
		{
			_playerRigidbody = GetComponent<Rigidbody>();
		}

		public void ElympicsUpdate()
		{
			if (_playerRigidbody.position.x > 2)
				_directionRight = false;
			else if (_playerRigidbody.position.x < -2)
				_directionRight = true;

			var speed = (_directionRight ? Vector3.right : Vector3.left) / 20;
			_playerRigidbody.MovePosition(_playerRigidbody.position + speed);
			// Debug.Log($"[{(PlayerId == ElympicsPlayer.WORLD_ID ? "Server" : PlayerId.ToString())}] Current position {_playerRigidbody.position.x}");
		}
	}
}
