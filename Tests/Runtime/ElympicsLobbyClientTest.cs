using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Elympics.Models.Authentication;
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
        private const int TestsTimeoutMs = 1000;
        private const double PingTimeoutTestSec = 1.1;
        private const double DefaultPingTimeoutSec = 30;

        private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private const string Nickname = "nickname";

        private const string FakeJwt = @"{
  ""header"": {
    ""alg"": ""RS256"",
    ""typ"": ""JWT""
  },
  ""payload"": {
    ""nameid"": ""057f2883-b4b4-4cc6-895f-e1332da86567"",
    ""auth-type"": ""client-secret"",
    ""nbf"": 1718803982,
    ""exp"": 111,
    ""iat"": 1718803982
  },
  ""signature"": ""rX85CHYGCpo2V1J6hXRj0rRySi-n7qxjiuwS98P9zS6W-hfKHKsApWJQeLUZ4_0DCUr8AE-YdkbYESKwv6Jl5OuyHDH4QCIVuTkCVrbT4duCiopitcVqwNubQARpTc7lApDAxihAtmdVUuUwz26po2ntlgv-p_JdHqN1g5Uk3vr9miKDdBzvSwSWwN1NP2cGEvzqlAs3wHtw4GYZChX_RugjM-vppuovQMOkwxJ7IvQXV7kb00ucpj71u9EmTmQFN9RMnB8b4c5K7-kXCM-_L2PNAC6MZX2-OExNWklQtqTUD3oF-dJFRH4Hew_ZEgt_SBw37NWN1NSfT2q1wnXh0TDpFPPnZSqYUGNYl7mhOlLrPWNi5e4dpiawy-23760qDmj4kriyqOPcVCzWTbmcvcEe-ktwBIo9MNwYZvQCFJ7yZfsdVTlw7WdBO9_Kf6JZNVZ7Rc6jjCN3OPmCJShTLg7GbiHOp9Bl8637mXXV7GwTzqZxoyAvU9ysRyRXC3kMkUEew0oyAr8eCXU1k-8DIiK_AYdzAUIqSfgV74MwONqQtmrxbGx8kw_l4D15ha7vOMI0QoN9Tu62ElFBgwk2j-1ysH7_7D_sx-9wYD-gUUaOIgL2e71cLzxzzQ0RJYh984BE6RawW4-mzjiR3J8g9NYPRhT-911w-F_HGRTXCZ4""
}";

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
            _ = _sut.MockIAuthClient(FakeJwt, UserId, Nickname, AuthType.ClientSecret).MockIWebSocket(UserId, Nickname, false, null);
        }


        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympicsClientSecret_NoRoomsJoined() => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
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
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.AreSame(ElympicsRegions.Mumbai, _sut.CurrentRegion);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympicsClientSecret_OneRoomIsJoined() => UniTask.ToCoroutine(async () =>
        {
            _ = _sut!.MockIWebSocket(UserId, Nickname, true, null);
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.AreEqual(1, _sut.RoomsManager.ListJoinedRooms().Count);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator SwitchRegion() => UniTask.ToCoroutine(async () =>
        {
            var authenticationCalled = false;
            var connectedCalled = false;

            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret,
                Region = new RegionData(ElympicsRegions.Warsaw)
            });
            _sut.AuthenticationSucceeded += (_) => authenticationCalled = true;
            _sut.WebSocketSession.Connected += () => connectedCalled = true;
            await _sut.ConnectToElympicsAsync(new ConnectionData()
            {
                Region = new RegionData(ElympicsRegions.Mumbai)
            });
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.IsFalse(authenticationCalled);
            Assert.IsTrue(connectedCalled);
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
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
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
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.AreEqual(cache!.UserId, _sut.AuthData!.UserId);
            Assert.AreEqual(cache.Nickname, _sut.AuthData!.Nickname);
            Assert.AreSame(cache.JwtToken, _sut.AuthData!.JwtToken);
            Assert.AreEqual(cache.AuthType, _sut.AuthData!.AuthType);
        });

        [UnityTest]
        public IEnumerator GetPingToPreventDisconnection() => UniTask.ToCoroutine(async () =>
        {
            _ = _sut!.MockIWebSocket(UserId, Nickname, false, PingTimeoutTestSec - 0.1d).SetPingThresholdTimeout(TimeSpan.FromSeconds(PingTimeoutTestSec));
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });

            var disconnectedCalled = false;
            _sut.WebSocketSession.Disconnected += () => disconnectedCalled = true;
            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutTestSec * 2));
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.IsFalse(disconnectedCalled);
        });

        [UnityTest]
        public IEnumerator DisconnectWebSocket() => UniTask.ToCoroutine(async () =>
        {
            _ = _sut!.SetPingThresholdTimeout(TimeSpan.FromSeconds(PingTimeoutTestSec));
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });

            var disconnectedCalled = false;
            _sut.WebSocketSession.Disconnected += () => disconnectedCalled = true;

            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutTestSec + 2));
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsFalse(_sut.WebSocketSession.IsConnected);
            Assert.IsTrue(disconnectedCalled);
        });
        [UnityTest]
        public IEnumerator DisconnectWebSocket_Reconnect() => UniTask.ToCoroutine(async () =>
        {
            var authenticationCalled = false;
            var connectedCalled = false;
            var disconnectedCalled = false;

            _ = _sut!.SetPingThresholdTimeout(TimeSpan.FromSeconds(PingTimeoutTestSec));
            _sut!.WebSocketSession.Connected += () => connectedCalled = true;
            _sut.AuthenticationSucceeded += (_) => authenticationCalled = true;
            _sut.WebSocketSession.Disconnected += () => disconnectedCalled = true;
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret,
                Region = new RegionData()
                {
                    Name = ElympicsRegions.Warsaw
                }
            });
            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutTestSec + 2));
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsFalse(_sut.WebSocketSession.IsConnected);
            Assert.IsTrue(authenticationCalled);
            Assert.IsTrue(connectedCalled);
            Assert.IsTrue(disconnectedCalled);

            authenticationCalled = false;
            connectedCalled = false;
            disconnectedCalled = false;
            await _sut.ConnectToElympicsAsync(new ConnectionData()
            {
                Region = new RegionData()
                {
                    Name = ElympicsRegions.Mumbai
                }
            });
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
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
            Assert.IsFalse(_sut.IsAuthenticated);
            Assert.IsFalse(_sut.WebSocketSession.IsConnected);
        });
        [TearDown]
        public void CleanUp()
        {
            ElympicsLogger.Log($"{nameof(ElympicsLobbyClientTest)} Cleanup");
            if (_sut!.IsAuthenticated)
                _sut.SignOut();
            _ = _sut.MockIWebSocket(UserId, Nickname, false, null).SetPingThresholdTimeout(TimeSpan.FromSeconds(DefaultPingTimeoutSec));
            WebSocketMockSetup.CancelPingToken();
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
