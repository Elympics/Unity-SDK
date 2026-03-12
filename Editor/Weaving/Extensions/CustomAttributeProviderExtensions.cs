using System;
using System.Linq;
using Mono.Cecil;

namespace Elympics.Editor.Weaving.Extensions
{
    public static class CustomAttributeProviderExtensions
    {
        public static CustomAttribute GetCustomAttribute<T>(this ICustomAttributeProvider instance) =>
            instance.HasCustomAttributes
                ? instance.CustomAttributes.FirstOrDefault(attribute => attribute.AttributeType.FullName.Equals(typeof(T).FullName, StringComparison.Ordinal))
                : null;
    }
}
