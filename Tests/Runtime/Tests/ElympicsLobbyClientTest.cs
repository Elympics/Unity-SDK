using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Elympics.Models.Authentication;
using Elympics.Tests.Common;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

#nullable enable

namespace Elympics.Tests
{
    [Category("ElympicsLobbyClient")]
    [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
    public class ElympicsLobbyClientTest : ElympicsMonoBaseTest
    {
        private ElympicsLobbyClient? _sut;
        public override string SceneName => "ElympicsLobbyClientTestScene";
        public override bool RequiresElympicsConfig => true;
        private const int TestsTimeoutMs = 200000;
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
            SceneManager.LoadScene(SceneName);
            yield return new WaitUntil(() => ElympicsLobbyClient.Instance != null);
            _sut = ElympicsLobbyClient.Instance;
            Assert.NotNull(_sut);
            _ = _sut!.MockSuccessIAuthClient(FakeJwt, UserId, Nickname).MockIWebSocket(UserId, Nickname, false, null, out _).MockIAvailableRegionRetriever(ElympicsRegions.Warsaw, ElympicsRegions.Mumbai, ElympicsRegions.Tokyo, ElympicsRegions.Dallas);
        }
        private static AuthType[] connectTestValue = { AuthType.ClientSecret, AuthType.EthAddress };

        [UnityTest]
        public IEnumerator ConnectToElympics([ValueSource(nameof(connectTestValue))] AuthType type) => UniTask.ToCoroutine(async () =>
        {

            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = type
            });
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);

        });
        [UnityTest]
        public IEnumerator ConnectToElympics_NoConnectionDataProvided_ThrowException() => UniTask.ToCoroutine(async () =>
        {
            _ = await AsyncAsserts.AssertThrowsAsync<ElympicsException>(async () => await _sut!.ConnectToElympicsAsync(new ConnectionData
            {
                AuthType = null,
                Region = null,
                AuthFromCacheData = null
            }));
            Assert.AreEqual((int)ElympicsState.Disconnected, (int)_sut.CurrentState.State);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympics_NoRoomsJoined([ValueSource(nameof(connectTestValue))] AuthType type) => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = type
            });
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.AreEqual(0, _sut.RoomsManager.ListJoinedRooms().Count);
            Assert.AreEqual(type, _sut.AuthData!.AuthType);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympics_And_Repeat_SameValues() => UniTask.ToCoroutine(async () =>
        {
            var authType = AuthType.ClientSecret;
            var region = new RegionData(ElympicsRegions.Warsaw);

            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = authType,
                Region = region
            });
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);

            var authenticationCalled = false;
            var connectedCalled = false;
            _sut.AuthenticationSucceeded += (_) => authenticationCalled = true;
            _sut.WebSocketSession.Connected += () => connectedCalled = true;

            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = authType,
                Region = region
            });
            Assert.IsTrue(authenticationCalled);
            Assert.IsFalse(connectedCalled);
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympics_And_Repeat_SameValues_With_SignOut() => UniTask.ToCoroutine(async () =>
        {
            var authType = AuthType.ClientSecret;
            var region = new RegionData(ElympicsRegions.Warsaw);

            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = authType,
                Region = region
            });
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);

            var authenticationCalled = false;
            var connectedCalled = false;
            _sut.AuthenticationSucceeded += (_) => authenticationCalled = true;
            _sut.WebSocketSession.Connected += () => connectedCalled = true;
            _sut.SignOut();
            Assert.AreEqual((int)ElympicsState.Disconnected, (int)_sut.CurrentState.State);
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = authType,
                Region = region
            });
            Assert.IsTrue(authenticationCalled);
            Assert.True(connectedCalled);
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympics_WIth_Selected_Not_Valid_Region() => UniTask.ToCoroutine(async () =>
        {

            _ = await AsyncAsserts.AssertThrowsAsync<ElympicsException>(async () => await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret,
                Region = new RegionData("WrongRegion")
            }));
            Assert.IsFalse(_sut!.IsAuthenticated);
            Assert.IsFalse(_sut.WebSocketSession.IsConnected);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympics_OneRoomIsJoined([ValueSource(nameof(connectTestValue))] AuthType type) => UniTask.ToCoroutine(async () =>
        {
            _ = _sut!.MockIWebSocket(UserId, Nickname, true, null, out _);
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = type
            });
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.AreEqual(1, _sut.RoomsManager.ListJoinedRooms().Count);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympics_And_Switch_Region_Different_Than_Current() => UniTask.ToCoroutine(async () =>
        {
            var authenticationCalled = false;
            var connectedCalled = false;

            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret,
                Region = new RegionData(ElympicsRegions.Warsaw)
            });
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
            _sut.AuthenticationSucceeded += (_) => authenticationCalled = true;
            _sut.WebSocketSession.Connected += () => connectedCalled = true;
            await _sut.ConnectToElympicsAsync(new ConnectionData()
            {
                Region = new RegionData(ElympicsRegions.Mumbai)
            });
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.IsTrue(authenticationCalled);
            Assert.IsTrue(connectedCalled);
            Assert.AreSame(ElympicsRegions.Mumbai, _sut.CurrentRegion);
        });

        [UnityTest]
        public IEnumerator ConnectToElympics_And_Switch_Region_Same_As_Current() => UniTask.ToCoroutine(async () =>
        {
            var authenticationCalled = false;
            var connectedCalled = false;

            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret,
                Region = new RegionData(ElympicsRegions.Warsaw)
            });
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
            _sut.AuthenticationSucceeded += (_) => authenticationCalled = true;
            _sut.WebSocketSession.Connected += () => connectedCalled = true;
            await _sut.ConnectToElympicsAsync(new ConnectionData()
            {
                Region = new RegionData(ElympicsRegions.Warsaw)
            });
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.True(authenticationCalled);
            Assert.IsFalse(connectedCalled);
        });
        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympics_UseCachedData_AutoRetry_Succeed() => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret,
            });
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
            var cachedData = _sut.AuthData;
            Assert.NotNull(cachedData);
            _sut.SignOut();
            Assert.AreEqual((int)ElympicsState.Disconnected, (int)_sut.CurrentState.State);
            await _sut.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthFromCacheData = new CachedAuthData()
                {
                    CachedData = new AuthData(cachedData!.UserId, ExpiredClientAuthJwt, cachedData.Nickname, cachedData.AuthType),
                    AutoRetryIfExpired = true,
                }
            });
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
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

            _ = await AsyncAsserts.AssertThrowsAsync<ElympicsException>(async () => await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthFromCacheData = new CachedAuthData()
                {
                    CachedData = new AuthData(Guid.Empty, ExpiredClientAuthJwt, CachedNickname, AuthType.ClientSecret),
                    AutoRetryIfExpired = false,
                }
            }));
            Assert.AreEqual((int)ElympicsState.Disconnected, (int)_sut.CurrentState.State);
            Assert.IsFalse(_sut!.IsAuthenticated);
            Assert.IsFalse(_sut!.WebSocketSession.IsConnected);
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
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.AreEqual(cache!.UserId, _sut.AuthData!.UserId);
            Assert.AreEqual(cache.Nickname, _sut.AuthData!.Nickname);
            Assert.AreSame(cache.JwtToken, _sut.AuthData!.JwtToken);
            Assert.AreEqual(cache.AuthType, _sut.AuthData!.AuthType);
        });
        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympics_UseSameDataToAuthenticate() => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });
            var cache = _sut.AuthData;
            Assert.NotNull(cache);

            var disconnected = false;
            _sut.WebSocketSession.Disconnected += (_) => disconnected = false;
            await _sut.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthFromCacheData = new CachedAuthData()
                {
                    CachedData = cache,
                    AutoRetryIfExpired = false,
                }
            });

            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.IsFalse(disconnected);
            Assert.IsTrue(_sut.AuthData is not null);
        });
        [UnityTest]
        public IEnumerator ConnectToElympics_And_Receive_Ping_Messages() => UniTask.ToCoroutine(async () =>
        {
            _ = _sut!.MockIWebSocket(UserId, Nickname, false, PingTimeoutTestSec - 0.1d, out _).SetPingThresholdTimeout(TimeSpan.FromSeconds(PingTimeoutTestSec));
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });

            var disconnectedCalled = false;
            _sut.WebSocketSession.Disconnected += (_) => disconnectedCalled = true;
            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutTestSec * 2));
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.IsFalse(disconnectedCalled);
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
        });

        [UnityTest]
        public IEnumerator ConnectToElympics_And_Then_Get_Disconnected_When_Ping_Message_Did_Not_Arrived() => UniTask.ToCoroutine(async () =>
        {
            _ = _sut!.SetPingThresholdTimeout(TimeSpan.FromSeconds(PingTimeoutTestSec));
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });

            var disconnectedCalled = false;
            _sut.WebSocketSession.Disconnected += (_) => disconnectedCalled = true;

            LogAssert.Expect(LogType.Error, new Regex(@"\[ElympicsSdk\]"));
            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutTestSec + 2));
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsFalse(_sut.WebSocketSession.IsConnected);
            Assert.IsTrue(disconnectedCalled);
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
        });

        [UnityTest]
        public IEnumerator DisconnectWebSocket_ConnectAgain() => UniTask.ToCoroutine(async () =>
        {
            _ = _sut!.SetPingThresholdTimeout(TimeSpan.FromSeconds(PingTimeoutTestSec));
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });

            var disconnectedCalled = false;
            _sut.WebSocketSession.Disconnected += (_) => disconnectedCalled = true;

            LogAssert.Expect(LogType.Error, new Regex(@"\[ElympicsSdk\]"));
            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutTestSec + 2));
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsFalse(_sut.WebSocketSession.IsConnected);
            Assert.IsTrue(disconnectedCalled);
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);

            var connectedCalled = false;
            var authCalled = false;
            _sut.WebSocketSession.Connected += () => connectedCalled = true;
            _sut.AuthenticationSucceeded += _ => authCalled = true;
            _ = _sut!.SetPingThresholdTimeout(TimeSpan.FromSeconds(DefaultPingTimeoutSec));
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.IsTrue(connectedCalled);
            Assert.IsTrue(authCalled);
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);

        });
        [UnityTest]
        public IEnumerator ConnectToElympics_And_Then_Get_Disconnected_When_Ping_Message_Did_Not_Arrived_And_Reconnect() => UniTask.ToCoroutine(async () =>
        {
            var authenticationCalled = false;
            var connectedCalled = false;
            var disconnectedCalled = false;

            _ = _sut!.SetPingThresholdTimeout(TimeSpan.FromSeconds(PingTimeoutTestSec));
            _sut!.WebSocketSession.Connected += () => connectedCalled = true;
            _sut.AuthenticationSucceeded += (_) => authenticationCalled = true;
            _sut.WebSocketSession.Disconnected += (_) => disconnectedCalled = true;
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret,
                Region = new RegionData()
                {
                    Name = ElympicsRegions.Warsaw
                }
            });
            LogAssert.Expect(LogType.Error, new Regex(@"\[ElympicsSdk\]"));
            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutTestSec + 2));
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsFalse(_sut.WebSocketSession.IsConnected);
            Assert.IsTrue(authenticationCalled);
            Assert.IsTrue(connectedCalled);
            Assert.IsTrue(disconnectedCalled);
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);

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
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.IsTrue(authenticationCalled);
            Assert.IsTrue(connectedCalled);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympics_No_Auth_Data_Only_Region() => UniTask.ToCoroutine(async () =>
        {
            var authenticationCalled = false;
            var connectedCalled = false;

            _sut!.AuthenticationSucceeded += (_) => authenticationCalled = true;
            _sut.WebSocketSession.Connected += () => connectedCalled = true;

            _ = await AsyncAsserts.AssertThrowsAsync<ElympicsException>(async () => await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                Region = new RegionData(ElympicsRegions.Warsaw)
            }));
            Assert.AreEqual((int)ElympicsState.Disconnected, (int)_sut.CurrentState.State);
            Assert.IsFalse(_sut.IsAuthenticated);
            Assert.IsFalse(_sut.WebSocketSession.IsConnected);
            Assert.IsFalse(authenticationCalled);
            Assert.IsFalse(connectedCalled);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympics_With_Selected_Valid_Region([ValueSource(nameof(connectTestValue))] AuthType type) => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = type,
                Region = new RegionData(ElympicsRegions.Mumbai)
            });
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.AreSame(ElympicsRegions.Mumbai, _sut.CurrentRegion);
        });

        [TearDown]
        public void CleanUp()
        {
            ElympicsLogger.Log($"{nameof(ElympicsLobbyClientTest)} Cleanup");
            if (_sut!.IsAuthenticated)
                _sut.SignOut();
            _ = _sut.MockIWebSocket(UserId, Nickname, false, null, out _).SetPingThresholdTimeout(TimeSpan.FromSeconds(DefaultPingTimeoutSec));
            WebSocketMockSetup.CancelPingToken();
        }

        [OneTimeTearDown]
        public void FinishTests() => Object.Destroy(_sut);
    }
}
