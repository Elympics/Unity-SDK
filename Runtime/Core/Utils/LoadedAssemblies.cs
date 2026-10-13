using System;
using System.Collections.Generic;
using System.Reflection;

namespace Elympics
{
    internal static class LoadedAssemblies
    {
        /// <summary>The managed assemblies currently loaded by Unity.</summary>
        /// <remarks><c>AppDomain.GetAssemblies</c> is discouraged on CoreCLR; Unity 6.6 adds <c>CurrentAssemblies</c> in its place.</remarks>
        public static IEnumerable<Assembly> Get()
        {
#if UNITY_6000_6_OR_NEWER
            return UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies();
#else
            return AppDomain.CurrentDomain.GetAssemblies();
#endif
        }
    }
}
