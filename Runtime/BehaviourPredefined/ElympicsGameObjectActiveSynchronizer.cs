using UnityEngine;

namespace Elympics
{
    [DisallowMultipleComponent]
    public class ElympicsGameObjectActiveSynchronizer : MonoBehaviour, IStateSerializationHandler
    {
        private readonly ElympicsBool _gameObjectActive = new();

        public void OnPostStateDeserialize()
        {
            gameObject.SetActive(_gameObjectActive.Value);
        }

        public void OnPreStateSerialize()
        {
            _gameObjectActive.Value = gameObject.activeSelf;
        }
    }
}
