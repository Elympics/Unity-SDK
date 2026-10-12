#nullable enable

using System.Net;
using NUnit.Framework;
using UnityEngine;

namespace Elympics.Tests
{
    [Category("HalfRemote")]
    public class TestHalfRemoteSettings
    {
        private ElympicsGameConfig _config = null!;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ElympicsGameConfig>();
            _config.maxPlayers = 4;
            for (var i = 0; i < 3; i++)
                _config.TestPlayers.Add(new ElympicsGameConfig.InitialUserData());
            _config.TestMatchData.queueName = "config-queue";
            _config.TestMatchData.regionName = "config-region";
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        #region Client

        [Test]
        public void EmptyClientDataResolvesToConfig()
        {
            var settings = HalfRemoteClientSettings.Resolve(new HalfRemoteClientLaunchData(), _config);

            Assert.That(settings.UseWeb, Is.EqualTo(_config.UseWeb));
            Assert.That(settings.ServerEndPoint.Address.ToString(), Is.EqualTo(_config.IpForHalfRemoteMode));
            Assert.That(settings.ServerEndPoint.Port, Is.EqualTo(_config.PortForHalfRemoteMode));
            Assert.That(settings.PlayerIndex, Is.EqualTo(_config.PlayerIndexForHalfRemoteMode));
            Assert.That(settings.QueueName, Is.EqualTo("config-queue"));
            Assert.That(settings.RegionName, Is.EqualTo("config-region"));
            Assert.That(settings.GameEngineData, Is.Null);
            Assert.That(settings.MatchmakerData, Is.Null);
        }

        [Test]
        public void NullClientDataResolvesLikeEmpty()
        {
            var fromNull = HalfRemoteClientSettings.Resolve(null, _config);
            var fromEmpty = HalfRemoteClientSettings.Resolve(new HalfRemoteClientLaunchData(), _config);

            Assert.That(fromNull.ServerEndPoint, Is.EqualTo(fromEmpty.ServerEndPoint));
            Assert.That(fromNull.UseWeb, Is.EqualTo(fromEmpty.UseWeb));
            Assert.That(fromNull.PlayerIndex, Is.EqualTo(fromEmpty.PlayerIndex));
        }

        [Test]
        public void ClientDataOverridesConfig()
        {
            var data = new HalfRemoteClientLaunchData("10.1.2.3:4567", useTcpUdpInsteadOfWebRtc: true, playerIndex: 2, regionName: "r", queueName: "q",
                gameEngineData: new byte[] { 7 }, matchmakerData: new[] { 1.5f });

            var settings = HalfRemoteClientSettings.Resolve(data, _config);

            Assert.That(settings.ServerEndPoint, Is.EqualTo(new IPEndPoint(IPAddress.Parse("10.1.2.3"), 4567)));
            Assert.That(settings.UseWeb, Is.False);
            Assert.That(settings.PlayerIndex, Is.EqualTo(2));
            Assert.That(settings.QueueName, Is.EqualTo("q"));
            Assert.That(settings.RegionName, Is.EqualTo("r"));
            Assert.That(settings.GameEngineData, Is.EqualTo(new byte[] { 7 }));
            Assert.That(settings.MatchmakerData, Is.EqualTo(new[] { 1.5f }));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ClientPortFollowsOverriddenTransport(bool useTcpUdp)
        {
            var settings = HalfRemoteClientSettings.Resolve(new HalfRemoteClientLaunchData(useTcpUdpInsteadOfWebRtc: useTcpUdp), _config);

            Assert.That(settings.UseWeb, Is.EqualTo(!useTcpUdp));
            Assert.That(settings.ServerEndPoint.Port, Is.EqualTo(useTcpUdp ? _config.TcpPortForHalfRemoteMode : _config.WebPortForHalfRemoteMode));
        }

        #endregion

        #region Server

        [Test]
        public void EmptyServerDataListensOnBothWithConfigEndpoints()
        {
            var settings = HalfRemoteServerSettings.Resolve(new HalfRemoteServerLaunchData(), _config);
            var address = IPAddress.Parse(_config.IpForHalfRemoteMode);

            Assert.That(settings.TcpUdpEndPoint, Is.EqualTo(new IPEndPoint(address, _config.TcpPortForHalfRemoteMode)));
            Assert.That(settings.WebEndPoint, Is.EqualTo(new IPEndPoint(address, _config.WebPortForHalfRemoteMode)));
            Assert.That(settings.PlayerCount, Is.EqualTo(3));
            Assert.That(settings.Warnings, Is.Empty);
        }

        [Test]
        public void ServerDataOverridesConfig()
        {
            var data = new HalfRemoteServerLaunchData(ElympicsHalfRemoteProtocols.Both, "0.0.0.0", 9001, 9002, 2);

            var settings = HalfRemoteServerSettings.Resolve(data, _config);

            Assert.That(settings.TcpUdpEndPoint, Is.EqualTo(new IPEndPoint(IPAddress.Any, 9001)));
            Assert.That(settings.WebEndPoint, Is.EqualTo(new IPEndPoint(IPAddress.Any, 9002)));
            Assert.That(settings.PlayerCount, Is.EqualTo(2));
        }

        [Test]
        public void TcpUdpOnlyServerHasNoWebEndpoint()
        {
            var settings = HalfRemoteServerSettings.Resolve(new HalfRemoteServerLaunchData(ElympicsHalfRemoteProtocols.TcpUdp), _config);

            Assert.That(settings.TcpUdpEndPoint, Is.Not.Null);
            Assert.That(settings.WebEndPoint, Is.Null);
        }

        [Test]
        public void WebRtcOnlyServerHasNoTcpUdpEndpoint()
        {
            var settings = HalfRemoteServerSettings.Resolve(new HalfRemoteServerLaunchData(ElympicsHalfRemoteProtocols.WebRtc), _config);

            Assert.That(settings.TcpUdpEndPoint, Is.Null);
            Assert.That(settings.WebEndPoint, Is.Not.Null);
        }

        [Test]
        public void PortForUnlistenedProtocolIsIgnoredWithWarning()
        {
            var settings = HalfRemoteServerSettings.Resolve(new HalfRemoteServerLaunchData(ElympicsHalfRemoteProtocols.WebRtc, tcpUdpPort: 9001, signalingPort: 9002), _config);

            Assert.That(settings.TcpUdpEndPoint, Is.Null);
            Assert.That(settings.WebEndPoint!.Port, Is.EqualTo(9002));
            Assert.That(settings.Warnings, Has.Count.EqualTo(1));
            Assert.That(settings.Warnings[0], Does.Contain("TcpUdpPort"));
        }

        [Test]
        public void MaxPlayersAboveTestPlayersUsesAllTestPlayers() =>
            Assert.That(HalfRemoteServerSettings.Resolve(new HalfRemoteServerLaunchData(maxPlayers: 4), _config).PlayerCount, Is.EqualTo(3));

        #endregion
    }
}
