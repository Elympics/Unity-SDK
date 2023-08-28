using Mono.Cecil;

namespace Elympics.Weaver.Extensions
{
    public static class ConstructorArguments
    {
        public static T GetValue<T>(this CustomAttribute customAttribute, string propertyName)
        {
            for (var i = 0; i < customAttribute.Properties.Count; i++)
            {
                var arguement = customAttribute.Properties[i];
                if (string.Equals(propertyName, arguement.Name, System.StringComparison.Ordinal))
                {
                    return (T)arguement.Argument.Value;
                }

            }
            return default;
        }
    }
}
