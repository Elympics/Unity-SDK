using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elympics
{
	[CreateAssetMenu(fileName = "ElympicsConfig", menuName = "Elympics/Config")]
	public class ElympicsConfig : ScriptableObject
	{
		public const string ELYMPICS_RESOURCES_PATH = "Assets/Resources/Elympics";
		public const string PATH_IN_RESOURCES = "Elympics/ElympicsConfig";

		#region ElympicsPrefs Keys

		public const string UsernameKey     = "Username";
		public const string PasswordKey     = "Password";
		public const string RefreshTokenKey = "RefreshToken";
		public const string AuthTokenKey    = "AuthToken";
		public const string IsLoginKey      = "IsLogin";

		#endregion

		[SerializeField] private string elympicsWebEndpoint = "https://api.elympics.cc";
		[SerializeField] private string elympicsEndpoint    = "http://127.0.0.1:9101";

		[SerializeField] internal int                      currentGame = -1;
		[SerializeField] internal List<ElympicsGameConfig> availableGames;

		internal string ElympicsWebEndpoint => elympicsWebEndpoint;
		internal string ElympicsEndpoint    => elympicsEndpoint;

		public event Action CurrentGameSwitched;

		public static ElympicsConfig Load() => Resources.Load<ElympicsConfig>(PATH_IN_RESOURCES);

		public static ElympicsGameConfig LoadCurrentElympicsGameConfig()
		{
			var elympicsConfig = Resources.Load<ElympicsConfig>(PATH_IN_RESOURCES);
			return elympicsConfig.GetCurrentGameConfig();
		}

		public ElympicsGameConfig GetCurrentGameConfig()
		{
			ValidateGameIndex(currentGame);
			return availableGames[currentGame];
		}

		public void SwitchGame(int game)
		{
			ValidateGameIndex(game);

			currentGame = game;
			CurrentGameSwitched?.Invoke();
		}

		private void ValidateGameIndex(int game)
		{
			if (game == -1)
				throw new NullReferenceException("Choose game config in ElympicsConfig!");
			if (game < 0 || game >= availableGames.Count)
				throw new NullReferenceException("Game config out of range in ElympicsConfig!");
		}

		public void UpdateElympicsEndpoint(string elympicsEndpoint)
		{
			if (string.IsNullOrEmpty(elympicsEndpoint))
				throw new NullReferenceException("Invalid elympics endpoint.");
			if (!Uri.IsWellFormedUriString(elympicsEndpoint, UriKind.Absolute))
				throw new NullReferenceException("Invalid elympics endpoint format. Please use an absolute uri");

			this.elympicsEndpoint = elympicsEndpoint;
		}
	}
}