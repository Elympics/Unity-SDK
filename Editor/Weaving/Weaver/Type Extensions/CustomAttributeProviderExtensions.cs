using System;
using Mono.Cecil;

namespace Elympics.Weaver.Extensions
{
    public static class CustomAttributeProviderExtensions
    {
        public static CustomAttribute GetCustomAttribute<T>(this ICustomAttributeProvider instance)
        {
            if (!instance.HasCustomAttributes)
                return null;

            var attributes = instance.CustomAttributes;

            for (var i = 0; i < attributes.Count; i++)
            {
                if (attributes[i].AttributeType.FullName.Equals(typeof(T).FullName, StringComparison.Ordinal))
                {
                    return attributes[i];
                }
            }
            return null;
        }
    }
}
