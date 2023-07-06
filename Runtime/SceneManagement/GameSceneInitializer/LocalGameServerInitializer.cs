using GameEngineCore.V1._3;
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
            var initialMatchData = new InitialMatchUserDatas(DebugPlayerListCreator.CreatePlayersList(elympicsGameConfig));
            _localGameEngineAdapter = gameEngineAdapter;
            _localGameEngineAdapter.Init(new LoggerNoop(), null);
            _localGameEngineAdapter.Init2(initialMatchData);

            Application.targetFrameRate = -1;
        }

        public override void Dispose()
        {
            base.Dispose();
            _localGameEngineAdapter = null;
        }
    }
}
