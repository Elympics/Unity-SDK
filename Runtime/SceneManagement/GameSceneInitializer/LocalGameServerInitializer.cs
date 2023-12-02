using GameEngineCore.V1._4;
using UnityEngine;

namespace Elympics
{
    internal class LocalGameServerInitializer : GameServerInitializer
    {
        protected override bool HandlingBotsOverride => true;
        protected override bool HandlingClientsOverride => true;

        private GameEngineAdapter _localGameEngineAdapter;

        protected override void InitializeGameServer(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter)
        {
            _localGameEngineAdapter = gameEngineAdapter;
            _localGameEngineAdapter.Initialize(new InitialMatchData { UserData = DebugPlayerListCreator.CreatePlayersList(elympicsGameConfig) });

            Application.targetFrameRate = -1;
        }

        public override void Dispose()
        {
            base.Dispose();
            _localGameEngineAdapter = null;
        }
    }
}
