using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics
{
	public class NetworkIdEnumerator
	{
		private static NetworkIdEnumerator instance;

		public static NetworkIdEnumerator Instance => instance ?? (instance = CreateNetworkIdEnumerator(0, ElympicsBehavioursManager.NetworkIdRange - 1));

		private readonly int          _min;
		private readonly int          _max;
		private          int          _current;
		private          bool         _checkDynamicAllocations;
		private          HashSet<int> _usedForcedIdsCached;
		private          HashSet<int> _dynamicAllocatedIds;

		public static NetworkIdEnumerator CreateNetworkIdEnumerator(int min, int max)
		{
			return new NetworkIdEnumerator(min, max);
		}

		private NetworkIdEnumerator(int min, int max)
		{
			Reset();
			_min = min;
			_max = max;
			_current = _min - 1;
			if (_min < ElympicsBehavioursManager.NetworkIdRange) // in case when developer wants to hardcode networkIds during production. The available range is from 0 to ElympicsBehavioursManager.NetworkIdRange
			{
				SetCurrentWithMaximumNotForcedNetworkIdPresentOnScene();
			}
			else
			{
				MoveNextAndGetCurrent();
			}
		}


		private void SetCurrentWithMaximumNotForcedNetworkIdPresentOnScene()
		{
			var activeScene = SceneManager.GetActiveScene();
			var behaviours = SceneObjectsFinder.FindObjectsOfType<ElympicsBehaviour>(activeScene, true);
			foreach (var behaviour in behaviours)
			{
				if (behaviour.forceNetworkId)
					continue;

				if (behaviour.NetworkId > _current)
					_current = behaviour.NetworkId;
			}

			_current = Mathf.Clamp(_current, _min, _current);
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
				if (behaviour.NetworkId != ElympicsBehaviour.UndefinedNetworkId && behaviour.forceNetworkId)
				{
					if (!_usedForcedIdsCached.Add(behaviour.NetworkId))
						Debug.LogError($"Repetition for FORCED network id {behaviour.NetworkId} in {behaviour.gameObject.name} {behaviour.GetType().Name}");
				}
			}

			return _usedForcedIdsCached;
		}

		public int GetCurrent() => _current;

		private int GetNext()
		{
			var usedForcedIds = GetForceIds();
			var next = _current;
			bool isOverflow = false;
			do
			{
				next++;
				if (next > _max && !isOverflow)
				{
					isOverflow = true;
					_checkDynamicAllocations = true;
					next = _min;
					continue;
				}

				if (next > _max && isOverflow)
				{
					throw new OverflowException($"Cannot generate NetworkId. The pool of min: {_min} max: {_max} ID's has been used out.");
				}

				if (next == int.MaxValue)
					Debug.LogError($"[Elympics] NetworkIds overflow! Try running {ElympicsEditorMenuPaths.RESET_IDS_MENU_PATH}.");
			} while (usedForcedIds.Contains(next) || (_checkDynamicAllocations && _dynamicAllocatedIds.Contains(next)));

			if (!_dynamicAllocatedIds.Add(next))
			{
				throw new Exception("Dynamically allocated NetworkId's already contains given Id.");
			}

			return next;
		}

		public void ReleaseId(int networkId)
		{
			_dynamicAllocatedIds.Remove(networkId);
		}

		public int MoveNextAndGetCurrent()
		{
			_current = GetNext();
			return _current;
		}

		public void MoveTo(int newCurrent) => _current = newCurrent;

		public void Reset()
		{
			_dynamicAllocatedIds = new HashSet<int>();
			_current = 0;
			_usedForcedIdsCached = null;
		}
	}
}