using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Elympics.Core.Utils
{
    internal static class ReflectionExtensions
    {
        public static bool HasAttribute<T>(this Type type) where T : Attribute => type.GetCustomAttribute<T>() != null;

        public static bool TryGetAttribute<T>(this Type type, [MaybeNullWhen(false)] out T attribute) where T : Attribute
        {
            attribute = type.GetCustomAttribute<T>();
            return attribute != null;
        }
    }
}
