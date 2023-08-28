using System;
using System.Linq;
using Mono.Cecil;

namespace Elympics.Editor
{
    public static class TypeReferenceExtensions
    {
        public static bool IsSubclassOf<T>(this TypeReference typeReference) => IsSubclassOf(typeReference, typeof(T));

        private static bool Is(this TypeReference typeReference, Type type)
        {
            if (typeReference == null)
                throw new ArgumentNullException(nameof(typeReference));

            if (typeReference.MetadataType == MetadataType.Void && type == typeof(void))
                return true;

            if (typeReference.IsValueType != type.IsValueType)
                return false;

            return typeReference.FullName == type.FullName;
        }

        private static bool TryResolve(this TypeReference typeReference, out TypeDefinition typeDefinition)
        {
            typeDefinition = null;
            try
            {
                typeDefinition = typeReference.Resolve();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsSubclassOf(this TypeReference typeReference, Type t)
        {
            if (typeReference.Is(t))
                return false;

            if (!typeReference.TryResolve(out var typeDefinition))
                return false;

            while (typeDefinition.BaseType != null)
            {
                if (typeDefinition.BaseType.Is(t))
                    return true;
                if (t.IsInterface && typeDefinition.Interfaces.Any(@interface => @interface.InterfaceType.Is(t)))
                    return true;
                if (!typeDefinition.BaseType.TryResolve(out typeDefinition))
                    return false;
            }

            return false;
        }
    }
}
