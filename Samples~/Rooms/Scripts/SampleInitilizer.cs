using System;
using Elympics;
using UnityEngine;

public class SampleInitilizer : MonoBehaviour
{
    [SerializeField] private RoomChoiceController roomChoiceController;
    [SerializeField] private RoomCreationController roomCreationController;
    [SerializeField] private AuthenticateWindowController authenticateWindowController;
    [SerializeField] private RoomsNavigationController roomNavigationController;
    [SerializeField] private RoomController roomController;

    private Action onSignout;
    private Action onSignin;
    private Action onStart;
    private void Awake()
    {
        onSignin += roomController.InitController;
        onSignin += roomChoiceController.ReinitializeRoomRecords;

        onSignout += roomController.DeinitController;
        onSignout += ElympicsLobbyClient.Instance.SignOut;
        onSignout += authenticateWindowController.EnableAuthenticateView;

        roomNavigationController.Init();
        onStart += roomNavigationController.ShowRoomChoiceView;

        authenticateWindowController.Init(onSignin, onSignout, onStart, roomController);
        roomChoiceController.Init(roomController);
        roomCreationController.Init(roomController);
    }
    private void OnDestroy()
    {
        onSignin -= roomController.InitController;
        onSignin -= roomChoiceController.ReinitializeRoomRecords;

        onSignout -= roomController.DeinitController;
        onSignout -= authenticateWindowController.EnableAuthenticateView;
        onSignout -= ElympicsLobbyClient.Instance.SignOut;

        roomCreationController.Deinit();
        roomChoiceController.Deinit();
        authenticateWindowController.Deinit();

        onStart -= roomNavigationController.ShowRoomChoiceView;
        roomNavigationController.Deinit();
    }
}
