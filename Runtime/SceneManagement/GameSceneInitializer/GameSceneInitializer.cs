namespace Elympics
{
	internal abstract class GameSceneInitializer
	{
		public abstract void Initialize(ElympicsClient client, ElympicsBot bot, ElympicsServer server, ElympicsGameConfig elympicsGameConfig);
		public virtual  void Dispose() { }
	}
}