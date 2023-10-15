using System;
using UnityEditor;
using UnityEngine;
using static Elympics.RoomMockController;

namespace Elympics.Editor.Tests
{
    [CustomEditor(typeof(RoomMockController))]
    public class RoomMockControllerEditorWindow : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            _ = DrawDefaultInspector();
            if (GUILayout.Button("Make players ready in current room"))
            {
                var mock = (RoomMockController)target;
                if (mock.RoomManager.ListJoinedRooms().Count <= 0)
                    return;

                var currentRoomId = mock.RoomManager.ListJoinedRooms()[0].RoomId;
                WebSocketMockSetup.MakeAllPlayersReadyForRoom(currentRoomId);
            }

            if (GUILayout.Button("Simulate Room Parameters Change"))
            {
                var mock = (RoomMockController)target;

                var roomManager = mock.RoomManager;

                var roomId = roomManager.ListJoinedRooms().Count > 0 ? roomManager.ListJoinedRooms()[0].RoomId : new Guid(mock.modifiedRoomId);

                WebSocketMockSetup.SimulateRoomParametersChange(roomId, mock.newRoomName, mock.newIsPrivate,
                    new(StringStringPair.ToDictionary(mock.newCustomRoomData)),
                    new(StringStringPair.ToDictionary(mock.newCustomMatchmakingData))
                    );
            }
        }
    }
}
