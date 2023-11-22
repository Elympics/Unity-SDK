using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Elympics
{
    [CreateAssetMenu(fileName = "ElympicsConfig", menuName = "Elympics/Config")]
    public class ElympicsConfig : ScriptableObject
    {
        public const string ElympicsResourcesPath = "Assets/Resources/Elympics";
        public const string PathInResources = "Elympics/ElympicsConfig";

        [SerializeField] private string elympicsWebEndpoint = "https://api.elympics.cc";
        [SerializeField] private string elympicsGameServersEndpoint = "https://gs.elympics.cc";

        [SerializeField] internal int currentGame = -1;
        [SerializeField] internal List<ElympicsGameConfig> availableGames;

        private static string sdkVersion;
        internal static string SdkVersion
        {
            get
            {
                if (string.IsNullOrEmpty(sdkVersion))
                    UpdateSdkVersion();
                return sdkVersion;
            }
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        private static void UpdateSdkVersion() => sdkVersion = ElympicsVersionRetriever.GetVersionStringFromAssembly();

        internal string ElympicsApiEndpoint => ApplicationParameters.Parameters.ApiEndpoint.GetValue(GetV2Endpoint("api"))
            .GetAbsoluteOrRelativeString();
        internal string ElympicsLobbyEndpoint => ApplicationParameters.Parameters.LobbyEndpoint.GetValue(GetV2Endpoint("lobby"))
            .GetAbsoluteOrRelativeString();
        internal string ElympicsAuthEndpoint => ApplicationParameters.Parameters.AuthEndpoint.GetValue(GetV2Endpoint("auth"))
            .GetAbsoluteOrRelativeString();
        internal string ElympicsLeaderboardsEndpoint => ApplicationParameters.Parameters.LeaderboardsEndpoint.GetValue(GetV2Endpoint("leaderboardservice"))
            .GetAbsoluteOrRelativeString();
        internal string ElympicsGameServersEndpoint => ApplicationParameters.Parameters.GameServersEndpoint.GetValue(new UriBuilder(elympicsGameServersEndpoint).Uri)
            .GetAbsoluteOrRelativeString();

        [PublicAPI]
        public Uri GetV2Endpoint(string serviceName) =>
            elympicsWebEndpoint.AppendPathSegments("v2", serviceName);

        public IReadOnlyList<ElympicsGameConfig> AvailableGames => availableGames;

        public event Action CurrentGameSwitched;

        [CanBeNull] public static ElympicsConfig Load() => Resources.Load<ElympicsConfig>(PathInResources);

        public static ElympicsGameConfig LoadCurrentElympicsGameConfig()
        {
            var elympicsConfig = Resources.Load<ElympicsConfig>(PathInResources);
            return elympicsConfig?.GetCurrentGameConfig();
        }

        [CanBeNull]
        public ElympicsGameConfig GetCurrentGameConfig()
        {
            try
            {
                ValidateGameIndex(currentGame);
                return availableGames[currentGame];
            }
            catch
            {
                return null;
            }
        }

        public void SwitchGame(int game)
        {
            ValidateGameIndex(game);

            currentGame = game;
            CurrentGameSwitched?.Invoke();
        }

        private void ValidateGameIndex(int game)
        {
            if (availableGames.Count == 0)
                throw ElympicsLogger.LogException(new InvalidOperationException(
                    $"No game configs have been configured in {nameof(ElympicsConfig)}"));
            if (game < 0 || game >= availableGames.Count)
                throw ElympicsLogger.LogException(new ArgumentOutOfRangeException(nameof(game)));
        }

#if UNITY_EDITOR

        #region EditorPrefs

        private const string UsernameKey = "Username";
        private const string PasswordKey = "Password";
        private const string RefreshTokenKey = "RefreshToken";
        private const string AuthTokenKey = "AuthToken";
        private const string AuthTokenExpKey = "AuthTokenExp";
        private const string IsLoginKey = "IsLogin";


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
