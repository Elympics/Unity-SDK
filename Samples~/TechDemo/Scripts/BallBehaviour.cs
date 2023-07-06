using Elympics;

namespace TechDemo
{
    public class BallBehaviour : ElympicsMonoBehaviour, IUpdatable
    {
        private readonly ElympicsInt _ticksAlive = new();

        public void ElympicsUpdate()
        {
            _ticksAlive.Value++;
            if (_ticksAlive.Value > 60)
                ElympicsDestroy(gameObject);
        }
    }
}
