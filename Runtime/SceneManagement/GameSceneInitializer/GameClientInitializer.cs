namespace Elympics
{
	internal abstract class GameClientInitializer : GameSceneInitializer
	{
		private ElympicsClient _client;

		public override void Initialize(ElympicsClient client, ElympicsBot bot, ElympicsServer server, ElympicsGameConfig elympicsGameConfig)
		{
			_client = client;
			bot.Destroy();
			server.Destroy();
			InitializeClient(client, elympicsGameConfig);
		}

		public override void Dispose()
		{
			base.Dispose();
			if (_client != null && _client.Initialized)
				_client.MatchConnectClient.Disconnect();
		}

		protected abstract void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig);
	}
}