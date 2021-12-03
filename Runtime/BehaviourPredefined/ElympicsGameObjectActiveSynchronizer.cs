using UnityEngine;

namespace Elympics
{
	[DisallowMultipleComponent]
	public class ElympicsGameObjectActiveSynchronizer : MonoBehaviour, IStateSerializationHandler
	{
		private readonly ElympicsBool _gameObjectActive = new ElympicsBool();

		public void OnPostStateDeserialize()
		{
			gameObject.SetActive(_gameObjectActive);
		}

		public void OnPreStateSerialize()
		{
			_gameObjectActive.Value = gameObject.activeSelf;
		}
	}
}
