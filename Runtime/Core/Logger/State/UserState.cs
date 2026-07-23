#nullable enable

namespace Elympics.Core.Logger.State
{
    internal sealed class UserState : IVisitableState
    {
        public string? UserId;
        public string? Nickname;
        public string? AuthType;
        public string? WalletAddress;

        private static class Names
        {
            public const string UserId = "userId";
            public const string Nickname = "nickname";
            public const string NicknameLegacy = "nickName";
            public const string AuthType = "authType";
            public const string WalletAddress = "walletAddress";
        }

        public bool Visit(IStateVisitor visitor)
        {
            var visited = false;
            visited |= visitor.ProcessOptionalProperty(Names.UserId, UserId);
            visited |= visitor.ProcessOptionalProperty(Names.Nickname, Nickname, legacyName: Names.NicknameLegacy);
            visited |= visitor.ProcessOptionalProperty(Names.AuthType, AuthType);
            visited |= visitor.ProcessOptionalProperty(Names.WalletAddress, WalletAddress);
            return visited;
        }
    }
}
