using System.Reflection;

#nullable enable

namespace Elympics
{
    internal static class MethodInfoExtensions
    {
        public static string GetFullName(this MethodBase method) =>
            (method.ReflectedType?.FullName ?? method.DeclaringType?.FullName) is { } typeName
                ? $"{typeName}.{method.Name}"
                : method.Name;
    }
}
