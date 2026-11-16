#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Elympics
{
    /// <summary>
    /// <see cref="UserSecret"/> and <see cref="JoinToken"/> are per-match game-server credentials, unrelated to any
    /// account-level Elympics client secret or session token. At least one of them is required.
    /// </summary>
    [PublicAPI]
    public sealed class OnlineLaunchData : LaunchData
    {
        /// <summary>
        /// With WebRTC, the web signaling address (absolute, or relative to the game servers endpoint of <see cref="ElympicsConfig"/>).
        /// With TCP/UDP, <c>host:port</c>.
        /// </summary>
        public string ServerAddress { get; }
        /// <summary>Null means <see cref="ElympicsGameConfig.UseWeb"/> decides.</summary>
        public bool? UseTcpUdpInsteadOfWebRtc { get; }
        public Guid MatchId { get; }
        public string UserSecret { get; }
        /// <summary>
        /// Signed join token (JWT) for this match and player. When set, it is presented to the game server instead of
        /// <see cref="UserSecret"/>. Empty means none.
        /// </summary>
        public string JoinToken { get; }
        /// <summary>The local player. Must be one of <see cref="MatchedPlayers"/>.</summary>
        public Guid UserId { get; }
        /// <summary>Their order defines <see cref="ElympicsPlayer"/> indices.</summary>
        public IReadOnlyList<Guid> MatchedPlayers { get; }
        public string? RegionName { get; }
        public IReadOnlyList<byte> GameEngineData { get; }
        public IReadOnlyList<float> MatchmakerData { get; }
        /// <summary>
        /// Per-match ICE (TURN/STUN) servers minted by the backend. Set from the launch JSON (<c>iceServers</c>) or by PlayPad.
        /// When null or empty, the client asks the game server for them, as before.
        /// </summary>
        internal IReadOnlyList<IceServer>? IceServers { get; init; }

        public OnlineLaunchData(string serverAddress, bool? useTcpUdpInsteadOfWebRtc, Guid matchId, string userSecret, Guid userId,
            IEnumerable<Guid> matchedPlayers, string? regionName = null, IEnumerable<byte>? gameEngineData = null, IEnumerable<float>? matchmakerData = null,
            string? joinToken = null)
        {
            ServerAddress = serverAddress ?? string.Empty;
            UseTcpUdpInsteadOfWebRtc = useTcpUdpInsteadOfWebRtc;
            MatchId = matchId;
            UserSecret = userSecret ?? string.Empty;
            JoinToken = joinToken ?? string.Empty;
            UserId = userId;
            MatchedPlayers = Copy(matchedPlayers);
            RegionName = regionName;
            GameEngineData = Copy(gameEngineData);
            MatchmakerData = Copy(matchmakerData);
        }

        internal override string ModeName => "Online";
        internal override Guid EventMatchId => MatchId;
        internal override Guid? EventUserId => UserId;

        internal bool UsesTcpUdp(ElympicsGameConfig gameConfig) => UseTcpUdpInsteadOfWebRtc ?? !gameConfig.UseWeb;

        internal override bool TryValidate(ElympicsGameConfig gameConfig, ElympicsLaunchPlatform platform, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            if (string.IsNullOrEmpty(ServerAddress))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(ServerAddress)} is required.", out failure);
            if (UsesTcpUdp(gameConfig) && !platform.CanUseTcpUdp)
                return Fail(ElympicsLaunchErrorCode.UnsupportedMode, ElympicsLaunchPlatform.NoTcpUdpMessage, out failure);
            if (UsesTcpUdp(gameConfig) && !LaunchDataParsing.TryParseEndPoint(ServerAddress, out _))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(ServerAddress)} must be ip:port with TCP/UDP, got \"{ServerAddress}\".", out failure);
            if (string.IsNullOrEmpty(UserSecret) && string.IsNullOrEmpty(JoinToken))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(UserSecret)} or {nameof(JoinToken)} is required.", out failure);
            if (MatchedPlayers.Count == 0)
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(MatchedPlayers)} is required.", out failure);
            if (MatchedPlayers.Distinct().Count() != MatchedPlayers.Count)
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(MatchedPlayers)} must not contain duplicates.", out failure);
            if (!MatchedPlayers.Contains(UserId))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(UserId)} {UserId} is not one of {nameof(MatchedPlayers)}.", out failure);
            if (MatchedPlayers.Count > gameConfig.MaxPlayers)
                return Fail(ElympicsLaunchErrorCode.InvalidPayload,
                    $"{MatchedPlayers.Count} matched players exceed {nameof(ElympicsGameConfig.MaxPlayers)} ({gameConfig.MaxPlayers}) of {gameConfig.GameName}.", out failure);
            return true;
        }

        private protected override GameSceneInitializer CreateInitializer(GameSceneLaunchContext context) => new OnlineGameClientInitializer(this);
    }
}
