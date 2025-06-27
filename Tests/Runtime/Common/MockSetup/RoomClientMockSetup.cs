using System;
using Cysharp.Threading.Tasks;
using NSubstitute;

namespace Elympics
{
    internal static class RoomClientMockSetup
    {
        public static IRoomsClient MockDefaultStartMatchMaking(this IRoomsClient client)
        {
            _ = client.StartMatchmaking(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(UniTask.CompletedTask);
            return client;
        }
    }
}
