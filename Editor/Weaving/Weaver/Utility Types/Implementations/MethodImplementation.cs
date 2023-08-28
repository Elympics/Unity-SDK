using Mono.Cecil;

namespace Elympics.Weaver
{
    public struct MethodImplementation
    {
        public MethodReference reference;
        public MethodDefinition definition;
        private readonly ModuleDefinition _module;

        public MethodImplementation(ModuleDefinition module, MethodDefinition methodDefinition)
        {
            _module = module;
            reference = _module.ImportReference(methodDefinition);
            definition = reference.Resolve();
        }
    }
}
