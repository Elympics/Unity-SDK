#nullable enable

using System;

namespace Elympics.Core.Logger.State
{
    internal class SdkState : IVisitableState
    {
        public readonly string SessionId;
        public readonly string SdkVersion;
        public string? ApiUrl;
        public string? GameServerUrl;
        public string? Region;

        public SdkState(string sdkVersion)
        {
            SessionId = Guid.NewGuid().ToString();
            SdkVersion = sdkVersion;
        }

        public bool Visit(IStateVisitor visitor)
        {
            visitor.ProcessProperty(nameof(SessionId), SessionId);
            visitor.ProcessProperty(nameof(SdkVersion), SdkVersion);
            visitor.ProcessOptionalProperty(nameof(ApiUrl), ApiUrl);
            visitor.ProcessOptionalProperty(nameof(GameServerUrl), GameServerUrl);
            visitor.ProcessOptionalProperty(nameof(Region), Region);
            return true;
        }
    }
}
