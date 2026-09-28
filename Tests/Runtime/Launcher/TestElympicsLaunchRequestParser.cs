#nullable enable

using System;
using System.Collections.Generic;
using Elympics.ElympicsSystems.Internal;
using Elympics.Models.Matchmaking;
using Elympics.SnapshotAnalysis.Retrievers;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Elympics.Tests
{
    [Category("Launcher")]
    public class TestElympicsLaunchRequestParser
    {
        private const string GameId = "a1b2c3d4-0000-0000-0000-000000000001";
        private const string UserId = "a1b2c3d4-0000-0000-0000-00000000000a";
        private const string OtherPlayerId = "a1b2c3d4-0000-0000-0000-00000000000b";
        private const string MatchId = "a1b2c3d4-0000-0000-0000-0000000000ff";

        private ElympicsGameConfig _game = null!;
        private List<ElympicsGameConfig?> _games = null!;

        [SetUp]
        public void SetUp()
        {
            _game = ScriptableObject.CreateInstance<ElympicsGameConfig>();
            _game.gameId = GameId;
            _game.gameplayScene = "Assets/Scenes/Gameplay.unity";
            _games = new List<ElympicsGameConfig?> { null, _game };
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_game);

        private static ElympicsLaunchRequest OnlineRequest() => new()
        {
            gameId = GameId,
            matchMode = "Online",
            userId = UserId,
            matchId = MatchId,
            userSecret = "secret",
            queueName = "queue",
            regionName = "warsaw",
            webServerAddress = "gs.example",
            tcpUdpServerAddress = "127.0.0.1:9000",
            gameEngineData = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            matchmakerData = new[] { 0.5f },
            matchedPlayers = new[] { OtherPlayerId, UserId },
        };

        private ElympicsLaunch Parse(ElympicsLaunchRequest request, ElympicsLaunchSource source = ElympicsLaunchSource.InProcess, bool canListen = true)
        {
            var ok = ElympicsLaunchRequestParser.TryParse(request, _games, source, canListen, out var launch, out var failure);
            Assert.That(ok, Is.True, failure?.ToString());
            Assert.That(failure, Is.Null);
            return launch!;
        }

        private ElympicsLaunchFailure ParseFailure(ElympicsLaunchRequest? request, ElympicsLaunchSource source = ElympicsLaunchSource.InProcess, bool canListen = true)
        {
            var ok = ElympicsLaunchRequestParser.TryParse(request, _games, source, canListen, out var launch, out var failure);
            Assert.That(ok, Is.False);
            Assert.That(launch, Is.Null);
            Assert.That(failure, Is.Not.Null);
            return failure!;
        }

        [Test]
        public void OnlineRequestMapsToMatchData()
        {
            var launch = Parse(OnlineRequest());

            Assert.That(launch.GameConfig, Is.SameAs(_game));
            Assert.That(launch.MatchMode, Is.EqualTo(JoinedMatchMode.Online));
            Assert.That(launch.UserId, Is.EqualTo(Guid.Parse(UserId)));
            var data = launch.MatchData!;
            Assert.That(data.MatchId, Is.EqualTo(Guid.Parse(MatchId)));
            Assert.That(data.UserSecret, Is.EqualTo("secret"));
            Assert.That(data.QueueName, Is.EqualTo("queue"));
            Assert.That(data.RegionName, Is.EqualTo("warsaw"));
            Assert.That(data.WebServerAddress, Is.EqualTo("gs.example"));
            Assert.That(data.TcpUdpServerAddress, Is.EqualTo("127.0.0.1:9000"));
            Assert.That(data.GameEngineData, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(data.MatchmakerData, Is.EqualTo(new[] { 0.5f }));
            Assert.That(data.MatchedPlayers, Is.EqualTo(new[] { Guid.Parse(OtherPlayerId), Guid.Parse(UserId) }));
        }

        [Test]
        public void MatchDataRoundTripsThroughRequest()
        {
            var original = new MatchmakingFinishedData(Guid.Parse(MatchId), "secret", "queue", "region", new byte[] { 9, 8 }, new[] { 1f, 2f },
                "1.2.3.4:5", "web", new[] { Guid.Parse(UserId), Guid.Parse(OtherPlayerId) });

            var request = ElympicsLaunchRequest.FromMatchData(GameId, JoinedMatchMode.Online, Guid.Parse(UserId), original);
            var launch = Parse(request);

            Assert.That(launch.MatchData, Is.EqualTo(original));
        }

        [Test]
        public void RequestRoundTripsThroughJson()
        {
            var json = JsonUtility.ToJson(OnlineRequest());
            var launch = Parse(JsonUtility.FromJson<ElympicsLaunchRequest>(json));

            Assert.That(launch.MatchData!.GameEngineData, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(launch.MatchData.MatchedPlayers, Has.Length.EqualTo(2));
        }

        [Test]
        public void MissingProtocolVersionInJsonDefaultsToCurrent()
        {
            var request = JsonUtility.FromJson<ElympicsLaunchRequest>($"{{\"gameId\":\"{GameId}\",\"matchMode\":\"SinglePlayer\"}}");
            Assert.That(request.protocolVersion, Is.EqualTo(ElympicsHostProtocol.Version));
            _ = Parse(request);
        }

        [Test]
        public void EmptyGameEngineDataGivesEmptyArray()
        {
            var request = OnlineRequest();
            request.gameEngineData = "";
            Assert.That(Parse(request).MatchData!.GameEngineData, Is.Empty);
        }

        [Test]
        public void RequestIdIsKeptOrGenerated()
        {
            var request = OnlineRequest();
            request.requestId = "abc";
            Assert.That(Parse(request).RequestId, Is.EqualTo("abc"));
            request.requestId = "";
            Assert.That(Parse(request).RequestId, Is.Not.Empty);
        }

        [Test]
        public void TimeoutsDefaultAndOptOut()
        {
            var request = OnlineRequest();
            var launch = Parse(request);
            Assert.That(launch.LaunchTimeout, Is.EqualTo(TimeSpan.FromSeconds(ElympicsLaunchDefaults.LaunchTimeoutSeconds)));
            Assert.That(launch.PostMatchTimeout, Is.EqualTo(TimeSpan.FromSeconds(ElympicsLaunchDefaults.PostMatchTimeoutSeconds)));

            request.launchTimeoutSeconds = 5;
            request.postMatchTimeoutSeconds = -1;
            launch = Parse(request);
            Assert.That(launch.LaunchTimeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(launch.PostMatchTimeout, Is.Null);
        }

        [Test]
        public void SinglePlayerAcceptsNoMatchData()
        {
            var launch = Parse(new ElympicsLaunchRequest { gameId = GameId, matchMode = "SinglePlayer" });

            Assert.That(launch.MatchData!.MatchId, Is.EqualTo(Guid.Empty));
            Assert.That(launch.MatchData.MatchedPlayers, Is.EqualTo(new[] { Guid.Empty }));
        }

        [Test]
        public void SinglePlayerUsesUserIdAsOnlyPlayer()
        {
            var launch = Parse(new ElympicsLaunchRequest { gameId = GameId, matchMode = "SinglePlayer", userId = UserId, queueName = "q" });

            Assert.That(launch.MatchData!.MatchedPlayers, Is.EqualTo(new[] { Guid.Parse(UserId) }));
            Assert.That(launch.MatchData.QueueName, Is.EqualTo("q"));
        }

        [TestCase("HalfRemoteClient", JoinedMatchMode.HalfRemoteClient)]
        [TestCase("HalfRemoteServer", JoinedMatchMode.HalfRemoteServer)]
        public void ConfigDrivenModesAcceptBareRequest(string mode, JoinedMatchMode expected)
        {
            var launch = Parse(new ElympicsLaunchRequest { gameId = GameId, matchMode = mode });
            Assert.That(launch.MatchMode, Is.EqualTo(expected));
            Assert.That(launch.MatchData, Is.Null);
        }

        [Test]
        public void SnapshotReplayInProcessCarriesRetriever()
        {
            var retriever = Substitute.For<SnapshotAnalysisRetriever>();
            var launch = Parse(new ElympicsLaunchRequest { gameId = GameId, matchMode = "SnapshotReplay", SnapshotRetriever = retriever });
            Assert.That(launch.SnapshotRetriever, Is.SameAs(retriever));
        }

        // --- rejections ---

        [Test]
        public void NullRequestIsInvalidPayload() =>
            Assert.That(ParseFailure(null).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));

        [TestCase("")]
        [TestCase("not-a-guid")]
        public void BadGameIdIsInvalidPayload(string gameId)
        {
            var request = OnlineRequest();
            request.gameId = gameId;
            Assert.That(ParseFailure(request).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));
        }

        [TestCase("2.0.0")]
        [TestCase("0.9")]
        [TestCase("x")]
        public void IncompatibleProtocolIsProtocolMismatch(string version)
        {
            var request = OnlineRequest();
            request.protocolVersion = version;
            Assert.That(ParseFailure(request).Code, Is.EqualTo(ElympicsLaunchErrorCode.ProtocolMismatch));
        }

        [Test]
        public void CompatibleMinorVersionIsAccepted()
        {
            var request = OnlineRequest();
            request.protocolVersion = "1.7.3";
            _ = Parse(request);
        }

        [Test]
        public void EmptyProtocolIsInvalidPayload()
        {
            var request = OnlineRequest();
            request.protocolVersion = "";
            Assert.That(ParseFailure(request).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));
        }

        [Test]
        public void UnknownGameIdIsUnknownGame()
        {
            var request = OnlineRequest();
            request.gameId = Guid.NewGuid().ToString();
            Assert.That(ParseFailure(request).Code, Is.EqualTo(ElympicsLaunchErrorCode.UnknownGame));
        }

        [Test]
        public void GameWithoutSceneIsUnknownGame()
        {
            _game.gameplayScene = "";
            Assert.That(ParseFailure(OnlineRequest()).Code, Is.EqualTo(ElympicsLaunchErrorCode.UnknownGame));
        }

        [TestCase("")]
        [TestCase("online")]
        [TestCase("Local")]
        [TestCase("1")]
        [TestCase("Bogus")]
        public void UnparseableModeIsUnsupportedMode(string mode)
        {
            var request = OnlineRequest();
            request.matchMode = mode;
            Assert.That(ParseFailure(request).Code, Is.EqualTo(ElympicsLaunchErrorCode.UnsupportedMode));
        }

        [Test]
        public void SnapshotReplayOverWireIsUnsupportedMode() =>
            Assert.That(ParseFailure(new ElympicsLaunchRequest { gameId = GameId, matchMode = "SnapshotReplay" }, ElympicsLaunchSource.Wire).Code,
                Is.EqualTo(ElympicsLaunchErrorCode.UnsupportedMode));

        [Test]
        public void SnapshotReplayWithoutRetrieverIsInvalidPayload() =>
            Assert.That(ParseFailure(new ElympicsLaunchRequest { gameId = GameId, matchMode = "SnapshotReplay" }).Code,
                Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));

        [Test]
        public void HalfRemoteServerWhereListeningIsImpossibleIsUnsupportedMode()
        {
            var failure = ParseFailure(new ElympicsLaunchRequest { gameId = GameId, matchMode = "HalfRemoteServer" }, canListen: false);
            Assert.That(failure.Code, Is.EqualTo(ElympicsLaunchErrorCode.UnsupportedMode));
            Assert.That(failure.message, Does.Contain(ElympicsLaunchRequestParser.CannotListenMessage));
        }

        [TestCase("HalfRemoteClient")]
        [TestCase("HalfRemoteServer")]
        public void ConfigDrivenModeWithMatchFieldsIsInvalidPayload(string mode)
        {
            var request = OnlineRequest();
            request.matchMode = mode;
            var failure = ParseFailure(request);
            Assert.That(failure.Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));
            Assert.That(failure.message, Does.Contain("matchedPlayers"));
        }

        private static IEnumerable<TestCaseData> BrokenOnlineRequests()
        {
            yield return Case("userId missing", r => r.userId = "");
            yield return Case("userId not a guid", r => r.userId = "nope");
            yield return Case("matchId not a guid", r => r.matchId = "nope");
            yield return Case("userSecret missing", r => r.userSecret = "");
            yield return Case("no server address", r =>
            {
                r.webServerAddress = "";
                r.tcpUdpServerAddress = "";
            });
            yield return Case("no matched players", r => r.matchedPlayers = Array.Empty<string>());
            yield return Case("matched player not a guid", r => r.matchedPlayers = new[] { UserId, "nope" });
            yield return Case("duplicate matched players", r => r.matchedPlayers = new[] { UserId, UserId });
            yield return Case("user not matched", r => r.matchedPlayers = new[] { OtherPlayerId });
            yield return Case("bad base64", r => r.gameEngineData = "***");
            yield return Case("negative launch timeout", r => r.launchTimeoutSeconds = -1);
            yield return Case("post-match timeout below -1", r => r.postMatchTimeoutSeconds = -2);

            static TestCaseData Case(string name, Action<ElympicsLaunchRequest> breakIt) => new TestCaseData(breakIt).SetName($"BrokenOnlineRequest({name})");
        }

        [TestCaseSource(nameof(BrokenOnlineRequests))]
        public void BrokenOnlineRequestIsInvalidPayload(Action<ElympicsLaunchRequest> breakIt)
        {
            var request = OnlineRequest();
            breakIt(request);
            Assert.That(ParseFailure(request).Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));
        }

        [TestCase("1.0.0", true)]
        [TestCase("1.2", true)]
        [TestCase("1", true)]
        [TestCase("2.0.0", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void ProtocolCompatibilityComparesMajor(string? version, bool expected) =>
            Assert.That(ElympicsHostProtocol.IsCompatible(version), Is.EqualTo(expected));

        [Test]
        public void FailureCodeIsSerializedByName()
        {
            var failure = new ElympicsLaunchFailure(ElympicsLaunchErrorCode.Busy, "busy");
            Assert.That(JsonUtility.ToJson(failure), Does.Contain("\"code\":\"Busy\""));
            Assert.That(JsonUtility.FromJson<ElympicsLaunchFailure>(JsonUtility.ToJson(failure)).Code, Is.EqualTo(ElympicsLaunchErrorCode.Busy));
        }
    }
}
