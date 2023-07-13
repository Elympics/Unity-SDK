namespace Elympics
{
    internal partial class ElympicsBehaviourEditor
    {
        private bool IgnoreComponent(ElympicsBehaviour behaviour)
            => behaviour.TryGetComponent<ElympicsFactory>(out _)
            || behaviour.TryGetComponent<ElympicsUnityPhysicsSimulator>(out _)
            || behaviour.TryGetComponent<ServerLogBehaviour>(out _);
    }
}
