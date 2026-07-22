#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MatchTcpLibrary
{
    internal static class UniTaskRetryExtensions
    {
        public static readonly Type[] DefaultAcceptedError =
        {
            typeof(TimeoutException),
        };

        public static async UniTask<T> WithRetry<T>(this Func<T> func, int maxRetries = 0, TimeSpan? delay = null, Type[]? acceptedErrors = null, Action<int>? onRetry = null, CancellationToken ct = default)
        {
            acceptedErrors ??= DefaultAcceptedError;

            var exceptions = new List<Exception>();
            for (var i = 0; maxRetries >= 0; i++, maxRetries--)
            {
                if (i > 0)
                {
                    onRetry?.Invoke(i);
                    if (delay.HasValue)
                        await UniTask.Delay(delay.Value, DelayType.Realtime, cancellationToken: ct);
                }

                try
                {
                    return func();
                }
                catch (Exception e) when (acceptedErrors.Any(acceptedError => acceptedError.IsAssignableFrom(e.GetType())))
                {
                    exceptions.Add(e);
                }
            }
            throw new AggregateException(exceptions);
        }

        public static async UniTask WithRetry(this Action action, int maxRetries = 0, TimeSpan? delay = null, Type[]? acceptedErrors = null, Action<int>? onRetry = null, CancellationToken ct = default)
        {
            acceptedErrors ??= DefaultAcceptedError;

            var exceptions = new List<Exception>();
            for (var i = 0; maxRetries >= 0; i++, maxRetries--)
            {
                if (i > 0)
                {
                    onRetry?.Invoke(i);
                    if (delay.HasValue)
                        await UniTask.Delay(delay.Value, DelayType.Realtime, cancellationToken: ct);
                }

                try
                {
                    action();
                }
                catch (Exception e) when (acceptedErrors.Any(acceptedError => acceptedError.IsAssignableFrom(e.GetType())))
                {
                    exceptions.Add(e);
                }
            }
            throw new AggregateException(exceptions);
        }

        public static async UniTask<T> WithRetry<T>(this Func<UniTask<T>> awaitableFunc, int maxRetries = 0, TimeSpan? delay = null, Type[]? acceptedErrors = null, Action<int>? onRetry = null, CancellationToken ct = default)
        {
            acceptedErrors ??= DefaultAcceptedError;

            var exceptions = new List<Exception>();
            for (var i = 0; maxRetries >= 0; i++, maxRetries--)
            {
                if (i > 0)
                {
                    onRetry?.Invoke(i);
                    if (delay.HasValue)
                        await UniTask.Delay(delay.Value, DelayType.Realtime, cancellationToken: ct);
                }

                try
                {
                    return await awaitableFunc();
                }
                catch (Exception e) when (acceptedErrors.Any(acceptedError => acceptedError.IsAssignableFrom(e.GetType())))
                {
                    exceptions.Add(e);
                }
            }
            throw new AggregateException(exceptions);
        }

        public static async UniTask WithRetry(this Func<UniTask> awaitableAction, int maxRetries = 0, TimeSpan? delay = null, Type[]? acceptedErrors = null, Action<int>? onRetry = null, CancellationToken ct = default)
        {
            acceptedErrors ??= DefaultAcceptedError;

            var exceptions = new List<Exception>();
            for (var i = 0; maxRetries >= 0; i++, maxRetries--)
            {
                if (i > 0)
                {
                    onRetry?.Invoke(i);
                    if (delay.HasValue)
                        await UniTask.Delay(delay.Value, DelayType.Realtime, cancellationToken: ct);
                }

                try
                {
                    await awaitableAction();
                }
                catch (Exception e) when (acceptedErrors.Any(acceptedError => acceptedError.IsAssignableFrom(e.GetType())))
                {
                    exceptions.Add(e);
                }
            }
            throw new AggregateException(exceptions);
        }
    }
}
