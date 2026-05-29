using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MatchTcpLibrary
{
    public static class UniTaskTimeoutExtensions
    {
        public static async UniTask<T> WithTimeout<T>(this UniTask<T> task, TimeSpan timeout, CancellationToken ct = default)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            var (winIndex, result, _) = await UniTask.WhenAny(task, TimeoutPlaceholder<T>(timeout, timeoutCts.Token));

            // The timeout placeholder finished (if the linked token is canceled, OperationCanceledException is thrown).
            if (winIndex != 0)
                throw new TimeoutException("Task timed out");

            timeoutCts.Cancel();
            return result;
        }

        public static async UniTask WithTimeout(this UniTask task, TimeSpan timeout, CancellationToken ct = default)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            var winIndex = await UniTask.WhenAny(task, TimeoutPlaceholder(timeout, timeoutCts.Token));

            // The timeout placeholder finished (if the linked token is canceled, OperationCanceledException is thrown).
            if (winIndex != 0)
                throw new TimeoutException("Task timed out");

            timeoutCts.Cancel();
        }

        private static async UniTask<T> TimeoutPlaceholder<T>(TimeSpan timeout, CancellationToken ct)
        {
            await UniTask.Delay(timeout, DelayType.Realtime, cancellationToken: ct);
            return default;
        }

        private static async UniTask TimeoutPlaceholder(TimeSpan timeout, CancellationToken ct) =>
            await UniTask.Delay(timeout, DelayType.Realtime, cancellationToken: ct);
    }
}
