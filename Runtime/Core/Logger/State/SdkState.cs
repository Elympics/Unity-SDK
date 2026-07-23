#nullable enable

using System;

namespace Elympics.Core.Logger.State
{
    internal class SdkState : IVisitableState
    {
        internal string SessionId { get; }

        private readonly string _sdkVersion;
        public string? ApiUrl;
        public string? GameServerUrl;
        public string? Region;

        private static class Names
        {
            public const string SessionId = "sessionId";
            public const string SdkVersion = "sdkVersion";
            public const string ApiUrl = "apiUrl";
            public const string ApiUrlLegacy = "lobbyUrl";
            public const string GameServerUrl = "gameServerUrl";
            public const string Region = "region";
        }

        public SdkState(string sdkVersion)
        {
            SessionId = Guid.NewGuid().ToString();
            _sdkVersion = sdkVersion;
        }

        public bool Visit(IStateVisitor visitor)
        {
            visitor.ProcessProperty(Names.SessionId, SessionId);
            visitor.ProcessProperty(Names.SdkVersion, _sdkVersion);
            _ = visitor.ProcessOptionalProperty(Names.ApiUrl, ApiUrl, legacyName: Names.ApiUrlLegacy);
            _ = visitor.ProcessOptionalProperty(Names.GameServerUrl, GameServerUrl);
            _ = visitor.ProcessOptionalProperty(Names.Region, Region);
            return true;
        }
    }
}
