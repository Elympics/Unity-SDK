using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Lobby.InternalModels.FromLobby;

#nullable enable

namespace Elympics.Lobby
{
    internal static class ResultUtils
    {
        public static async UniTask<TResult> WaitForResult<TResult, TAction>(TimeSpan timeout, Func<UniTaskCompletionSource<TResult>, TAction> handlerFactory, Action<TAction>? onBeforeWait = null, Action<TAction>? onAfterWait = null, CancellationToken ct = default)
            where TAction : Delegate
        {
            var tcs = new UniTaskCompletionSource<TResult>();
            using var delayCts = new CancellationTokenSource();
            using var timer = delayCts.CancelAfterSlim(timeout, DelayType.Realtime);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, delayCts.Token);
            using var registration = linkedCts.Token.RegisterWithoutCaptureExecutionContext(tcs.TrySetCanceledAndIgnoreResult);

            var handler = handlerFactory(tcs);
            onBeforeWait?.Invoke(handler);
            try
            {
                var (canceled, data) = await tcs.Task.SuppressCancellationThrow();
                if (!canceled)
                    return data;
            }
            finally
            {
                onAfterWait?.Invoke(handler);
            }

            ct.ThrowIfCancellationRequested();
            throw new LobbyOperationException($"Operation {nameof(WaitForResult)} timed out");
        }

        public static async UniTask<T> WithTimeout<T>(this UniTaskCompletionSource<T> tcs, TimeSpan timeout, CancellationToken ct = default)
        {
            using var delayCts = new CancellationTokenSource();
            using var timer = delayCts.CancelAfterSlim(timeout, DelayType.Realtime);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, delayCts.Token);
            using var registration = linkedCts.Token.RegisterWithoutCaptureExecutionContext(tcs.TrySetCanceledAndIgnoreResult);

            var (canceled, data) = await tcs.Task.SuppressCancellationThrow();
            if (!canceled)
                return data;

            throw new LobbyOperationException($"Operation {nameof(WithTimeout)} timed out");
        }


        public static async UniTask WaitUntil(Func<bool> predicate, TimeSpan timeout, CancellationToken ct = default)
        {
            using var delayCts = new CancellationTokenSource();
            using var timer = delayCts.CancelAfterSlim(timeout, DelayType.Realtime);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, delayCts.Token);

            var canceled = await UniTask.WaitUntil(predicate, cancellationToken: linkedCts.Token).SuppressCancellationThrow();
            if (!canceled)
                return;

            ct.ThrowIfCancellationRequested();
            throw new LobbyOperationException($"Operation {nameof(WaitUntil)} timed out");
        }

        public static Action<IFromLobby, IPromise<ValueTuple>> GetOperationResultHandler(Guid operationId)
        {
            return HandleOperationResult;

            void HandleOperationResult(IFromLobby message, IPromise<ValueTuple> tcs)
            {
                if (message is not OperationResultDto result
                    || result.OperationId != operationId)
                    return;
                _ = result.Success ? tcs.TrySetResult(new ValueTuple()) : tcs.TrySetException(new LobbyOperationException(result));
            }
        }
    }
}
