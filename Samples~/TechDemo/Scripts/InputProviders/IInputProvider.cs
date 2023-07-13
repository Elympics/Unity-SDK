namespace TechDemo
{
    public interface IInputProvider
    {
        void GetRawInput(out float forwardMovement, out float rightMovement, out bool fire);
    }
}
