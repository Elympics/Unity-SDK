#nullable enable

using System;

namespace Elympics.Core.Logger.State
{
    internal class SdkState : IVisitableState
    {
        public string SessionId;
        public string SdkVersion;
        public string? ApiUrl;
        public string? GameServerUrl;

        public SdkState(string sdkVersion)
        {
            SessionId = Guid.NewGuid().ToString();
            SdkVersion = sdkVersion;
        }

        public void Visit(IStateVisitor visitor)
        {
            visitor.ProcessProperty(nameof(SessionId), SessionId);
            visitor.ProcessProperty(nameof(SdkVersion), SdkVersion);
            if (!string.IsNullOrEmpty(ApiUrl))
                visitor.ProcessProperty(nameof(ApiUrl), ApiUrl);
            if (!string.IsNullOrEmpty(GameServerUrl))
                visitor.ProcessProperty(nameof(GameServerUrl), GameServerUrl);
        }
    }
}
