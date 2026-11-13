#nullable enable

using System;
using System.Text;
using System.Threading;
using Elympics.Cysharp.Threading.Tasks;
using MatchTcpClients;
using MatchTcpClients.Synchronizer;
using MatchTcpModels.Commands;
using NSubstitute;
using NUnit.Framework;

namespace Elympics.Tests
{
    [Category("Launcher")]
    public class TestJoinCredentials
    {
        private const string FakeSecret = "fake-user-secret";
        private const string FakeJoinToken = "fake-header.fake-claims.fake-signature";

        private static string SerializeCommand(Command command) =>
            Encoding.ASCII.GetString(new GameServerJsonSerializer().Serialize(command));

        [Test]
        public void JoinTokenReplacesUserSecretOnTheWire()
        {
            var json = SerializeCommand(GameServerClient.CreateAuthenticatePlayerCommand(FakeSecret, FakeJoinToken));

            Assert.That(json, Does.Contain($"\"JoinToken\":\"{FakeJoinToken}\""));
            Assert.That(json, Does.Contain("\"UserSecret\":\"\""));
        }

        [TestCase(null)]
        [TestCase("")]
        public void UserSecretIsSentWithoutJoinToken(string? joinToken)
        {
            var json = SerializeCommand(GameServerClient.CreateAuthenticatePlayerCommand(FakeSecret, joinToken));

            Assert.That(json, Does.Contain($"\"UserSecret\":\"{FakeSecret}\""));
            Assert.That(json, Does.Contain("\"JoinToken\":\"\""));
        }

        [Test]
        public void SpectatorCommandCarriesJoinToken()
        {
            var json = SerializeCommand(new AuthenticateAsSpectatorCommand { JoinToken = FakeJoinToken });

            Assert.That(json, Does.Contain($"\"JoinToken\":\"{FakeJoinToken}\""));
        }

        [TestCase(FakeSecret, null)]
        [TestCase("", FakeJoinToken)]
        [TestCase(FakeSecret, FakeJoinToken)]
        public void PlayerJoinPassesBothCredentials(string userSecret, string? joinToken)
        {
            var gameServerClient = Substitute.For<IGameServerClient>();
            var connectClient = new RemoteMatchConnectClient(gameServerClient, userSecret, joinToken);
            using var cts = new CancellationTokenSource();

            var join = connectClient.ConnectAndJoinAsPlayerAsync(cts.Token);
            gameServerClient.ConnectedAndSynchronized += Raise.Event<Action<TimeSynchronizationData>>(TimeSynchronizationData.Localhost);
            cts.Cancel();

            gameServerClient.Received(1).AuthenticateMatchUserSecretAsync(userSecret, joinToken);
            Assert.That(join.Status.IsCanceled(), Is.True);
        }

        [Test]
        public void SpectatorJoinPassesJoinToken()
        {
            var gameServerClient = Substitute.For<IGameServerClient>();
            var connectClient = new RemoteMatchConnectClient(gameServerClient, "", FakeJoinToken);
            using var cts = new CancellationTokenSource();

            var join = connectClient.ConnectAndJoinAsSpectatorAsync(cts.Token);
            gameServerClient.ConnectedAndSynchronized += Raise.Event<Action<TimeSynchronizationData>>(TimeSynchronizationData.Localhost);
            cts.Cancel();

            gameServerClient.Received(1).AuthenticateAsSpectatorAsync(FakeJoinToken);
            Assert.That(join.Status.IsCanceled(), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        public void PlayerJoinWithoutCredentialsThrows(string? joinToken)
        {
            var connectClient = new RemoteMatchConnectClient(Substitute.For<IGameServerClient>(), "", joinToken);

            _ = Assert.Throws<ArgumentNullException>(() => connectClient.ConnectAndJoinAsPlayerAsync(CancellationToken.None));
        }
    }
}
