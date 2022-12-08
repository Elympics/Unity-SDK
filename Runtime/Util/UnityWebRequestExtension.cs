using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics
{
	public static class UnityWebRequestExtension
	{
		public static bool IsConnectionError(this UnityWebRequest request)
		{
#if UNITY_2020_2_OR_NEWER
			return request != null && request.result == UnityWebRequest.Result.ConnectionError;
#else
			return request != null && request.isNetworkError;
#endif
		}

		public static bool IsProtocolError(this UnityWebRequest request)
		{
#if UNITY_2020_2_OR_NEWER
			return request != null && request.result == UnityWebRequest.Result.ProtocolError;
#else
			return request != null && request.isHttpError;
#endif
		}
	}
}