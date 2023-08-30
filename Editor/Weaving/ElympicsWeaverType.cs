using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Elympics.Editor
{
    public class ElympicsWeaverType
    {
        public readonly TypeReference Reference;

        private readonly ElympicsWeaverAssembly _asm;
        private readonly List<TypeDefinition> _baseDefinitions;
        private readonly Dictionary<string, MethodReference> _methods = new();
        private readonly Dictionary<string, MethodReference> _propertyGetters = new();

        public ElympicsWeaverType(ElympicsWeaverAssembly asm, Type type)
        {
            _asm = asm;
            var baseType = type;
            _baseDefinitions = new List<TypeDefinition>();
            while (baseType != null)
            {
                _baseDefinitions.Add(ImportTypeDefinition(asm, type));
                baseType = baseType.BaseType;
            }

            if (_baseDefinitions[0] != null)
                Reference = asm.Assembly.MainModule.ImportReference(_baseDefinitions[0]);
        }

        private static TypeDefinition ImportTypeDefinition(ElympicsWeaverAssembly asm, Type type) =>
            asm.Assembly.MainModule.ImportReference(type).Resolve();

        public MethodReference GetPropertyGetter(string name)
        {
            if (_propertyGetters.TryGetValue(name, out var methodRef))
                return methodRef;

            foreach (var baseType in _baseDefinitions)
            {
                var propertyDef = baseType.Properties.FirstOrDefault(x => x.Name == name);
                if (propertyDef == null)
                    continue;

                methodRef = _asm.Assembly.MainModule.ImportReference(propertyDef.GetMethod);
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

                methodRef = _asm.Assembly.MainModule.ImportReference(methodDef);
                _methods.Add(name, methodRef);
                return methodRef;
            }
            return null;
        }
    }
}
