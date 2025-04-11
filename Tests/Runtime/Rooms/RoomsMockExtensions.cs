using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NSubstitute.Core;

#nullable enable

namespace Elympics.Tests.Rooms
{
    internal static class RoomsMockExtensions
    {
        private static readonly Action<CallInfo> NoOp = _ =>
        {
            var x = 5;
        };

        public static void ReturnsForJoinOrCreate(this IRoomsClient client, Func<UniTask<Guid>> resultFactory, Action<CallInfo>? andDoes = null)
        {
            _ = client.CreateRoom("", false, false, "", false, new Dictionary<string, string>(), new Dictionary<string, string>())
                .ReturnsForAnyArgs(resultFactory())
                .AndDoes(andDoes ?? NoOp);
            _ = client.JoinRoom(Arg.Any<Guid>(), null)
                .ReturnsForAnyArgs(resultFactory())
                .AndDoes(andDoes ?? NoOp);
            _ = client.JoinRoom(Arg.Any<string>(), null)
                .ReturnsForAnyArgs(resultFactory())
                .AndDoes(andDoes ?? NoOp);
        }
    }
}
