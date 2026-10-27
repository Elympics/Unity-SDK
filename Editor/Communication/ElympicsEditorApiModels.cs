using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elympics.Editor.Communication
{
    // Response models keep fields they don't declare in ExtensionData, so the CLI still reports new backend fields.

    internal class FleetModel
    {
        public string Id { get; set; }
        public string Name { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    internal class GameModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FleetId { get; set; }
        public string ExternalBackend { get; set; }
        public string EventQueueJoinUrl { get; set; }
        public string EventQueueLeaveUrl { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    internal class CreateGameRequestModel
    {
        public string GameName { get; set; }
        public string InitialRegionName { get; set; }
        public string FleetId { get; set; }
    }

    internal class UpdateGameRequestModel
    {
        public string GameName { get; set; }
        public string FleetId { get; set; }
        public string ExternalBackend { get; set; }
        public string EventQueueJoinUrl { get; set; }
        public string EventQueueLeaveUrl { get; set; }
    }

    /// <summary>A null flag stays unchanged.</summary>
    internal class UpdateGameVersionRequestModel
    {
        public bool? Blocked { get; set; }
        public bool? DebugMode { get; set; }
        public bool? DebugModeWithBots { get; set; }
    }

    /// <summary>The body of the create and update queue requests.</summary>
    internal class QueueSettingsModel
    {
        public string Name { get; set; }
        public string MatchType { get; set; }
        public string MatchmakingStrategy { get; set; }
        public string SamplingStrategy { get; set; }
        public string PredictionStrategy { get; set; }
        public string MatchmakingMode { get; set; }
        public int? NumberOfTeams { get; set; }
        public int? PlayersInTeam { get; set; }
        public bool? MatchWithBots { get; set; }
        public int? MaxWaitTime { get; set; }
        public int? MinPlayersToMatch { get; set; }
        public string SmartContractDeploymentId { get; set; }
    }

    internal class QueueModel : QueueSettingsModel
    {
        public string Id { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; }
    }
}
