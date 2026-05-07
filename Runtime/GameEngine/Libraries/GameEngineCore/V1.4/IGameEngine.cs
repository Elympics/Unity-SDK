#nullable enable
using System;

namespace GameEngineCore.V1._4
{
    public interface IGameEngine : _3.IGameEngine
    {
        event Action? Initialized;

        void Initialize(InitialMatchData initialMatchData);
    }
}
