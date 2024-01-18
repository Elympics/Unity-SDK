using System;
using Cysharp.Threading.Tasks;

#nullable enable

namespace Elympics.Tests.Common
{
    public class AsyncEventsDispatcherMock : IAsyncEventsDispatcher
    {
        public void Enqueue(Action action) => ExecuteOnMainThread(action).Forget();

        private static async UniTaskVoid ExecuteOnMainThread(Action action)
        {
            await UniTask.SwitchToMainThread();
            action.Invoke();
        }
    }
}
