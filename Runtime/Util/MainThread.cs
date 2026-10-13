#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading;
using Elympics.Core.Logger;
using Elympics.Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace Elympics
{
    /// <summary>Posts work from any thread to UniTask's Update loop, which runs before any script's Update.</summary>
    internal static class MainThread
    {
        public static void Post(Action action) => WorkItem.Rent(action, default, null).Post();

        /// <summary>
        /// UniTask's queue is process-wide and outlives the scene: the action is dropped if <paramref name="token"/> is cancelled
        /// or <paramref name="owner"/> is destroyed by the time it runs.
        /// </summary>
        public static void Post(Action action, CancellationToken token, Object owner) => WorkItem.Rent(action, token, owner).Post();

        /// <summary>Pooled with a cached delegate, so posting from a network thread allocates no wrapper.</summary>
        private sealed class WorkItem
        {
            private const int MaxPooled = 64;
            private static readonly ConcurrentQueue<WorkItem> Pool = new();

            private readonly Action _run;
            private Action? _action;
            private CancellationToken _token;
            private Object? _owner;
            private bool _hasOwner;

            private WorkItem() => _run = Run;

            public static WorkItem Rent(Action action, CancellationToken token, Object? owner)
            {
                if (!Pool.TryDequeue(out var item))
                    item = new WorkItem();
                item._action = action;
                item._token = token;
                item._owner = owner;
                item._hasOwner = owner is not null;
                return item;
            }

            public void Post() => UniTask.Post(_run);

            private void Run()
            {
                var action = _action!;
                var dropped = _token.IsCancellationRequested || (_hasOwner && _owner == null);
                _action = null;
                _token = default;
                _owner = null;
                _hasOwner = false;
                if (Pool.Count < MaxPooled)
                    Pool.Enqueue(this);

                if (dropped)
                    return;
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    ElympicsLogger.LogException(e);
                }
            }
        }
    }
}
