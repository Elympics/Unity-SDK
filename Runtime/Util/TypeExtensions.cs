using System;
using System.Collections.Generic;

#nullable enable

namespace Elympics.Util
{
    public static class TypeExtensions
    {
        public static IEnumerable<Type> GetBaseTypes(this Type derivedType)
        {
            var type = derivedType;
            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }
    }
}
