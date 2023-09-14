using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics
{
    public class NetworkIdEnumerator
    {
        private static NetworkIdEnumerator instance;

        public static NetworkIdEnumerator Instance => instance ??= CreateNetworkIdEnumerator(0, ElympicsBehavioursManager.NetworkIdRange - 1);

        private readonly int _min;
        private readonly int _max;
        private int _current;
        private bool _checkDynamicAllocations;
        private HashSet<int> _usedForcedIdsCached;
        private HashSet<int> _dynamicAllocatedIds;

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
                SetCurrentWithMaximumNotForcedNetworkIdPresentOnScene();
            else
                _ = MoveNextAndGetCurrent();
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
                if (behaviour.NetworkId != ElympicsBehaviour.UndefinedNetworkId && behaviour.forceNetworkId)
                    if (!_usedForcedIdsCached.Add(behaviour.NetworkId))
                        throw ElympicsLogger.LogException("Repetition for FORCED network ID "
                            + $"{behaviour.NetworkId} in {behaviour.gameObject.name} {behaviour.GetType().Name}");

            return _usedForcedIdsCached;
        }

        public int GetCurrent() => _current;

        private int GetNext()
        {
            var usedForcedIds = GetForceIds();
            var next = _current;
            var isOverflow = false;
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
                    throw ElympicsLogger.LogException(new OverflowException("Cannot generate a network ID. "
                        + $"The pool of IDs between min: {_min} and max: {_max} has been used up."));

                if (next == int.MaxValue)  // TODO: can this error occur? ~dsygocki 2023-08-24
                    throw ElympicsLogger.LogException(new OverflowException("Network ID overflow! "
                        + $"Try running {ElympicsEditorMenuPaths.RESET_IDS_MENU_PATH}."));
            } while (usedForcedIds.Contains(next) || (_checkDynamicAllocations && _dynamicAllocatedIds.Contains(next)));

            if (!_dynamicAllocatedIds.Add(next))
                throw ElympicsLogger.LogException($"Generated network ID {next} is already in use.");

            return next;
        }

        public void ReleaseId(int networkId)
        {
            _ = _dynamicAllocatedIds.Remove(networkId);
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
