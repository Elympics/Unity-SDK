using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class CoroutineTaskCreator
{
	private const float TIMEOUT_LIMIT = 10;
	private const string TIMEOUT_ERROR_MESSAGE = "Connection timed out";

	public static IEnumerator RunTaskCoroutine<ResponseType>(
		Task<ResponseType> task,
		Action<ResponseType> successCallback = null,
		Action<string> errorCallback = null,
		CancellationTokenSource cts = null)
	{
		float time = 0;
		while (!task.IsCompleted && (cts == null || !cts.IsCancellationRequested) && time < TIMEOUT_LIMIT)
		{
			time += Time.deltaTime;
			yield return null;
		}
		if (time >= TIMEOUT_LIMIT)
		{
			cts?.Cancel();
			errorCallback?.Invoke(TIMEOUT_ERROR_MESSAGE);
		}
		//else if (task.IsCompleted && !task.Result.IsSuccess)
			//errorCallback?.Invoke(task.Result.ErrorMessage);
		else if (task.IsCompleted)
			successCallback?.Invoke(task.Result);
	}
}
