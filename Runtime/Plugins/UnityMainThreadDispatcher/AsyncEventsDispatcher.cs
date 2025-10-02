/*
Copyright 2015 Pim de Witte All Rights Reserved.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

/*
 * With modifications licensed to Elympics Sp. z o.o.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;

#nullable enable

namespace Elympics
{
	/// Author: Pim de Witte (pimdewitte.com) and contributors, https://github.com/PimDeWitte/AsyncEventsDispatcher
	/// <summary>
	/// A thread-safe class which holds a queue with actions to execute on the next Update() method. It can be used to make calls to the main thread for
	/// things such as UI Manipulation in Unity. It was developed for use in combination with the Firebase Unity plugin, which uses separate threads for event handling
	/// </summary>
	[DefaultExecutionOrder(ElympicsExecutionOrder.AsyncEventsDispatcher)]
	public class AsyncEventsDispatcher : MonoBehaviour, IAsyncEventsDispatcher
	{
		public static AsyncEventsDispatcher Instance { get; set; } = null!;

		private readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();

		private void Awake()
		{
			if (Instance == null)
				Instance = this;
		}

		private void Update()
		{
			while (_executionQueue.TryDequeue(out var action))
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"AsyncDispatcher exception: {e.Message}{Environment.NewLine}{e.StackTrace}");
                }
		}

		/// <summary>
		/// Locks the queue and adds the Action to the queue
		/// </summary>
		/// <param name="action">function that will be executed from the main thread.</param>
		public void Enqueue(Action action) => _executionQueue.Enqueue(action);
	}
}
