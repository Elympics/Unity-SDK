using Mono.Cecil;

namespace Elympics.Editor
{
    public class ElympicsWeaverAssembly
    {
        public readonly AssemblyDefinition Assembly;
        private ElympicsWeaverType _elympicsMonoBehaviour;
        private ElympicsWeaverType _elympicsBehaviour;
        private ElympicsWeaverType _elympicsRpcProperties;

        public ElympicsWeaverAssembly(AssemblyDefinition assembly) => Assembly = assembly;

        public ElympicsWeaverType ElympicsMonoBehaviour => ImportType<ElympicsMonoBehaviour>(ref _elympicsMonoBehaviour);
        public ElympicsWeaverType ElympicsRpcProperties => ImportType<ElympicsRpcProperties>(ref _elympicsRpcProperties);
        public ElympicsWeaverType ElympicsBehaviour => ImportType<ElympicsBehaviour>(ref _elympicsBehaviour);

        public ElympicsWeaverType ImportType<T>(ref ElympicsWeaverType importType) =>
            importType ??= new ElympicsWeaverType(this, typeof(T));
    }
}
