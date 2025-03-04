#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Debug = UnityEngine.Debug;

namespace Elympics.AssemblyCommunicator
{
    /// <summary>
    /// Allows an assembly to raise events that other assemblies can subscribe to.
    /// Each event has one argument and type of that argument identifies that event, so it needs to be unique to tha event across all assemblies using this class.
    /// </summary>
    /// <remarks>
    /// All members of this class are not thread safe and should be used from the main thread.
    /// This class should be used for application wide events, like ending a match.
    /// Each event should only be raised from one assembly.
    /// The assembly that raises an event should never subscribe to it, because this would introduce similar issues as singleton pattern.
    /// Use instance references and dependency injection within an assembly instead.
    /// </remarks>
    public static partial class CrossAssemblyEventBroadcaster
    {
        private static readonly BroadcastEventCollection Events = new();

        static CrossAssemblyEventBroadcaster() => ValidateObservers();

        [Conditional("UNITY_ASSERTIONS")]
        internal static void ValidateObservers()
        {
            //Skip test assemblies
            var types = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.GetName().Name.StartsWith("Elympics.Tests")).SelectMany(assembly => assembly.GetTypes());

            //For each type that is not an interface get a list of all interfaces it implements
            //and look for variants of IElympicsObserver<T> with type arguments that are illegal in the assembly that defines this type.
            foreach (var type in types.Where(t => !t.IsInterface))
            {
                foreach (var eventArgumentType in GetImplementedIElympicsObserverVariantTypes(type))
                {
                    if (!IsValidObserverType(type))
                        Debug.Assert(false, $"{type.Name} implements {nameof(IElympicsObserver<object>)}<{eventArgumentType.Name}> interface, but {type.Name} is a struct and can't be used as observer of {nameof(ElympicsEvent<object>)}, because it relies on weak references (boxed struct would become eligible for GC right after being added as an observer).");
                    else if (!IsValidEventArgumentTypeForObserverInAssembly(type.Assembly, eventArgumentType))
                        Debug.Assert(false, $"{type.Name} implements {nameof(IElympicsObserver<object>)}<{eventArgumentType.Name}> interface, but {eventArgumentType.Name} can't be used as argument type for {nameof(ElympicsEvent<object>)} in assembly {type.Assembly.GetName().Name}.");
                }
            }
        }

        private static IEnumerable<Type> GetImplementedIElympicsObserverVariantTypes(Type type) =>
            type.GetInterfaces()
            .Where(interfaceType =>
                interfaceType.IsGenericType &&
                !interfaceType.IsGenericTypeDefinition &&
                interfaceType.GetGenericTypeDefinition().Equals(typeof(IElympicsObserver<>))
            )
            .Select(i => i.GetGenericArguments()[0]);

        internal static bool IsValidObserverType(Type type) => !type.IsValueType; //Boxed struct will become eligible for garbage collection as soon as it is added when it's wrapped in WeakReference, so using structs here would be pointless
        internal static bool IsValidEventArgumentTypeForObserverInAssembly(Assembly observerAssembly, Type argumentType) => !argumentType.Assembly.Equals(observerAssembly); //Assemblies can't subscribe to their own events (see class summary for details)

        private static void AddEvent<T>() => Events.AddEvent(new ElympicsEvent<T>());

        /// <summary>Calls <see cref="IElympicsObserver{T}.OnEvent(T)"/> on each observer registered with <see cref="AddObserver{T}(IElympicsObserver{T})"/>.</summary>
        /// <typeparam name="T">Type of the raised event's argument that uniquely identifies that event.</typeparam>
        public static void RaiseEvent<T>(T argument)
        {
            if (Events.TryGetEvent<T>(out var elympicsEvent))
                elympicsEvent.Raise(argument);
        }

        /// <summary>Registers <paramref name="observer"/> on which <see cref="IElympicsObserver{T}.OnEvent(T)"/> will be called whenever <see cref="RaiseEvent{T}(T)"/> is called.</summary>
        /// <remarks>
        /// <paramref name="observer"/> is stored as <see cref="WeakReference{T}"/>, so passing it to this method won't prevent it from being garbage collected.
        /// <paramref name="observer"/> will stop receiving events once it is garbage collected (or destroyed in case of game objects).
        /// </remarks>
        /// <typeparam name="T">Type of the observed event's argument that uniquely identifies that event.</typeparam>
        public static void AddObserver<T>(IElympicsObserver<T> observer) => GetOrAddEvent<T>().AddObserver(observer);

        private static ElympicsEvent<T> GetOrAddEvent<T>()
        {
            if (!Events.TryGetEvent<T>(out var elympicsEvent))
            {
                elympicsEvent = new();
                Events.AddEvent(elympicsEvent);
            }

            return elympicsEvent;
        }
    }
}
