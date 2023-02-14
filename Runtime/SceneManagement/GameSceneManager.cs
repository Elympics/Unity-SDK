using System;
using UnityEngine;

namespace Elympics
{
	public class GameSceneManager : MonoBehaviour
	{
		[SerializeField] private ElympicsClient elympicsClient = null;
		[SerializeField] private ElympicsBot    elympicsBot    = null;
		[SerializeField] private ElympicsServer elympicsServer = null;

		private GameSceneInitializer              _gameSceneInitializer;

		public void Awake()
		{
			try
			{
				var elympicsGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
				_gameSceneInitializer = GameSceneInitializerFactory.Create(elympicsGameConfig);
				_gameSceneInitializer.Initialize(elympicsClient, elympicsBot, elympicsServer, elympicsGameConfig);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		private void OnDisable()
		{
			_gameSceneInitializer?.Dispose();
		}
	}
}