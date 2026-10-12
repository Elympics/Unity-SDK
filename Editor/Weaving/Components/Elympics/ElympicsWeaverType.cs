using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Elympics.Editor.Weaving.Components.Elympics
{
    internal class ElympicsWeaverType
    {
        public TypeDefinition Reference => _baseDefinitions[0];

        private readonly AssemblyDefinition _assembly;
        private readonly List<TypeDefinition> _baseDefinitions;
        private readonly Dictionary<string, MethodReference> _methods = new();
        private readonly Dictionary<string, MethodReference> _propertyGetters = new();

        public ElympicsWeaverType(AssemblyDefinition assembly, Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            _assembly = assembly;
            var baseDefinition = assembly.MainModule.ImportReference(type).Resolve()
                ?? throw new InvalidOperationException("Could not resolve type: " + type.FullName);
            _baseDefinitions = new List<TypeDefinition>();
            while (baseDefinition != null)
            {
                _baseDefinitions.Add(baseDefinition);
                baseDefinition = baseDefinition.BaseType?.Resolve();
            }
        }

        public MethodReference GetPropertyGetter(string name)
        {
            if (_propertyGetters.TryGetValue(name, out var methodRef))
                return methodRef;

            foreach (var baseType in _baseDefinitions)
            {
                var propertyDef = baseType.Properties.FirstOrDefault(x => x.Name == name);
                if (propertyDef == null)
                    continue;

                methodRef = _assembly.MainModule.ImportReference(propertyDef.GetMethod);
                _propertyGetters.Add(name, methodRef);
                return methodRef;
            }
            return null;
        }

        public MethodReference GetMethod(string name)
        {
            if (_methods.TryGetValue(name, out var methodRef))
                return methodRef;

            foreach (var baseType in _baseDefinitions)
            {
                var methodDef = baseType.Methods.FirstOrDefault(x => x.Name == name);
                if (methodDef == null)
                    continue;

                methodRef = _assembly.MainModule.ImportReference(methodDef);
                _methods.Add(name, methodRef);
                return methodRef;
            }
            return null;
        }

        private MethodReference GetMethod(string name, params TypeReference[] parameterTypes)
        {
            var key = $"{name}({string.Join(", ", parameterTypes.Select(t => t.FullName))})";
            if (_methods.TryGetValue(key, out var methodRef))
                return methodRef;

            foreach (var baseType in _baseDefinitions)
            {
                var methodDef = baseType.Methods.Where(m => m.Name == name && m.Parameters.Count == parameterTypes.Length)
                    .FirstOrDefault(m => m.Parameters.All(p => p.ParameterType == parameterTypes[p.Index]));
                if (methodDef == null)
                    continue;

                methodRef = _assembly.MainModule.ImportReference(methodDef);
                _methods.Add(key, methodRef);
                return methodRef;
            }
            return null;
        }

        public MethodReference GetConstructor(params TypeReference[] parameterTypes) => GetMethod(".ctor", parameterTypes);
    }
}
