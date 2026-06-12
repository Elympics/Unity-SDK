#nullable enable

namespace Elympics.Core.Logger.State
{
    internal sealed class UserState : IVisitableState
    {
        public readonly string UserId;
        public string? Nickname;
        public string? AuthType;
        public string? WalletAddress;

        public UserState(string userId) => UserId = userId;

        public bool Visit(IStateVisitor visitor)
        {
            visitor.ProcessProperty(nameof(UserId), UserId);
            if (!string.IsNullOrEmpty(Nickname))
                visitor.ProcessProperty(nameof(Nickname), Nickname);
            if (!string.IsNullOrEmpty(AuthType))
                visitor.ProcessProperty(nameof(AuthType), AuthType);
            if (!string.IsNullOrEmpty(WalletAddress))
                visitor.ProcessProperty(nameof(WalletAddress), WalletAddress);
            return true;
        }
    }
}
