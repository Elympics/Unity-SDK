using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Communication.Lobby.InternalModels;
using JetBrains.Annotations;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#nullable enable

namespace Elympics
{
    [CreateAssetMenu(fileName = "ElympicsConfig", menuName = "Elympics/Config")]
    public class ElympicsConfig : ScriptableObject
    {
        public const string ElympicsResourcesPath = "Assets/Resources/Elympics";
        public const string PathInResources = "Elympics/ElympicsConfig";

        [SerializeField] private string elympicsWebEndpoint = "https://api.elympics.cc";
        [SerializeField] private string elympicsGameServersEndpoint = "https://gs.elympics.cc";

        [SerializeField] private int currentGame = -1;
        [SerializeField] internal List<ElympicsGameConfig> availableGames = new();

        [SerializeField] private bool migratedActiveGame;

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

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (currentGame > 0 && currentGame < availableGames.Count)
            {
                var activeGame = availableGames[currentGame];
                availableGames.RemoveAt(currentGame);
                availableGames.Insert(0, activeGame);
            }
            migratedActiveGame = true;
        }
#endif

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

        internal string ElympicsRespectEndpoint => ApplicationParameters.Parameters.RespectEndpoint.GetValue(GetV2Endpoint("respect"))
            .GetAbsoluteOrRelativeString();

        internal Uri ElympicsReplaySource => ApplicationParameters.Parameters.ReplaySource.GetValue(GetV2Endpoint("replay"));

        internal string ElympicsWebSocketUrl => ElympicsLobbyEndpoint.AppendPathSegments(Routes.Base).ToString();

        internal string ElympicsAvailableRegionsUrl => ElympicsApiEndpoint.AppendPathSegments(ElympicsApiModels.ApiModels.Regions.Routes.AllRegionsRouteUnityFormat).ToString();
        internal string GameAvailableRegionsUrl(string gameId) =>
            ElympicsApiEndpoint.AppendPathSegments(string.Format(ElympicsApiModels.ApiModels.Regions.Routes.RegionForGameIdRoute, gameId)).ToString();

        [PublicAPI]
        public Uri GetV2Endpoint(string serviceName) =>
            elympicsWebEndpoint.AppendPathSegments("v2", serviceName);

        public IReadOnlyList<ElympicsGameConfig> AvailableGames => availableGames;

        public event Action? CurrentGameSwitched;

        public static ElympicsConfig? Load() => Resources.Load<ElympicsConfig>(PathInResources);

        public static ElympicsGameConfig? LoadCurrentElympicsGameConfig()
        {
            var elympicsConfig = Resources.Load<ElympicsConfig>(PathInResources);
            if (elympicsConfig)
                return elympicsConfig.GetCurrentGameConfig();
            throw new ElympicsException($"Couldn't load ElympicsConfig from {PathInResources}");
        }

        public ElympicsGameConfig? GetCurrentGameConfig() => availableGames.FirstOrDefault(gameConfig => gameConfig != null);

        public void SwitchGame(int game)
        {
            ValidateGameIndex(game);

            var activeGame = availableGames[game];
            availableGames.RemoveAt(game);
            availableGames.Insert(0, activeGame);
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
