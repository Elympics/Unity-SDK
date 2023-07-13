using Elympics;
using UnityEngine;

public class TestServerAnimatonController : ElympicsMonoBehaviour
{
    private readonly int _movingAnimationBool = Animator.StringToHash("IsMoving");

    [SerializeField]
    public Animator _animator;

    private readonly ElympicsInt _tick = new();

    private void FixedUpdate()
    {
        _tick.Value++;
        if (_tick.Value == 200)
            _animator.SetBool(_movingAnimationBool, true);
    }
}
