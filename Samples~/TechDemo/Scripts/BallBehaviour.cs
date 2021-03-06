using Elympics;

namespace TechDemo
{
	public class BallBehaviour : ElympicsMonoBehaviour, IUpdatable
	{
		private readonly ElympicsInt _ticksAlive = new ElympicsInt();

		public void ElympicsUpdate()
		{
			_ticksAlive.Value++;
			if (_ticksAlive > 60)
				ElympicsDestroy(gameObject);
		}
	}
}
