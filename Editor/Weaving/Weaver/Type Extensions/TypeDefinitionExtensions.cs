using System;
using Mono.Cecil;

namespace Elympics.Weaver.Extensions
{
    public static class TypeDefinitionExtensions
    {
        /// <summary>
        /// Finds a method from a type based on it's name. 
        /// </summary>
        public static MethodDefinition GetMethod(this TypeDefinition instance, string name)
        {
            for (var i = 0; i < instance.Methods.Count; i++)
            {
                var methodDef = instance.Methods[i];

                if (string.CompareOrdinal(methodDef.Name, name) == 0)
                {
                    return methodDef;
                }
            }
            return null;
        }

        public static MethodDefinition GetMethod(this TypeDefinition instance, string name, params Type[] parameterTypes)
        {
            for (var i = 0; i < instance.Methods.Count; i++)
            {
                var methodDefinition = instance.Methods[i];

                if (!string.Equals(methodDefinition.Name, name, StringComparison.Ordinal) ||
                    parameterTypes.Length != methodDefinition.Parameters.Count)
                {
                    continue;
                }

                var result = methodDefinition;
                for (var x = methodDefinition.Parameters.Count - 1; x >= 0; x--)
                {
                    var parameter = methodDefinition.Parameters[x];
                    if (!string.Equals(parameter.ParameterType.Name, parameterTypes[x].Name, StringComparison.Ordinal))
                    {
                        break;
                    }

                    if (x == 0)
                    {
                        return result;
                    }
                }
            }
            return null;
        }

        public static MethodDefinition GetMethod(this TypeDefinition instance, string name, params TypeReference[] parameterTypes)
        {
            if (instance.Methods != null)
            {
                for (var i = 0; i < instance.Methods.Count; i++)
                {
                    var methodDefinition = instance.Methods[i];
                    if (string.Equals(methodDefinition.Name, name, StringComparison.Ordinal) // Names Match
                        && parameterTypes.Length == methodDefinition.Parameters.Count) // The same number of parameters
                    {
                        var result = methodDefinition;
                        for (var x = methodDefinition.Parameters.Count - 1; x >= 0; x--)
                        {
                            var parameter = methodDefinition.Parameters[x];
                            if (!string.Equals(parameter.ParameterType.Name, parameterTypes[x].Name, StringComparison.Ordinal))
                            {
                                break;
                            }

                            if (x == 0)
                            {
                                return result;
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Finds a method from a type based on it's name and argument count
        /// </summary>
        public static MethodDefinition GetMethod(this TypeDefinition instance, string name, int argCount)
        {
            for (var i = 0; i < instance.Methods.Count; i++)
            {
                var methodDef = instance.Methods[i];

                if (string.CompareOrdinal(methodDef.Name, name) == 0 && methodDef.Parameters.Count == argCount)
                {
                    return methodDef;
                }
            }
            return null;
        }

        public static PropertyDefinition GetProperty(this TypeDefinition instance, string name)
        {
            for (var i = 0; i < instance.Properties.Count; i++)
            {
                var preopertyDef = instance.Properties[i];

                // Properties can only have one argument or they are an indexer. 
                if (string.CompareOrdinal(preopertyDef.Name, name) == 0 && preopertyDef.Parameters.Count == 0)
                {
                    return preopertyDef;
                }
            }
            return null;
        }
    }
}
