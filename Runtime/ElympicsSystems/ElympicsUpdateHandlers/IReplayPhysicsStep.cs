namespace Elympics.ElympicsSystems
{
    /// <summary>
    /// A scene component that refreshes physics outside this assembly (2D physics) after a replay snapshot is applied,
    /// so this assembly doesn't link that physics module.
    /// </summary>
    internal interface IReplayPhysicsStep
    {
        void StepForReplay();
    }
}
