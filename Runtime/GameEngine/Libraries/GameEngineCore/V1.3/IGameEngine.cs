#nullable enable
using System;

namespace GameEngineCore.V1._3
{
    public interface IGameEngine : _2.IGameEngine
    {
        new event Action<ResultMatchUserDatas>? GameEnded;

        void Init2(InitialMatchUserDatas initialMatchUserDatas);
    }
}
