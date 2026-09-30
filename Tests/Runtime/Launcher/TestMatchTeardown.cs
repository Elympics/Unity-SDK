#nullable enable

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Replication;
using MatchTcpClients;
using MatchTcpLibrary;
using MatchTcpLibrary.TransportLayer.Interfaces;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Elympics.Tests
{
    [Category("Launcher")]
    public class TestMatchTeardown
    {
        private sealed class FakeGameServerClient : GameServerClient
        {
            public readonly INetworkClient Network = Substitute.For<INetworkClient>();

            public FakeGameServerClient() : base(Substitute.For<IGameServerSerializer>(), default)
            { }

            protected override INetworkClient CreateNetworkClient() => Network;

            // Never connects, so the test can dispose mid-connect.
            protected override UniTask ConnectInternalAsync(CancellationToken ct = default) => UniTask.Never(ct);
        }

        private sealed class NullSink : IElympicsMatchLifecycleSink
        {
            public void SceneInitializationFailed(ElympicsLaunchErrorCode code, string message) { }
            public void MatchJoined() { }
            public void JoinFailed(string message) { }
            public void MatchEnded(Guid matchId) { }
            public void Disconnected(bool byServer) { }
        }

        private static ElympicsMatchSession NewSession()
        {
            var game = ScriptableObject.CreateInstance<ElympicsGameConfig>();
            var launch = new ElympicsLaunch("r", game, new SinglePlayerLaunchData(), TimeSpan.FromSeconds(1), null);
            return new ElympicsMatchSession(launch, new NullSink());
        }

        [Test]
        public void GameServerClientDisposeReleasesNetworkClientMidConnect()
        {
            var client = new FakeGameServerClient();
            _ = client.ConnectAsync();

            client.Dispose();
            client.Dispose();

            client.Network.Received(1).Dispose();
            client.Network.Received(1).DataReceived -= Arg.Any<Action<string, byte[]>>();
            client.Network.Received(1).Disconnected -= Arg.Any<Action>();
        }

        [Test]
        public void GameServerClientCannotConnectAfterDispose()
        {
            var client = new FakeGameServerClient();
            client.Dispose();
            _ = Assert.ThrowsAsync<ObjectDisposedException>(async () => await client.ConnectAsync());
        }

        [Test]
        public void RemoteMatchConnectClientDisposeDisposesGameServerClient()
        {
            var gameServerClient = Substitute.For<IGameServerClient>();
            var connectClient = new RemoteMatchConnectClient(gameServerClient, "secret");

            connectClient.Dispose();

            gameServerClient.Received(1).Dispose();
            gameServerClient.Received().Disconnected -= Arg.Any<Action>();
        }

        [Test]
        public void SessionDetachRunsStepsInOrderAndSurvivesFailures()
        {
            var session = NewSession();
            var order = new List<string>();
            session.RegisterTeardown("first", () => order.Add("first"));
            session.RegisterTeardown("throwing", () => throw new InvalidOperationException("boom"));
            session.RegisterTeardown("last", () => order.Add("last"));
            LogAssert.Expect(LogType.Exception, new Regex("throwing"));

            session.Detach();
            session.Detach();

            Assert.That(order, Is.EqualTo(new[] { "first", "last" }));
            Assert.That(session.Token.IsCancellationRequested, Is.True);
        }

        [Test]
        public void SessionTokenStaysReadableAfterDetach()
        {
            var session = NewSession();
            session.Detach();
            Assert.DoesNotThrow(() => _ = session.Token.IsCancellationRequested);
        }

        [Test]
        public void ProcessGlobalsAreRestored()
        {
            var original = ElympicsProcessGlobals.Capture();
            var fixedDeltaTime = Time.fixedDeltaTime;
            var targetFrameRate = Application.targetFrameRate;
            var autoSimulation = Physics.autoSimulation;
            try
            {
                Time.fixedDeltaTime = 0.1234f;
                Application.targetFrameRate = 7;
                Physics.autoSimulation = !autoSimulation;

                original.Restore();

                Assert.That(Time.fixedDeltaTime, Is.EqualTo(fixedDeltaTime));
                Assert.That(Application.targetFrameRate, Is.EqualTo(targetFrameRate));
                Assert.That(Physics.autoSimulation, Is.EqualTo(autoSimulation));
            }
            finally
            {
                original.Restore();
            }
        }

        [Test]
        public void ReleaseStaticsDisposesReplicationWorld()
        {
            var world = new ElympicsWorld(2);
            ElympicsWorld.Current = world;
            ReplicationPipeline.Initialize(2, world);

            ElympicsMatchTeardown.ReleaseStatics();

            Assert.That(ElympicsWorld.Current, Is.Null);
            Assert.That(ReplicationPipeline.Current, Is.Null);
            Assert.That(world.IsDisposed, Is.True);
            Assert.That(ElympicsBehavioursManager.IndicesPerPlayer, Is.Zero);
        }

        [Test]
        public void DisposedWorldIgnoresLateDispose() => Assert.DoesNotThrow(() =>
        {
            var world = new ElympicsWorld(2);
            world.Dispose();
            world.Dispose();
        });

        [Test]
        public void SinglePlayerInitializerDisposeIsSafeBeforeInitialize() =>
            Assert.DoesNotThrow(() => new SinglePlayerGameInitializer(null!).Dispose());
    }
}
