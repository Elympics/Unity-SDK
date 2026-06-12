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
            visitor.ProcessOptionalProperty(nameof(Nickname), Nickname);
            visitor.ProcessOptionalProperty(nameof(AuthType), AuthType);
            visitor.ProcessOptionalProperty(nameof(WalletAddress), WalletAddress);
            return true;
        }
    }
}
