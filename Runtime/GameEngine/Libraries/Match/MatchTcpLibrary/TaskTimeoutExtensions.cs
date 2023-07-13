using System;
using System.Threading;
using System.Threading.Tasks;
using Elympics;

namespace MatchTcpLibrary
{
    public static class TaskTimeoutExtensions
    {
        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout, CancellationTokenSource cancellationTokenSource = null)
        {
            if (cancellationTokenSource == null)
            {
                if (await Task.WhenAny(task, TaskUtil.Delay(timeout)) == task)
                    return await task;
            }
            else
            {
                if (await Task.WhenAny(task, TaskUtil.Delay(timeout, cancellationTokenSource.Token)) == task)
                    return await task;
            }

            throw new TimeoutException("Task timed out");
        }
        public static async Task WithTimeout(this Task task, TimeSpan timeout, CancellationTokenSource cancellationTokenSource = null)
        {
            if (cancellationTokenSource == null)
            {
                if (await Task.WhenAny(task, TaskUtil.Delay(timeout)) == task)
                {
                    await task;
                    return;
                }
            }
            else
            {
                if (await Task.WhenAny(task, TaskUtil.Delay(timeout, cancellationTokenSource.Token)) == task)
                {
                    await task;
                    return;
                }
            }

            throw new TimeoutException("Task timed out");
        }
    }
}
