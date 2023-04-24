using System;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_WEBGL
using System.Collections;
using Cysharp.Threading.Tasks;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#endif

namespace Elympics
{
	internal static class TaskUtil
	{
#if UNITY_WEBGL
		private static AsyncEventsDispatcher coroutineRunner;

		private static IEnumerator Delay(TimeSpan delay, TaskCompletionSource<bool> tcs, CancellationToken ct = default)
		{
			Exception exc = null;
			yield return UniTask.Delay(delay, true, cancellationToken: ct).ToCoroutine(exception => exc = exception);
			if (exc == null)
				tcs.SetResult(true);
			else if (exc is OperationCanceledException)
				tcs.SetCanceled();
			else
				tcs.SetException(exc);
		}
#endif

		internal static async Task Delay(TimeSpan delay, CancellationToken cancellationToken = default)
		{
#if UNITY_WEBGL
			if (coroutineRunner == null)
				coroutineRunner = Object.FindObjectOfType<AsyncEventsDispatcher>();
			var tcs = new TaskCompletionSource<bool>();
			coroutineRunner.Enqueue(Delay(delay, tcs, cancellationToken));
			await tcs.Task;
#else
			await Task.Delay(delay, cancellationToken);
#endif
		}

		internal static Task Delay(int millisecondsDelay, CancellationToken cancellationToken = default) =>
		    Delay(TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken);
	}
}
