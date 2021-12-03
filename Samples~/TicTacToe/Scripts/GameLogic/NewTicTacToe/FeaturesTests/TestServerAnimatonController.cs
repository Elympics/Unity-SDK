using Elympics;
using UnityEngine;

public class TestServerAnimatonController : ElympicsMonoBehaviour
{
	private readonly int MovingAnimationBool = Animator.StringToHash("IsMoving");

	[SerializeField]
	public Animator _animator = null;

	private readonly ElympicsInt _tick = new ElympicsInt();

	private void FixedUpdate()
	{
		_tick.Value++;
		if (_tick == 200)
			_animator.SetBool(MovingAnimationBool, true);
	}
}