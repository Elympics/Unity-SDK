using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Elympics
{
	[CreateAssetMenu(fileName = "ElympicsConfig", menuName = "Elympics/Config")]
	public class ElympicsConfig : ScriptableObject
	{
		public const string ELYMPICS_RESOURCES_PATH = "Assets/Resources/Elympics";
		public const string PATH_IN_RESOURCES       = "Elympics/ElympicsConfig";

		[SerializeField] private string elympicsApiEndpoint         = "https://api.elympics.cc";
		[SerializeField] private string elympicsLobbyEndpoint       = "https://lobby.elympics.cc";
		[SerializeField] private string elympicsGameServersEndpoint = "https://gs.elympics.cc";

		[SerializeField] internal int                      currentGame = -1;
		[SerializeField] internal List<ElympicsGameConfig> availableGames;

		internal string ElympicsApiEndpoint         => elympicsApiEndpoint;
		internal string ElympicsLobbyEndpoint       => elympicsLobbyEndpoint;
		internal string ElympicsGameServersEndpoint => elympicsGameServersEndpoint;

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

			this.elympicsLobbyEndpoint = elympicsEndpoint;
		}

#if UNITY_EDITOR

		#region EditorPrefs

		private const string UsernameKey     = "Username";
		private const string PasswordKey     = "Password";
		private const string RefreshTokenKey = "RefreshToken";
		private const string AuthTokenKey    = "AuthToken";
		private const string AuthTokenExpKey = "AuthTokenExp";
		private const string IsLoginKey      = "IsLogin";

		public static string Username
		{
			get => EditorPrefs.GetString(UsernameKey);
			set => EditorPrefs.SetString(UsernameKey, value);
		}

		public static string Password
		{
			get => EditorPrefs.GetString(PasswordKey);
			set => EditorPrefs.SetString(PasswordKey, value);
		}

		public static string RefreshToken
		{
			get => EditorPrefs.GetString(RefreshTokenKey);
			set => EditorPrefs.SetString(RefreshTokenKey, value);
		}

		public static string AuthToken
		{
			get => EditorPrefs.GetString(AuthTokenKey);
			set => EditorPrefs.SetString(AuthTokenKey, value);
		}

		public static string AuthTokenExp
		{
			get => EditorPrefs.GetString(AuthTokenExpKey);
			set => EditorPrefs.SetString(AuthTokenExpKey, value);
		}

		public static bool IsLogin
		{
			get => EditorPrefs.GetBool(IsLoginKey);
			set => EditorPrefs.SetBool(IsLoginKey, value);
		}

		#endregion

#endif
	}
}
