using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Models.Authentication;
using Elympics.Tests.Common;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace Elympics.Tests
{
    public class ElympicsLobbyClientStateTest : ElympicsMonoBaseTest
    {
        public override string SceneName => "ElympicsLobbyClientStateMachineTestScene";
        private ElympicsLobbyClient _sut;

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

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene(SceneName);
            yield return new WaitUntil(() => ElympicsLobbyClient.Instance != null);
            _sut = ElympicsLobbyClient.Instance;
            Assert.NotNull(_sut);
            _ = _sut!.MockSuccessIAuthClient(FakeJwt, UserId, Nickname).MockIWebSocket(UserId, Nickname, false, null, out _).MockIAvailableRegionRetriever(ElympicsRegions.Warsaw, ElympicsRegions.Mumbai, ElympicsRegions.Tokyo, ElympicsRegions.Dallas).MockIRoomManager();
        }

        public List<(ElympicsState, ElympicsState)> _stateTransitions = new();

        [SetUp]
        public void SetupSut()
        {
            _sut.StateChanged += OnStateChanged;
            _stateTransitions.Clear();
        }

        [UnityTest]
        public IEnumerator ConnectToElympics() => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });

            Assert.AreEqual(_stateTransitions.Count, 2);
            AssertStateTransition(0, ElympicsState.Disconnected, ElympicsState.Connecting);
            AssertStateTransition(1, ElympicsState.Connecting, ElympicsState.Connected);
        });

        [UnityTest]
        public IEnumerator ConnectToElympics_Twice() => UniTask.ToCoroutine(async () =>
        {
            _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            }).Forget();

            _ = await AsyncAsserts.AssertThrowsAsync<ElympicsException>(async () => await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                Region = new RegionData(ElympicsRegions.Warsaw)
            }));

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(1));
            var isCanceled = await UniTask.WaitUntil(() => _stateTransitions.Count == 2, PlayerLoopTiming.Update, cts.Token).SuppressCancellationThrow();
            Assert.IsFalse(isCanceled);
            Assert.AreEqual(_stateTransitions.Count, 2);
            AssertStateTransition(0, ElympicsState.Disconnected, ElympicsState.Connecting);
            AssertStateTransition(1, ElympicsState.Connecting, ElympicsState.Connected);
        });

        [UnityTest]
        public IEnumerator ConnectToElympics_StartMatchmaking() => UniTask.ToCoroutine(async () =>
        {
            await _sut!.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret
            });

            var room = _sut.RoomsManager.ListJoinedRooms()[0];
            await room.StartMatchmaking();
            Assert.AreEqual(_stateTransitions.Count, 3);
            AssertStateTransition(0, ElympicsState.Disconnected, ElympicsState.Connecting);
            AssertStateTransition(1, ElympicsState.Connecting, ElympicsState.Connected);
            AssertStateTransition(2, ElympicsState.Connected, ElympicsState.Matchmaking);
        });

        [TearDown]
        public void CleanUp()
        {
            ElympicsLogger.Log($"{nameof(ElympicsLobbyClientTest)} Cleanup");
            if (_sut!.IsAuthenticated)
            {
                _sut.SwitchState(ElympicsState.Connected);
                _sut.SignOut();
            }
            _sut.StateChanged -= OnStateChanged;
        }

        [OneTimeTearDown]
        public void FinishTests() => Object.Destroy(_sut);

        private void OnStateChanged(ElympicsState oldState, ElympicsState newState) => _stateTransitions.Add((oldState, newState));
        private void AssertStateTransition(int index, ElympicsState expectedOld, ElympicsState expectedNew)
        {
            Assert.AreEqual((int)_stateTransitions[index].Item1, (int)expectedOld);
            Assert.AreEqual((int)_stateTransitions[index].Item2, (int)expectedNew);
        }
    }
}
