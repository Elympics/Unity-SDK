using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics
{
	public class NetworkIdEnumerator
	{
		private static NetworkIdEnumerator instance;

		public static NetworkIdEnumerator Instance => instance ?? (instance = new NetworkIdEnumerator());

		private int _current;
		private HashSet<int> _usedForcedIdsCached;


		internal NetworkIdEnumerator()
		{
			Reset();
			SetCurrentWithMaximumNotForcedNetworkIdPresentOnScene();
		}

		internal NetworkIdEnumerator(int start)
		{
			Reset();
			_current = start - 1;
			MoveNextAndGetCurrent();
		}

		private void SetCurrentWithMaximumNotForcedNetworkIdPresentOnScene()
		{
			var activeScene = SceneManager.GetActiveScene();
			var behaviours = SceneObjectsFinder.FindObjectsOfType<ElympicsBehaviour>(activeScene, true);
			foreach (var behaviour in behaviours)
			{
				if (behaviour.ForceNetworkId)
					continue;

				if (behaviour.NetworkId > _current)
					_current = behaviour.NetworkId;
			}
		}

		private HashSet<int> GetForceIds()
		{
			if (_usedForcedIdsCached != null)
				return _usedForcedIdsCached;

			var activeScene = SceneManager.GetActiveScene();
			var behaviours = SceneObjectsFinder.FindObjectsOfType<ElympicsBehaviour>(activeScene, true);

			_usedForcedIdsCached = new HashSet<int>();
			foreach (var behaviour in behaviours)
			{
				if (behaviour.NetworkId != ElympicsBehaviour.UndefinedNetworkId && behaviour.ForceNetworkId)
				{
					if (!_usedForcedIdsCached.Add(behaviour.NetworkId))
						Debug.LogError($"Repetition for FORCED network id {behaviour.NetworkId} in {behaviour.gameObject.name} {behaviour.GetType().Name}");
				}
			}

			return _usedForcedIdsCached;
		}

		public int GetCurrent() => _current;

		public int GetNext()
		{
			var usedForcedIds = GetForceIds();
			var next = _current;
			do
			{
				next++;
				if (next == int.MaxValue)
					Debug.LogError($"[Elympics] NetworkIds overflow! Try running {ElympicsEditorMenuPaths.RESET_IDS_MENU_PATH}.");
			} while (usedForcedIds.Contains(next));

			return next;
		}

		public int MoveNextAndGetCurrent()
		{
			_current = GetNext();
			return _current;
		}

		public void MoveTo(int newCurrent) => _current = newCurrent;

		public void Reset()
		{
			_current = 0;
			_usedForcedIdsCached = null;
		}
	}
}
