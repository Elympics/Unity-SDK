using System;
using System.Collections.Generic;
using Elympics.Communication.Authentication.Models;
using Elympics.Communication.Authentication.Models.Internal;
using Elympics.Communication.Rooms.InternalModels;
using Elympics.Communication.Rooms.InternalModels.FromRooms;
using Elympics.Rooms.Models;
using NSubstitute;

namespace Elympics
{
    internal static class RoomsManagerMockSetup
    {
        public static IRoomsManager SetCurrentDefaultRoom(this IRoomsManager roomsManager, IRoomsClient roomClient, IMatchLauncher roomMatchClient)
        {
            IRoom room = new Room(roomMatchClient,
                roomClient,
                Guid.Empty,
                new RoomStateChangedDto(Guid.Empty,
                    DateTime.Now,
                    string.Empty,
                    null,
                    false,
                    new MatchmakingData(DateTime.Now,
                        MatchmakingStateDto.Playing,
                        "test",
                        1,
                        1,
                        new Dictionary<string, string>(),
                        new MatchDataDto(Guid.Empty, MatchStateDto.Running, new MatchDetailsDto(new List<Guid>(), null, null, null, null, null), null),
                        null,
                        null),
                    new List<UserInfoDto> { new(0, true, new Dictionary<string, string>(), new ElympicsUserDTO(Guid.Empty.ToString(), "", (int)NicknameStatus.NotVerified, "")) },
                    false,
                    false,
                    null));
            room.IsJoined = true;
            _ = roomsManager.CurrentRoom.Returns(room);
            return roomsManager;
        }
    }
}
