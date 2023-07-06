using TechDemo;
using UnityEngine;

public class PlayerJoystickInputProvider : MonoBehaviour, IInputProvider
{
    [SerializeField] private Joystick joystick;
    [SerializeField] private FireButton fireButton;

    public void GetRawInput(out float forwardMovement, out float rightMovement, out bool fire)
    {
        forwardMovement = Mathf.Clamp(joystick.Vertical + Input.GetAxis("Vertical"), -1, 1);
        rightMovement = Mathf.Clamp(joystick.Horizontal + Input.GetAxis("Horizontal"), -1, 1);
        fire = fireButton.IsPressed;
    }
}
