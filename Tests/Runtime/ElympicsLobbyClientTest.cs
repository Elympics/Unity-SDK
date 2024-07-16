using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Elympics.Lobby;
using Elympics.Models.Authentication;
using HybridWebSocket;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

#nullable enable

namespace Elympics.Tests
{
    [Category("ElympicsLobbyClient")]
    public class ElympicsLobbyClientTest : IPrebuildSetup
    {
        private ElympicsLobbyClient? _sut;
        private const string TestNameScene = "ElympicsLobbyClientTestScene";
        private const string AuthClientFieldName = "_auth";
        private const string WebSocketSessionName = "_webSocketSession";
        private const string WebSocketFactory = "_wsFactory";
        private const string PingTimeoutThreshold = "_automaticDisconnectThreshold";
        private const int TestsTimeoutMs = 1000;
        private const double PingTimeoutSec = 1.1;


        private const string ExpiredClientAuthJwt =
            "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIwNTdmMjg4My1iNGI0LTRjYzYtODk1Zi1lMTMzMmRhODY1NjciLCJhdXRoLXR5cGUiOiJjbGllbnQtc2VjcmV0IiwibmJmIjoxNzE4MzczOTc4LCJleHAiOjE3MTg0NjAzNzgsImlhdCI6MTcxODM3Mzk3OH0.O0h2FLCSA69a-D_GLeL6zo_Bqf8D6bW1n8o1Ue8TM1D8bDPv8KPblwBG13JyM76RJf30l7I77RjnYwmYvIxdMn1y8p14QtPkf_-nmxCEyRztE-7el44ud_z7gvzREJ0V0P89_BxPlJfIWG4kXdQGTczERRg4SkQWZyyMtTNNcXtK_KdREmDQm8_QXC9u15xcwVnjUxWfyCevcD-7djl2Sx_S1GFCKJDOsseBtWp8nTAtcCFFEioZDQh0cSf6G773eqFK_sy_jzCNPCGlJ7SCc6qs3MR2Fgg31P3jfQ7vtz1qrVC2mz86WPNQqwXvL9PubfxEL06g5xh9qcGUJuvXAehnaAG6iB098RvvBbHbM55p9cTaXtjk9DalZfMnwAEyEX9dfa6nLQhTMuWjQ8pScGcyG_RybbS932TaTdz_YiVFhnDmGKTugZLWVwLvJPVeri-o8E-BRY4bldKYTX5_ro26jY9tfPgYBi6H8K_alG5hx_A2Hf3Evyd3oWphMl61muReBqmLduL1jUr1V22C4rDPXToQgqhVp_y3p9iGI10tRRmywChFANYeRU2vtBKRQxazvUMCwgjCR8rpHz6JICcP6dlsmgW0WZmc4H0UkC_gAavQVHBpPlq0Ggd8Xf-Ihlx1MymLSCGoid0Ou09vWCAGbiQalnup-TDXjnJINDw";

        private const string CachedNickname = "CachedNickName";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene(TestNameScene);
            yield return new WaitUntil(() => ElympicsLobbyClient.Instance != null);
            _sut = ElympicsLobbyClient.Instance;
            Assert.NotNull(_sut);

            var mockAuthClient = WebSocketMockSetup.MockAuthClient();
#pragma warning disable IDE0062
            IWebSocket MockWebSocket(string s, string? s1) => WebSocketMockSetup.WebSocket;
#pragma warning restore IDE0062
            var authField = _sut!.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == AuthClientFieldName);
            Assert.NotNull(authField);

            authField.SetValue(_sut, mockAuthClient);
            var webSocketSessionField = _sut.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == WebSocketSessionName);
            Assert.NotNull(webSocketSessionField);

            var lazyWebSocketObject = (Lazy<WebSocketSession>)webSocketSessionField.GetValue(_sut);
            var webSocketFactory = lazyWebSocketObject.Value.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(x => x.Name == WebSocketFactory);
            Assert.NotNull(webSocketFactory);

            webSocketFactory.SetValue(lazyWebSocketObject.Value, (WebSocketSession.WebSocketFactory)MockWebSocket);

            var webSocketSession = lazyWebSocketObject.Value;
            var pingDisconnectTimeout = webSocketSession.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(x => x.Name == PingTimeoutThreshold);

            pingDisconnectTimeout!.SetValue(webSocketSession, TimeSpan.FromSeconds(PingTimeoutSec));
        }

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympicsClientSecret_NoRoomsJoined() => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });
            Assert.IsTrue(_sut is { IsAuthenticated: true, WebSocketSession: { IsConnected: true } });
            Assert.AreEqual(0, _sut.RoomsManager.ListJoinedRooms().Count);
            Assert.AreEqual(AuthType.ClientSecret, _sut.AuthData!.AuthType);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympicsClientSecret_SelectRegion() => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret,
                Region = new RegionData(ElympicsRegions.Mumbai)
            });
            Assert.IsTrue(_sut is { IsAuthenticated: true, WebSocketSession: { IsConnected: true } });
            Assert.AreSame(ElympicsRegions.Mumbai, _sut.CurrentRegion);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympicsClientSecret_OneRoomIsJoined() => UniTask.ToCoroutine(async () =>
        {
            WebSocketMockSetup.IsUserInRoom(true);
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });
            Assert.IsTrue(_sut is { IsAuthenticated: true, WebSocketSession: { IsConnected: true } });
            Assert.AreEqual(1, _sut.RoomsManager.ListJoinedRooms().Count);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator SwitchRegion() => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });
            await _sut.ConnectToElympicsAsync(new ConnectionData()
            {
                Region = new RegionData(ElympicsRegions.Mumbai)
            });
            Assert.IsTrue(_sut is { IsAuthenticated: true, WebSocketSession: { IsConnected: true } });
            Assert.AreSame(ElympicsRegions.Mumbai, _sut.CurrentRegion);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympics_UseCachedData_AutoRetry_Succeed() => UniTask.ToCoroutine(async () =>
        {

            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret,
            });
            var cachedData = _sut.AuthData;
            Assert.NotNull(cachedData);
            _sut.SignOut();
            await _sut.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthFromCacheData = new CachedAuthData()
                {
                    CachedData = new AuthData(cachedData!.UserId, ExpiredClientAuthJwt, cachedData.Nickname, cachedData.AuthType),
                    AutoRetryIfExpired = true,
                }
            });
            Assert.IsTrue(_sut.IsAuthenticated && _sut.WebSocketSession.IsConnected);
            Assert.AreNotSame(ExpiredClientAuthJwt, _sut.AuthData!.JwtToken);
            Assert.AreSame(cachedData.Nickname, _sut.AuthData.Nickname);
            Assert.AreEqual(cachedData.UserId, _sut.AuthData.UserId);
            Assert.AreEqual(AuthType.ClientSecret, _sut.AuthData!.AuthType);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympics_UseCachedData_Expired() => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthFromCacheData = new CachedAuthData()
                {
                    CachedData = new AuthData(Guid.Empty, ExpiredClientAuthJwt, CachedNickname, AuthType.ClientSecret),
                    AutoRetryIfExpired = false,
                }
            });
            LogAssert.Expect(LogType.Error, new Regex(@"Jwt\s+token\s+has\s+expired\."));
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympics_UseCachedData_Succeed() => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });
            var cache = _sut.AuthData;
            Assert.NotNull(cache);

            _sut.SignOut();

            await _sut.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthFromCacheData = new CachedAuthData()
                {
                    CachedData = cache,
                    AutoRetryIfExpired = false,
                }
            });
            Assert.IsTrue(_sut.IsAuthenticated && _sut.WebSocketSession.IsConnected);
            Assert.AreEqual(cache!.UserId, _sut.AuthData!.UserId);
            Assert.AreEqual(cache.Nickname, _sut.AuthData!.Nickname);
            Assert.AreSame(cache.JwtToken, _sut.AuthData!.JwtToken);
            Assert.AreEqual(cache.AuthType, _sut.AuthData!.AuthType);
        });

        [UnityTest]
        public IEnumerator GetPingToPreventDisconnection() => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });

            var disconnectedCalled = false;
            _sut.WebSocketSession.Disconnected += () => disconnectedCalled = true;
            WebSocketMockSetup.SendPing(); //TODO: Mock this in WebSocketMockSetup.
            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutSec - 0.1d));
            Assert.IsTrue(_sut is { IsAuthenticated: true, WebSocketSession: { IsConnected: true } });
            Assert.IsFalse(disconnectedCalled);
            WebSocketMockSetup.SendPing();
            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutSec - 0.1d));
            Assert.IsTrue(_sut is { IsAuthenticated: true, WebSocketSession: { IsConnected: true } });
            Assert.IsFalse(disconnectedCalled);
            WebSocketMockSetup.SendPing();
            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutSec - 0.1d));
            Assert.IsTrue(_sut is { IsAuthenticated: true, WebSocketSession: { IsConnected: true } });
            Assert.IsFalse(disconnectedCalled);
        });

        [UnityTest]
        public IEnumerator DisconnectWebSocket() => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });

            var disconnectedCalled = false;
            _sut.WebSocketSession.Disconnected += () => disconnectedCalled = true;

            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutSec + 2));
            Assert.IsTrue(_sut is { IsAuthenticated: true, WebSocketSession: { IsConnected: false } });
            Assert.IsTrue(disconnectedCalled);
        });
        [UnityTest]
        public IEnumerator DisconnectWebSocket_Reconnect() => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret,
                Region = new RegionData()
                {
                    Name = ElympicsRegions.Warsaw
                }
            });
            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutSec + 2));
            Assert.IsTrue(_sut is { IsAuthenticated: true, WebSocketSession: { IsConnected: false } });

            var authenticationCalled = false;
            var connectedCalled = false;

            _sut.WebSocketSession.Connected += () => connectedCalled = true;
            _sut.AuthenticationSucceeded += (_) => authenticationCalled = true;

            await _sut.ConnectToElympicsAsync(new ConnectionData()
            {
                Region = new RegionData()
                {
                    Name = ElympicsRegions.Mumbai
                }
            });
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            Assert.IsTrue(_sut is { IsAuthenticated: true, WebSocketSession: { IsConnected: true } });
            Assert.IsFalse(authenticationCalled);
            Assert.IsTrue(connectedCalled);
        });
        [UnityTest]
        public IEnumerator ConnectToElympicsRegionOnly() => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                Region = new RegionData()
                {
                    Name = ElympicsRegions.Warsaw
                }
            });
            Assert.IsTrue(_sut is { IsAuthenticated: false, WebSocketSession: { IsConnected: false } });
        });
        [TearDown]
        public void CleanUp()
        {
            ElympicsLogger.Log($"{nameof(ElympicsLobbyClientTest)} Cleanup");
            if (_sut!.IsAuthenticated)
                _sut.SignOut();
            WebSocketMockSetup.IsUserInRoom(false);
        }

        public void Setup()
        {
#if UNITY_EDITOR
            ElympicsLogger.Log("Setup configs");
            var config = ElympicsConfig.Load();
            if (config == null)
            {
                if (!Directory.Exists(ElympicsConfig.ElympicsResourcesPath))
                {
                    ElympicsLogger.Log("Creating Elympics resources directory...");
                    _ = Directory.CreateDirectory(ElympicsConfig.ElympicsResourcesPath);
                }

                var newConfig = ScriptableObject.CreateInstance<ElympicsConfig>();

                const string resourcesDirectory = "Assets/Resources/";
                var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(resourcesDirectory + ElympicsConfig.PathInResources + ".asset");
                AssetDatabase.CreateAsset(newConfig, assetPathAndName);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                config = newConfig;
            }
            var currentConfigs = Resources.LoadAll<ElympicsGameConfig>("Elympics");
            if (currentConfigs is null
                || currentConfigs.Length == 0)
            {
                var gameConfig = ScriptableObject.CreateInstance<ElympicsGameConfig>();
                if (!Directory.Exists(ElympicsConfig.ElympicsResourcesPath))
                {
                    ElympicsLogger.Log("Creating Elympics Resources directory...");
                    _ = Directory.CreateDirectory(ElympicsConfig.ElympicsResourcesPath);
                    ElympicsLogger.Log("Elympics Resources directory created successfully.");
                }

                AssetDatabase.CreateAsset(gameConfig, ElympicsConfig.ElympicsResourcesPath + "/ElympicsGameConfig.asset");
                config.availableGames = new()
                {
                    gameConfig
                };
                config.currentGame = 0;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            if (config.availableGames == null
                || config.availableGames.Count == 0)
            {
                var games = config.availableGames ?? new List<ElympicsGameConfig>();
                games.Add(currentConfigs![0]);
                config.availableGames = games;
                config.currentGame = 0;
            }
            ElympicsLogger.Log($"Current test elympicsConfig has {config.availableGames.Count} games and current game index is {config.currentGame}");
            var currentScenes = EditorBuildSettings.scenes.ToList();
            if (currentScenes.Any(x => x.path.Contains(TestNameScene)))
                return;

            var guids = AssetDatabase.FindAssets(TestNameScene + " t:Scene");
            if (guids.Length != 1)
                throw new ArgumentException($"There cannot be more than 1 {TestNameScene} scene asset.");

            var scene = AssetDatabase.GUIDToAssetPath(guids[0]);
            var editorBuildSettingScene = new EditorBuildSettingsScene(scene, true);
            currentScenes.Add(editorBuildSettingScene);
            EditorBuildSettings.scenes = currentScenes.ToArray();
#endif
        }
    }
}
