using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

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
                Debug.Log(exception.ToString());
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
