using System;
using System.Collections;
using Elympics;
using NUnit.Framework;
using Cysharp.Threading.Tasks;
using UnityEngine.TestTools;

namespace Tests
{
	public class TestCloudPing
	{
		[UnityTest]
		public IEnumerator TestZeroRegions() => UniTask.ToCoroutine(async () =>
		{
			Exception exc = null;
			try
			{
				await ElympicsCloudPing.ChooseClosestRegion(Array.Empty<string>());
			}
			catch (Exception e)
			{
				exc = e;
			}
			Assert.NotNull(exc);
			Assert.AreEqual(typeof(ArgumentException), exc.GetType());
		});

		[UnityTest]
		public IEnumerator TestNullRegions() => UniTask.ToCoroutine(async () =>
		{
			Exception exc = null;
			try
			{
				await ElympicsCloudPing.ChooseClosestRegion(null);
			}
			catch (Exception e)
			{
				exc = e;
			}
			Assert.NotNull(exc);
			Assert.AreEqual(typeof(ArgumentNullException), exc.GetType());
		});
	}
}
