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

#nullable enable

namespace Elympics.Tests
{
    public class ElympicsLobbyClientStateTest : ElympicsMonoBaseTest
    {
        public override string SceneName => "ElympicsLobbyClientStateMachineTestScene";
        public override bool RequiresElympicsConfig => true;
        private ElympicsLobbyClient _sut;

        private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private const string Nickname = "nickname";
        private const string? AvatarUrl = null;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene(SceneName);
            yield return new WaitUntil(() => ElympicsLobbyClient.Instance != null);
            _sut = ElympicsLobbyClient.Instance;
            Assert.NotNull(_sut);
            _ = _sut!.MockSuccessIAuthClient(UserId, Nickname)
                .MockIWebSocket(UserId, Nickname, AvatarUrl, false, null, out _)
                .MockIAvailableRegionRetriever(ElympicsRegions.Warsaw, ElympicsRegions.Mumbai, ElympicsRegions.Tokyo, ElympicsRegions.Dallas)
                .MockIRoomManager();
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

        private void OnStateChanged(ElympicsState oldState, ElympicsState newState)
        {
            Debug.Log($"Test state old {oldState} new {newState}");
            _stateTransitions.Add((oldState, newState));
        }

        private void AssertStateTransition(int index, ElympicsState expectedOld, ElympicsState expectedNew)
        {
            Assert.AreEqual((int)_stateTransitions[index].Item1, (int)expectedOld);
            Assert.AreEqual((int)_stateTransitions[index].Item2, (int)expectedNew);
        }
    }
}
