#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace Elympics.Tests
{
    [Category("Launcher")]
    public class TestElympicsLaunchValidator
    {
        private const string GameId = "a1b2c3d4-0000-0000-0000-000000000001";
        private static readonly Guid UserId = new("a1b2c3d4-0000-0000-0000-00000000000a");
        private static readonly Guid OtherPlayerId = new("a1b2c3d4-0000-0000-0000-00000000000b");
        private static readonly Guid MatchId = new("a1b2c3d4-0000-0000-0000-0000000000ff");

        private ElympicsGameConfig _game = null!;
        private List<ElympicsGameConfig?> _games = null!;

        [SetUp]
        public void SetUp()
        {
            _game = ScriptableObject.CreateInstance<ElympicsGameConfig>();
            _game.gameId = GameId;
            _game.gameplayScene = "Assets/Scenes/Gameplay.unity";
            _game.maxPlayers = 4;
            _games = new List<ElympicsGameConfig?> { null, _game };
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_game);

        private static OnlineLaunchData Online(string serverAddress = "gs.example", bool? useTcpUdp = false, string userSecret = "secret", Guid? userId = null, Guid[]? players = null) =>
            new(serverAddress, useTcpUdp, MatchId, userSecret, userId ?? UserId, players ?? new[] { OtherPlayerId, UserId }, "warsaw", new byte[] { 1, 2, 3 }, new[] { 0.5f });

        private static readonly ElympicsLaunchPlatform WebGlPlayer = new(canListen: false, canUseTcpUdp: false, hasLocalFiles: false);
        private static readonly ElympicsLaunchPlatform IosPlayer = new(canListen: false, canUseTcpUdp: true, hasLocalFiles: true);

        private ElympicsLaunch Validate(LaunchData data, ElympicsLaunchOptions? options = null, ElympicsLaunchPlatform? platform = null, string gameId = GameId)
        {
            var ok = ElympicsLaunchValidator.TryValidate("request", gameId, data, options, _games, platform ?? ElympicsLaunchPlatform.Unrestricted, out var launch, out var failure);
            Assert.That(ok, Is.True, failure?.ToString());
            Assert.That(failure, Is.Null);
            return launch!;
        }

        private ElympicsLaunchFailure GetValidateFailure(LaunchData? data, ElympicsLaunchOptions? options = null, ElympicsLaunchPlatform? platform = null, string? gameId = GameId)
        {
            var ok = ElympicsLaunchValidator.TryValidate("request", gameId, data, options, _games, platform ?? ElympicsLaunchPlatform.Unrestricted, out var launch, out var failure);
            Assert.That(ok, Is.False);
            Assert.That(launch, Is.Null);
            Assert.That(failure, Is.Not.Null);
            return failure!;
        }

        private static LaunchData ParseData(ElympicsHostLaunchKind kind, string json)
        {
            var ok = ElympicsLaunchValidator.TryParseData(kind, json, out var data, out var failure);
            Assert.That(ok, Is.True, failure?.ToString());
            return data!;
        }

        private static ElympicsLaunchFailure GetParseDataFailure(ElympicsHostLaunchKind kind, string json)
        {
            var ok = ElympicsLaunchValidator.TryParseData(kind, json, out var data, out var failure);
            Assert.That(ok, Is.False);
            Assert.That(data, Is.Null);
            return failure!;
        }

        #region Game and options

        [Test]
        public void ValidLaunchKeepsGameDataAndRequestId()
        {
            var data = Online();
            var launch = Validate(data);

            Assert.That(launch.GameConfig, Is.SameAs(_game));
            Assert.That(launch.Data, Is.SameAs(data));
            Assert.That(launch.RequestId, Is.EqualTo("request"));
            Assert.That(launch.MatchId, Is.EqualTo(MatchId));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("not-a-guid")]
        public void BadGameIdIsInvalidPayload(string? gameId) =>
            Assert.That(GetValidateFailure(new SinglePlayerLaunchData(), gameId: gameId).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));

        [Test]
        public void UnknownGameIdIsUnknownGame() =>
            Assert.That(GetValidateFailure(new SinglePlayerLaunchData(), gameId: Guid.NewGuid().ToString()).Code, Is.EqualTo(ElympicsLaunchErrorCode.UnknownGame));

        [Test]
        public void GameWithoutSceneIsUnknownGame()
        {
            _game.gameplayScene = "";
            Assert.That(GetValidateFailure(new SinglePlayerLaunchData()).Code, Is.EqualTo(ElympicsLaunchErrorCode.UnknownGame));
        }

        [Test]
        public void MissingDataIsInvalidPayload() =>
            Assert.That(GetValidateFailure(null).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));

        [Test]
        public void TimeoutsDefaultAndOptOut()
        {
            var defaults = Validate(new SinglePlayerLaunchData());
            Assert.That(defaults.LaunchTimeout, Is.EqualTo(TimeSpan.FromSeconds(ElympicsLaunchDefaults.LaunchTimeoutSeconds)));
            Assert.That(defaults.PostMatchTimeout, Is.EqualTo(TimeSpan.FromSeconds(ElympicsLaunchDefaults.PostMatchTimeoutSeconds)));

            var never = Validate(new SinglePlayerLaunchData(), new ElympicsLaunchOptions { LaunchTimeout = Timeout.InfiniteTimeSpan, PostMatchTimeout = Timeout.InfiniteTimeSpan });
            Assert.That(never.LaunchTimeout, Is.Null);
            Assert.That(never.PostMatchTimeout, Is.Null);

            var custom = Validate(new SinglePlayerLaunchData(), new ElympicsLaunchOptions { LaunchTimeout = TimeSpan.FromSeconds(5), PostMatchTimeout = TimeSpan.FromSeconds(7) });
            Assert.That(custom.LaunchTimeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(custom.PostMatchTimeout, Is.EqualTo(TimeSpan.FromSeconds(7)));
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void NonPositiveTimeoutIsInvalidPayload(int seconds)
        {
            Assert.That(GetValidateFailure(new SinglePlayerLaunchData(), new ElympicsLaunchOptions { LaunchTimeout = TimeSpan.FromSeconds(seconds) }).Code,
                Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));
            Assert.That(GetValidateFailure(new SinglePlayerLaunchData(), new ElympicsLaunchOptions { PostMatchTimeout = TimeSpan.FromSeconds(seconds) }).Code,
                Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));
        }

        [TestCase("", null, null, null)]
        [TestCase("{}", null, null, null)]
        [TestCase("{\"requestId\":\"r1\",\"launchTimeoutSeconds\":5,\"postMatchTimeoutSeconds\":-1}", "r1", 5, -1)]
        public void OptionsJsonIsParsed(string json, string? requestId, int? launchSeconds, int? postMatchSeconds)
        {
            var ok = ElympicsLaunchValidator.TryParseOptions(json, out var options, out var failure);

            Assert.That(ok, Is.True, failure?.ToString());
            Assert.That(options!.RequestId, Is.EqualTo(requestId));
            Assert.That(options.LaunchTimeout, Is.EqualTo(launchSeconds is { } l ? TimeSpan.FromSeconds(l) : null));
            Assert.That(options.PostMatchTimeout, Is.EqualTo(postMatchSeconds switch
            {
                null => (TimeSpan?)null,
                -1 => Timeout.InfiniteTimeSpan,
                var p => TimeSpan.FromSeconds(p.Value),
            }));
        }

        [TestCase("{not json")]
        [TestCase("{\"launchTimeoutSeconds\":-2}")]
        [TestCase("{\"postMatchTimeoutSeconds\":-5}")]
        public void BadOptionsJsonIsInvalidPayload(string json)
        {
            var ok = ElympicsLaunchValidator.TryParseOptions(json, out _, out var failure);
            Assert.That(ok, Is.False);
            Assert.That(failure!.Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));
        }

        [Test]
        public void FailureCodeIsSerializedByName()
        {
            var json = JsonUtility.ToJson(new ElympicsLaunchFailure(ElympicsLaunchErrorCode.UnknownGame, "m"));
            Assert.That(json, Does.Contain("\"UnknownGame\""));
            Assert.That(JsonUtility.FromJson<ElympicsLaunchFailure>(json).Code, Is.EqualTo(ElympicsLaunchErrorCode.UnknownGame));
        }

        #endregion

        #region Online

        [Test]
        public void OnlineWireDataMapsToLaunchData()
        {
            var json = JsonUtility.ToJson(new OnlineLaunchDataDto
            {
                serverAddress = "gs.example",
                transport = "webRtc",
                matchId = MatchId.ToString(),
                userSecret = "secret",
                userId = UserId.ToString(),
                matchedPlayers = new[] { OtherPlayerId.ToString(), UserId.ToString() },
                regionName = "warsaw",
                gameEngineData = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
                matchmakerData = new[] { 0.5f },
            });

            var data = (OnlineLaunchData)ParseData(ElympicsHostLaunchKind.Online, json);

            Assert.That(data.ServerAddress, Is.EqualTo("gs.example"));
            Assert.That(data.UseTcpUdpInsteadOfWebRtc, Is.False);
            Assert.That(data.MatchId, Is.EqualTo(MatchId));
            Assert.That(data.UserSecret, Is.EqualTo("secret"));
            Assert.That(data.UserId, Is.EqualTo(UserId));
            Assert.That(data.MatchedPlayers, Is.EqualTo(new[] { OtherPlayerId, UserId }));
            Assert.That(data.RegionName, Is.EqualTo("warsaw"));
            Assert.That(data.GameEngineData, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(data.MatchmakerData, Is.EqualTo(new[] { 0.5f }));
            _ = Validate(data);
        }

        [TestCase("", null)]
        [TestCase("webRtc", false)]
        [TestCase("tcpUdp", true)]
        public void TransportSentinelsMapToOverride(string transport, bool? expected)
        {
            var json = "{\"transport\":\"" + transport + "\",\"matchId\":\"" + MatchId + "\",\"userId\":\"" + UserId + "\"}";
            Assert.That(((OnlineLaunchData)ParseData(ElympicsHostLaunchKind.Online, json)).UseTcpUdpInsteadOfWebRtc, Is.EqualTo(expected));
        }

        private static IEnumerable<TestCaseData> BrokenOnlineWireData()
        {
            yield return Case("bad transport", "{\"transport\":\"udp\",\"matchId\":\"" + MatchId + "\",\"userId\":\"" + UserId + "\"}");
            yield return Case("missing matchId", "{\"userId\":\"" + UserId + "\"}");
            yield return Case("bad matchId", "{\"matchId\":\"x\",\"userId\":\"" + UserId + "\"}");
            yield return Case("missing userId", "{\"matchId\":\"" + MatchId + "\"}");
            yield return Case("bad player", "{\"matchId\":\"" + MatchId + "\",\"userId\":\"" + UserId + "\",\"matchedPlayers\":[\"x\"]}");
            yield return Case("bad base64", "{\"matchId\":\"" + MatchId + "\",\"userId\":\"" + UserId + "\",\"gameEngineData\":\"***\"}");
            yield return Case("not json", "{not json");

            static TestCaseData Case(string name, string json) => new TestCaseData(json).SetName($"BrokenOnlineWireData({name})");
        }

        [TestCaseSource(nameof(BrokenOnlineWireData))]
        public void BrokenOnlineWireDataIsInvalidPayload(string json) =>
            Assert.That(GetParseDataFailure(ElympicsHostLaunchKind.Online, json).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));

        private static IEnumerable<TestCaseData> BrokenOnlineData()
        {
            yield return Case("no server address", Online(serverAddress: ""));
            yield return Case("no user secret", Online(userSecret: ""));
            yield return Case("no players", Online(players: Array.Empty<Guid>()));
            yield return Case("duplicate players", Online(players: new[] { UserId, UserId }));
            yield return Case("user not among players", Online(players: new[] { OtherPlayerId }));
            yield return Case("more players than max", Online(players: new[] { UserId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }));

            static TestCaseData Case(string name, OnlineLaunchData data) => new TestCaseData(data).SetName($"BrokenOnlineData({name})");
        }

        [TestCaseSource(nameof(BrokenOnlineData))]
        public void BrokenOnlineDataIsInvalidPayload(OnlineLaunchData data) =>
            Assert.That(GetValidateFailure(data).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));

        [Test]
        public void OnlineTcpUdpAddressWithoutPortIsInvalidPayload()
        {
            Assert.That(GetValidateFailure(Online(serverAddress: "127.0.0.1", useTcpUdp: true)).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));
            _ = Validate(Online(serverAddress: "127.0.0.1:9000", useTcpUdp: true));
        }

        [Test]
        public void TcpUdpOnWebGlIsUnsupportedMode()
        {
            Assert.That(GetValidateFailure(Online(serverAddress: "127.0.0.1:9000", useTcpUdp: true), platform: WebGlPlayer).Code, Is.EqualTo(ElympicsLaunchErrorCode.UnsupportedMode));
            Assert.That(GetValidateFailure(new HalfRemoteClientLaunchData(useTcpUdpInsteadOfWebRtc: true), platform: WebGlPlayer).Code, Is.EqualTo(ElympicsLaunchErrorCode.UnsupportedMode));
            _ = Validate(Online(serverAddress: "gs.example", useTcpUdp: false), platform: WebGlPlayer);
        }

        [Test]
        public void OnlineWebRtcAddressNeedsNoPort() => _ = Validate(Online(serverAddress: "gs.example", useTcpUdp: false));

        [Test]
        public void OnlineTransportFallsBackToGameConfig()
        {
            var data = Online(serverAddress: "gs.example", useTcpUdp: null);
            Assert.That(data.UsesTcpUdp(_game), Is.EqualTo(!_game.UseWeb));
        }

        [Test]
        public void OnlineDataCopiesArrays()
        {
            var players = new[] { UserId };
            var data = Online(players: players);
            players[0] = OtherPlayerId;
            Assert.That(data.MatchedPlayers, Is.EqualTo(new[] { UserId }));
        }

        #endregion

        #region Single player

        [Test]
        public void EmptySinglePlayerWireDataIsAllDefaults()
        {
            var data = (SinglePlayerLaunchData)ParseData(ElympicsHostLaunchKind.SinglePlayer, "");

            Assert.That(data.MatchId, Is.Null);
            Assert.That(data.UserId, Is.Null);
            Assert.That(data.QueueName, Is.Null);
            Assert.That(data.RegionName, Is.Null);
            Assert.That(data.GameEngineData, Is.Empty);
            Assert.That(data.MatchmakerData, Is.Empty);
            Assert.That(Validate(data).MatchId, Is.EqualTo(Guid.Empty));
        }

        [Test]
        public void SinglePlayerWireDataMapsToLaunchData()
        {
            var json = "{\"matchId\":\"" + MatchId + "\",\"userId\":\"" + UserId + "\",\"queueName\":\"q\",\"regionName\":\"r\",\"gameEngineData\":\"AQI=\",\"matchmakerData\":[1.5]}";
            var data = (SinglePlayerLaunchData)ParseData(ElympicsHostLaunchKind.SinglePlayer, json);

            Assert.That(data.MatchId, Is.EqualTo(MatchId));
            Assert.That(data.UserId, Is.EqualTo(UserId));
            Assert.That(data.QueueName, Is.EqualTo("q"));
            Assert.That(data.RegionName, Is.EqualTo("r"));
            Assert.That(data.GameEngineData, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(data.MatchmakerData, Is.EqualTo(new[] { 1.5f }));
            Assert.That(Validate(data).MatchId, Is.EqualTo(MatchId));
        }

        [TestCase("{\"matchId\":\"x\"}")]
        [TestCase("{\"userId\":\"x\"}")]
        [TestCase("{\"gameEngineData\":\"***\"}")]
        public void BrokenSinglePlayerWireDataIsInvalidPayload(string json) =>
            Assert.That(GetParseDataFailure(ElympicsHostLaunchKind.SinglePlayer, json).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));

        #endregion

        #region Half-remote client

        [Test]
        public void EmptyHalfRemoteClientWireDataIsAllDefaults()
        {
            var data = (HalfRemoteClientLaunchData)ParseData(ElympicsHostLaunchKind.HalfRemoteClient, "");

            Assert.That(data.ServerAddress, Is.Null);
            Assert.That(data.UseTcpUdpInsteadOfWebRtc, Is.Null);
            Assert.That(data.PlayerIndex, Is.Null);
            Assert.That(data.RegionName, Is.Null);
            Assert.That(data.QueueName, Is.Null);
            Assert.That(data.GameEngineData, Is.Null);
            Assert.That(data.MatchmakerData, Is.Null);
            _ = Validate(data);
        }

        [Test]
        public void HalfRemoteClientWireDataMapsToLaunchData()
        {
            var json = "{\"serverAddress\":\"10.0.0.1:9101\",\"transport\":\"webRtc\",\"playerIndex\":2,\"regionName\":\"r\",\"queueName\":\"q\",\"gameEngineData\":\"AQI=\",\"matchmakerData\":[2.5]}";
            var data = (HalfRemoteClientLaunchData)ParseData(ElympicsHostLaunchKind.HalfRemoteClient, json);

            Assert.That(data.ServerAddress, Is.EqualTo("10.0.0.1:9101"));
            Assert.That(data.UseTcpUdpInsteadOfWebRtc, Is.False);
            Assert.That(data.PlayerIndex, Is.EqualTo(2));
            Assert.That(data.RegionName, Is.EqualTo("r"));
            Assert.That(data.QueueName, Is.EqualTo("q"));
            Assert.That(data.GameEngineData, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(data.MatchmakerData, Is.EqualTo(new[] { 2.5f }));
            _ = Validate(data);
        }

        [Test]
        public void HalfRemoteClientPlayerIndexBelowSentinelIsInvalidPayload() =>
            Assert.That(GetParseDataFailure(ElympicsHostLaunchKind.HalfRemoteClient, "{\"playerIndex\":-2}").Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));

        [Test]
        public void HalfRemoteClientBadAddressIsInvalidPayload() =>
            Assert.That(GetValidateFailure(new HalfRemoteClientLaunchData("localhost")).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));

        [Test]
        public void HalfRemoteClientNegativePlayerIndexIsInvalidPayload() =>
            Assert.That(GetValidateFailure(new HalfRemoteClientLaunchData(playerIndex: -1)).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));

        #endregion

        #region Half-remote server

        [TestCase("", null)]
        [TestCase("tcpUdp", ElympicsHalfRemoteProtocols.TcpUdp)]
        [TestCase("webRtc", ElympicsHalfRemoteProtocols.WebRtc)]
        [TestCase("both", ElympicsHalfRemoteProtocols.Both)]
        public void HalfRemoteServerProtocolsWireValuesAreParsed(string protocols, ElympicsHalfRemoteProtocols? expected)
        {
            var data = (HalfRemoteServerLaunchData)ParseData(ElympicsHostLaunchKind.HalfRemoteServer, "{\"protocols\":\"" + protocols + "\"}");
            Assert.That(data.Protocols, Is.EqualTo(expected));
            _ = Validate(data);
        }

        [TestCase("tcp")]
        [TestCase("Both")]
        public void UnknownHalfRemoteServerProtocolsAreInvalidPayload(string protocols) =>
            Assert.That(GetParseDataFailure(ElympicsHostLaunchKind.HalfRemoteServer, "{\"protocols\":\"" + protocols + "\"}").Code,
                Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));

        [Test]
        public void HalfRemoteServerWireSentinelsAreUnset()
        {
            var data = (HalfRemoteServerLaunchData)ParseData(ElympicsHostLaunchKind.HalfRemoteServer, "{}");

            Assert.That(data.Protocols, Is.Null);
            Assert.That(data.ServerHostname, Is.Null);
            Assert.That(data.TcpUdpPort, Is.Null);
            Assert.That(data.SignalingPort, Is.Null);
            Assert.That(data.MaxPlayers, Is.Null);
        }

        [Test]
        public void HalfRemoteServerWireDataMapsToLaunchData()
        {
            var data = (HalfRemoteServerLaunchData)ParseData(ElympicsHostLaunchKind.HalfRemoteServer,
                "{\"serverHostname\":\"0.0.0.0\",\"tcpUdpPort\":9001,\"signalingPort\":9002,\"maxPlayers\":2}");

            Assert.That(data.ServerHostname, Is.EqualTo("0.0.0.0"));
            Assert.That(data.TcpUdpPort, Is.EqualTo(9001));
            Assert.That(data.SignalingPort, Is.EqualTo(9002));
            Assert.That(data.MaxPlayers, Is.EqualTo(2));
            _ = Validate(data);
        }

        private static IEnumerable<TestCaseData> BrokenHalfRemoteServerData()
        {
            yield return Case("no protocols", new HalfRemoteServerLaunchData(default(ElympicsHalfRemoteProtocols)));
            yield return Case("unknown protocol flag", new HalfRemoteServerLaunchData((ElympicsHalfRemoteProtocols)4));
            yield return Case("hostname", new HalfRemoteServerLaunchData(serverHostname: "localhost"));
            yield return Case("tcp port", new HalfRemoteServerLaunchData(tcpUdpPort: 70000));
            yield return Case("signaling port", new HalfRemoteServerLaunchData(signalingPort: 0));
            yield return Case("zero players", new HalfRemoteServerLaunchData(maxPlayers: 0));
            yield return Case("more players than max", new HalfRemoteServerLaunchData(maxPlayers: 5));

            static TestCaseData Case(string name, HalfRemoteServerLaunchData data) => new TestCaseData(data).SetName($"BrokenHalfRemoteServerData({name})");
        }

        [TestCaseSource(nameof(BrokenHalfRemoteServerData))]
        public void BrokenHalfRemoteServerDataIsInvalidPayload(HalfRemoteServerLaunchData data) =>
            Assert.That(GetValidateFailure(data).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));

        [Test]
        public void HalfRemoteServerWhereListeningIsImpossibleIsUnsupportedMode()
        {
            foreach (var platform in new[] { WebGlPlayer, IosPlayer })
            {
                var failure = GetValidateFailure(new HalfRemoteServerLaunchData(), platform: platform);
                Assert.That(failure.Code, Is.EqualTo(ElympicsLaunchErrorCode.UnsupportedMode));
                Assert.That(failure.message, Does.Contain(ElympicsLaunchPlatform.CannotListenMessage));
            }
        }

        [Test]
        public void HalfRemoteClientWhereListeningIsImpossibleIsAccepted() => _ = Validate(new HalfRemoteClientLaunchData(), platform: IosPlayer);

        [Test]
        public void HalfRemoteClientWithoutItInThePlayerIsUnsupportedMode()
        {
            var platform = new ElympicsLaunchPlatform(canListen: false, canUseTcpUdp: false, hasLocalFiles: false, hasHalfRemoteClient: false);
            var failure = GetValidateFailure(new HalfRemoteClientLaunchData(), platform: platform);
            Assert.That(failure.Code, Is.EqualTo(ElympicsLaunchErrorCode.UnsupportedMode));
            Assert.That(failure.message, Does.Contain(ElympicsLaunchPlatform.NoHalfRemoteClientMessage));
        }

        [Test]
        public void HalfRemoteClientOnWebGlWithItInThePlayerIsAccepted() => _ = Validate(new HalfRemoteClientLaunchData(), platform: WebGlPlayer);

        #endregion

        #region Snapshot replay

        [Test]
        public void SnapshotReplayWireDataMapsToPath() =>
            Assert.That(((SnapshotReplayLaunchData)ParseData(ElympicsHostLaunchKind.SnapshotReplay, "{\"path\":\"a/b\"}")).Path, Is.EqualTo("a/b"));

        [Test]
        public void SnapshotReplayWithoutPathIsInvalidPayload() =>
            Assert.That(GetValidateFailure(new SnapshotReplayLaunchData("")).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));

        [Test]
        public void SnapshotReplayByPathWithoutLocalFilesIsUnsupportedMode() =>
            Assert.That(GetValidateFailure(new SnapshotReplayLaunchData("replays/match"), platform: WebGlPlayer).Code, Is.EqualTo(ElympicsLaunchErrorCode.UnsupportedMode));

        [TestCase("https://replays.example/match.snapshots")]
        [TestCase("http://localhost:8080/match")]
        public void SnapshotReplayByUrlIsAcceptedEverywhere(string url) =>
            _ = Validate(new SnapshotReplayLaunchData(url), platform: WebGlPlayer);

        [Test]
        public void SnapshotReplayOfMissingFileIsInvalidPayload()
        {
            var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Assert.That(GetValidateFailure(new SnapshotReplayLaunchData(missing)).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));
            Assert.That(GetValidateFailure(new SnapshotReplayLaunchData(missing + Path.DirectorySeparatorChar)).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));
        }

        #endregion

        [Test]
        public void EveryLaunchKindHasADto() =>
            Assert.That(Enum.GetValues(typeof(ElympicsHostLaunchKind)).Cast<ElympicsHostLaunchKind>().Select(ElympicsLaunchDtos.Create), Has.None.Null);
    }
}
