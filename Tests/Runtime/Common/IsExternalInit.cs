using JetBrains.Annotations;

// Required for init-only setters in records:
// https://docs.unity3d.com/2021.3/Documentation/Manual/CSharpCompiler.html#:~:text=Record%20support

namespace System.Runtime.CompilerServices
{
    [UsedImplicitly]
    internal static class IsExternalInit
    { }
}
