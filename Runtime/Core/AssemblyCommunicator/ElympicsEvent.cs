#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace Elympics.AssemblyCommunicator
{
    /// <typeparam name="T">Type of argument passed to this event.</typeparam>
    internal sealed class ElympicsEvent<T>
    {
        /// <summary>Number of garbage collected observers that needs to be met or exceeded for cleanup of <see cref="WeakReference{T}"/> list to be triggered.</summary>
        private const int CleanupThreshold = 5;

        private readonly List<WeakReference<IElympicsObserver<T>>> _observers = new();

        public void AddObserver(IElympicsObserver<T> observer)
        {
            ThrowIfAlreadyAdded(observer);
            _observers.Add(new(observer));
        }

        [Conditional("UNITY_ASSERTIONS")]
        private void ThrowIfAlreadyAdded(IElympicsObserver<T> observer)
        {
            if (_observers.Any(observerReference => observerReference.TryGetTarget(out var target) && target.Equals(observer)))
                throw new InvalidOperationException($"Can't add observer {observer}, because it was already added before.");
        }

        public void Raise(T argument)
        {
            var deadReferences = 0;

            foreach (var observerReference in _observers)
            {
                //Try to get target and then perform additional null check to skip destroyed game objects that are not garbage collected
                if (!observerReference.TryGetTarget(out var observer) || observer == null)
                    deadReferences++;
                else
                {
                    try
                    {
                        observer.OnEvent(argument);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Oservers {observer} failed to handle event {nameof(ElympicsEvent<T>)}<{nameof(T)}> raised with argument {argument} with exception:\n{e}");
                    }
                }
            }

            if (deadReferences >= CleanupThreshold)
                _ = _observers.RemoveAll(reference => !reference.TryGetTarget(out var observer) || observer == null);
        }
    }
}
