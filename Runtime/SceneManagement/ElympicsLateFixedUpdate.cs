using System;
using UnityEngine;

namespace Elympics
{
	public class ElympicsLateFixedUpdate : MonoBehaviour
	{
		internal Action LateFixedUpdateAction { get; set; }

		private void FixedUpdate()
		{
			LateFixedUpdateAction?.Invoke();
			LateFixedUpdateAction = null;
		}
	}
}
