using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MatchTcpLibrary
{
    public static class TaskTimeoutExtensions
    {
        public static async UniTask<T> WithTimeout<T>(this UniTask<T> task, TimeSpan timeout, CancellationTokenSource cancellationTokenSource = null)
        {
            using var timeoutCts = cancellationTokenSource != null
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token)
                : new CancellationTokenSource();

            var (winIndex, result, _) = await UniTask.WhenAny(task, TimeoutPlaceholder<T>(timeout, timeoutCts.Token));

            // The timeout placeholder finished (either by elapsing or because the linked token was cancelled).
            // In both cases the awaited task did not complete in time, so surface it as a timeout.
            if (winIndex != 0)
                throw new TimeoutException("Task timed out");

            timeoutCts.Cancel();
            return result;
        }

        public static async UniTask WithTimeout(this UniTask task, TimeSpan timeout, CancellationTokenSource cancellationTokenSource = null)
        {
            using var timeoutCts = cancellationTokenSource != null
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token)
                : new CancellationTokenSource();

            var winIndex = await UniTask.WhenAny(task, TimeoutPlaceholder(timeout, timeoutCts.Token));

            if (winIndex != 0)
                throw new TimeoutException("Task timed out");

            timeoutCts.Cancel();
        }

        private static async UniTask<T> TimeoutPlaceholder<T>(TimeSpan timeout, CancellationToken ct)
        {
            _ = await UniTask.Delay(timeout, DelayType.Realtime, cancellationToken: ct).SuppressCancellationThrow();
            return default;
        }

        private static async UniTask TimeoutPlaceholder(TimeSpan timeout, CancellationToken ct)
        {
            _ = await UniTask.Delay(timeout, DelayType.Realtime, cancellationToken: ct).SuppressCancellationThrow();
        }
    }
}
