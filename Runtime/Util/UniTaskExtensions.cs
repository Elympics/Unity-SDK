using System;
using Cysharp.Threading.Tasks;

#nullable enable

namespace Elympics
{
    internal static class UniTaskExtensions
    {
        public static void TrySetCanceledAndIgnoreResult(this ICancelPromise tcs) => tcs.TrySetCanceled();

        public static async UniTask<Exception?> Catch(this UniTask task)
        {
            Exception? exception = null;
            try
            {
                await task;
            }
            catch (Exception e)
            {
                exception = e;
            }
            return exception;
        }
    }
}
