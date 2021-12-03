using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.NewTicTacToe
{
	public class FullPhysicsMovingCubeController : MonoBehaviour
	{
		[SerializeField] private List<Rigidbody> cubeRigidbodies = null;

		private void Start()
		{
			StartCoroutine(AddGravityForRigidBody());
		}

		private IEnumerator AddGravityForRigidBody()
		{
			yield return new WaitForSeconds(5);
			foreach (var cubeRigidbody in cubeRigidbodies)
				cubeRigidbody.useGravity = true;
		}
	}
}
