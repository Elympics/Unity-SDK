using System;
using Elympics.AssemblyCommunicator;
using Elympics.AssemblyCommunicator.Events;
using UnityEngine.SceneManagement;

#nullable enable

namespace Elympics
{
    internal class GameplaySceneMonitor : IGameplaySceneMonitor
    {
        public event Action? GameplayStarted;
        public event Action? GameplayFinished;
        public bool IsCurrentlyInMatch { get; private set; }

        private string? _pendingNewPath;
        private string _gameplayScenePath;

        public GameplaySceneMonitor(string scenePath)
        {
            _gameplayScenePath = scenePath;
            SceneManager.sceneLoaded += OnGameplaySceneLoaded;
            SceneManager.sceneUnloaded += OnGameplaySceneUnloaded;
        }

        private void OnGameplaySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != _gameplayScenePath)
                return;

            IsCurrentlyInMatch = true;
            GameplayStarted?.Invoke();
        }
        private void OnGameplaySceneUnloaded(Scene scene)
        {
            if (scene.path != _gameplayScenePath)
                return;

            IsCurrentlyInMatch = false;
            UpdateGameplayScenePathFromPending();
            GameplayFinished?.Invoke();
            CrossAssemblyEventBroadcaster.RaiseEvent(new GameplayFinished());
        }
        private void UpdateGameplayScenePathFromPending()
        {
            if (_pendingNewPath is null)
                return;
            _gameplayScenePath = _pendingNewPath;
            _pendingNewPath = null;
        }

        void IGameplaySceneMonitor.GameConfigChanged(string newScenePath)
        {
            if (IsCurrentlyInMatch)
                _pendingNewPath = newScenePath;
            else
                _gameplayScenePath = newScenePath;
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnGameplaySceneLoaded;
            SceneManager.sceneUnloaded -= OnGameplaySceneUnloaded;
        }
    }
}
