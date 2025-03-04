#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elympics.AssemblyCommunicator
{
    public static partial class CrossAssemblyEventBroadcaster
    {
        /// <summary>
        /// Collection of events that can be looked up by their argument type.
        /// Each argument type can have only one event associated with it.
        /// </summary>
        internal class BroadcastEventCollection
        {
            /// <summary>Stores different variants of <see cref="ElympicsEvent{T}"/> as values using generic type arguments as keys.</summary>
            private readonly Dictionary<Type, object> _events = new();

            public void AddEvent<T>(ElympicsEvent<T> e)
            {
                if (!TryAddEvent(e))
                    throw new ArgumentException($"{nameof(ElympicsEvent<object>)} for type {nameof(T)} is already registered.", nameof(e));
            }

            public bool TryAddEvent<T>(ElympicsEvent<T> e) => _events.TryAdd(typeof(T), e);

            public ElympicsEvent<T> GetEvent<T>()
            {
                if (TryGetEvent<T>(out var e))
                    return e;
                else
                    throw new ArgumentException($"{nameof(ElympicsEvent<object>)} for type {nameof(T)} not found.", nameof(e));
            }

            public bool TryGetEvent<T>([MaybeNullWhen(false)] out ElympicsEvent<T> e)
            {
                if (!_events.TryGetValue(typeof(T), out var value))
                {
                    e = null;
                    return false;
                }

                //Type safety is already guarnateed by how event adding is implemented,
                //so we can skip type safety checks and use unsafe reference cast to save time
                e = Unsafe.As<ElympicsEvent<T>>(value);
                return true;
            }
        }
    }
}
