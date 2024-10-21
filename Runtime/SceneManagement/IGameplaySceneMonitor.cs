using System;

namespace Elympics
{
    public interface IGameplaySceneMonitor : IDisposable
    {
        public bool IsCurrentlyInMatch { get; }
        public event Action GameplayStarted;
        public event Action GameplayFinished;
        internal void GameConfigChanged(string newName)
        {

        }
    }
}
