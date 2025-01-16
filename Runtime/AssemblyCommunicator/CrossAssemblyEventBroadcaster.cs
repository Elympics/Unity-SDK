#nullable enable

using System;
using System.Linq;
using UnityEngine;

namespace Elympics.AssemblyCommunicator
{
    public static class CrossAssemblyEventBroadcaster
    {
        static CrossAssemblyEventBroadcaster()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name.Equals("Elympics")))
            {
                foreach (var type in assembly.GetTypes().Where(t => !t.IsInterface && t.GetInterfaces().Contains(typeof(ElympicsObserver))))
                {
                    if (type.GetInterfaces()
                        .Where(i =>
                            i != typeof(ElympicsObserver) &&
                            typeof(ElympicsObserver).IsAssignableFrom(i) &&
                            i.IsGenericType &&
                            !i.IsGenericTypeDefinition
                            )
                        .Any(i => i.GetGenericTypeDefinition() == typeof(ElympicsSdkObserver<>)))
                    {
                        Debug.LogError($"CrossAssemblyEventBroadcaster error - {type.Name} is located in Elympics assembly and implements ElympicsSdkObserver interface.");
                    }
                }
            }
            Debug.Log("CrossAssemblyEventBroadcaster checked all assemblies.");
        }

        public static int I => 0;
    }

    public interface ElympicsSdkObserver<T> : ElympicsObserver { }

    public interface ElympicsObserver { }
}
