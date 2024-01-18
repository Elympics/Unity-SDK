using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

#nullable enable

namespace Elympics.Tests.Common
{
    public static class AsyncAsserts
    {
        public static async UniTask<T> AssertThrowsAsync<T>(UniTask task)
            where T : Exception
        {
            Exception? caughtException = null;
            try
            {
                await task;
            }
            catch (Exception exception)
            {
                caughtException = exception;
            }
            Assert.IsInstanceOf<T>(caughtException);
            return (T)caughtException!;
        }

        public static UniTask<T> AssertThrowsAsync<T>(Func<UniTask> taskFactory)
            where T : Exception =>
            AssertThrowsAsync<T>(UniTask.Create(taskFactory));
    }
}
