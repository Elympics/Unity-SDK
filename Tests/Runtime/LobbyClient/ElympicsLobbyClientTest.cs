using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Utils;
using Elympics.Models.Authentication;
using Elympics.Tests.Common;
using HybridWebSocket;
using NSubstitute;
using NSubstitute.ClearExtensions;
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
        private readonly IAuthClient _authClientMock = Substitute.For<IAuthClient>();
        private readonly IWebSocket _webSocketMock = Substitute.For<IWebSocket>();
        private readonly IAvailableRegionRetriever _regionRetrieverMock = Substitute.For<IAvailableRegionRetriever>();


        public override string SceneName => "ElympicsLobbyClientTestScene";
        public override bool RequiresElympicsConfig => true;
        private const int TestsTimeoutMs = 200000;
        private const double PingTimeoutTestSec = 1.1;
        private const double DefaultPingTimeoutSec = 30;

        private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private const string Nickname = "nickname";
        private const string? AvatarUrl = null;

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
            _ = _sut!.InjectMockIAuthClient(_authClientMock).InjectMockIWebSocket(_webSocketMock).InjectRegionIAvailableRegionRetriever(_regionRetrieverMock);
            _ = _authClientMock.CreateSuccessIAuthClient(UserId, Nickname);
            _ = _regionRetrieverMock.GetAvailableRegions()
                .Returns(UniTask.FromResult(new List<string> { ElympicsRegions.Warsaw, ElympicsRegions.Mumbai, ElympicsRegions.Tokyo, ElympicsRegions.Dallas }));
            _webSocketMock.ClearSubstitute();
        }

        private static AuthType[] connectTestValue = { AuthType.ClientSecret, AuthType.EthAddress };

        [UnityTest]
        public IEnumerator ConnectToElympics([ValueSource(nameof(connectTestValue))] AuthType type) => UniTask.ToCoroutine(async () =>
        {
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
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
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
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
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
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
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
            const AuthType authType = AuthType.ClientSecret;
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
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
            const AuthType authType = AuthType.ClientSecret;
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
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
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
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(true, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
            await _sut!.ConnectToElympicsAsync(new ConnectionData
            {
                AuthType = type
            });
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.That(_sut.RoomsManager.CurrentRoom, Is.Not.Null);
        });

        [UnityTest]
        [Timeout(TestsTimeoutMs)]
        public IEnumerator ConnectToElympics_And_Switch_Region_Different_Than_Current() => UniTask.ToCoroutine(async () =>
        {
            var authenticationCalled = false;
            var connectedCalled = false;
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
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
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
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
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
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
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
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
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
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
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
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
            ElympicsTimeout.WebSocketHeartbeatTimeout = TimeSpan.FromSeconds(PingTimeoutTestSec);
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl)
                .SetPingDelayMessage(PingTimeoutTestSec - 0.1d);

            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });

            var disconnectedCalled = false;
            _sut.WebSocketSession.Disconnected += (_) => disconnectedCalled = true;
            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutTestSec * 2), DelayType.Realtime);
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.IsFalse(disconnectedCalled);
            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
        });

        [UnityTest]
        public IEnumerator ConnectToElympics_And_Then_Get_Disconnected_When_Ping_Message_Did_Not_Arrived() => UniTask.ToCoroutine(async () =>
        {
            ElympicsTimeout.WebSocketHeartbeatTimeout = TimeSpan.FromSeconds(PingTimeoutTestSec);
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });
            LogAssert.Expect(LogType.Error, new Regex(@"\[ElympicsSdk\]"));
            var webSocketDisconnectCalled = false;
            var elympcisConnectionLostCalled = false;
            _sut.WebSocketSession.Disconnected += (_) => webSocketDisconnectCalled = true;
            _sut.ElympicsConnectionLost += (_) => elympcisConnectionLostCalled = true;

            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutTestSec + 2), DelayType.Realtime);
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsFalse(_sut.WebSocketSession.IsConnected);
            Assert.AreEqual((int)ElympicsState.Disconnected, (int)_sut.CurrentState.State);
            Assert.IsTrue(webSocketDisconnectCalled);
            Assert.IsTrue(elympcisConnectionLostCalled);
        });

        [UnityTest]
        public IEnumerator ConnectToElympics_And_Then_Get_Disconnected_Because_Lobby_Scales_Down() => UniTask.ToCoroutine(async () =>
        {
            _webSocketMock.ClearSubstitute();
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(true, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);

            List<(ElympicsState, ElympicsState)> statesCalled = new();
            _sut.StateChanged += (oldState, newState) => statesCalled.Add((oldState, newState));
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });
            var disconnectedCalled = false;
            _sut.WebSocketSession.Disconnected += (_) => disconnectedCalled = true;
            LogAssert.Expect(LogType.Error, new Regex(@"\[ElympicsSdk\]"));
            _webSocketMock.OnClose += Raise.Event<WebSocketCloseEventHandler>(WebSocketCloseCode.Away, Arg.Any<string>());

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            await UniTask.WaitUntil(() => statesCalled.Count == 4, PlayerLoopTiming.Update, cts.Token);

            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsTrue(_sut.WebSocketSession.IsConnected);
            Assert.IsFalse(disconnectedCalled);
            Assert.AreEqual((int)ElympicsState.Disconnected, (int)statesCalled[0].Item1);
            Assert.AreEqual((int)ElympicsState.Connecting, (int)statesCalled[0].Item2);

            Assert.AreEqual((int)ElympicsState.Connecting, (int)statesCalled[1].Item1);
            Assert.AreEqual((int)ElympicsState.Connected, (int)statesCalled[1].Item2);

            Assert.AreEqual((int)ElympicsState.Connected, (int)statesCalled[2].Item1);
            Assert.AreEqual((int)ElympicsState.Reconnecting, (int)statesCalled[2].Item2);

            Assert.AreEqual((int)ElympicsState.Reconnecting, (int)statesCalled[3].Item1);
            Assert.AreEqual((int)ElympicsState.Connected, (int)statesCalled[3].Item2);

            Assert.AreEqual((int)ElympicsState.Connected, (int)_sut.CurrentState.State);
        });

        [UnityTest]
        public IEnumerator DisconnectWebSocket_ConnectAgain() => UniTask.ToCoroutine(async () =>
        {
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
            ElympicsTimeout.WebSocketHeartbeatTimeout = TimeSpan.FromSeconds(PingTimeoutTestSec);
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });

            var disconnectedCalled = false;
            _sut.WebSocketSession.Disconnected += (_) => disconnectedCalled = true;

            LogAssert.Expect(LogType.Error, new Regex(@"\[ElympicsSdk\]"));
            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutTestSec + 2), DelayType.Realtime);
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsFalse(_sut.WebSocketSession.IsConnected);
            Assert.IsTrue(disconnectedCalled);
            Assert.AreEqual((int)ElympicsState.Disconnected, (int)_sut.CurrentState.State);

            var connectedCalled = false;
            var authCalled = false;
            _sut.WebSocketSession.Connected += () => connectedCalled = true;
            _sut.AuthenticationSucceeded += _ => authCalled = true;
            ElympicsTimeout.WebSocketHeartbeatTimeout = TimeSpan.FromSeconds(DefaultPingTimeoutSec);
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

            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
            ElympicsTimeout.WebSocketHeartbeatTimeout = TimeSpan.FromSeconds(PingTimeoutTestSec);
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
            await UniTask.Delay(TimeSpan.FromSeconds(PingTimeoutTestSec + 5), DelayType.Realtime);
            Assert.IsTrue(_sut.IsAuthenticated);
            Assert.IsFalse(_sut.WebSocketSession.IsConnected);
            Assert.IsTrue(authenticationCalled);
            Assert.IsTrue(connectedCalled);
            Assert.IsTrue(disconnectedCalled);
            Assert.AreEqual((int)ElympicsState.Disconnected, (int)_sut.CurrentState.State);

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
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
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
            _ = _webSocketMock.SetupOpenCloseDefaultBehaviour().SetupJoinLobby(false, UserId, Nickname, AvatarUrl).SetShowAuthMessage(UserId, Nickname, AvatarUrl);
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
            _webSocketMock.ClearSubstitute();
            ElympicsTimeout.WebSocketHeartbeatTimeout = TimeSpan.FromSeconds(DefaultPingTimeoutSec);
            WebSocketMockSetup.CancelPingToken();
        }

        [OneTimeTearDown]
        public void FinishTests() => Object.Destroy(_sut);
    }
}
