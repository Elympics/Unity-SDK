using Elympics.Resolvers;
using MessagePack;
using MessagePack.Resolvers;
using UnityEngine;

namespace Elympics
{
    public class ResolverRegistrator
    {
        private static bool serializerRegistered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (serializerRegistered)
                return;

            StaticCompositeResolver.Instance.Register(
                GeneratedResolver.Instance,
                AttributeFormatterResolver.Instance,
                BuiltinResolver.Instance,
                PrimitiveObjectResolver.Instance,
                MissingTypesResolver.Instance
            );

            var option = MessagePackSerializerOptions.Standard.WithResolver(StaticCompositeResolver.Instance);

            MessagePackSerializer.DefaultOptions = option;
            serializerRegistered = true;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInitialize() => Initialize();
#endif
    }
}
