using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using Elympics.Models.Authentication;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button rejoinButton;
    [SerializeField] private Button changeRegionButton;
    [SerializeField] private InputField halfRemotePlayerId;
    [SerializeField] private string regionToJoin;

    private const string PlayOnlineText = "Play Online";
    private const string RejoinOnlineText = "Rejoin Online";

    private IRoomsManager _roomsManager;
    private Text _playButtonText;
    private Text _rejoinButtonText;
    private CancellationTokenSource _cts;
    private string _closestRegion;

    private async void Start()
    {
        _roomsManager = ElympicsLobbyClient.Instance!.RoomsManager;

        await ElympicsLobbyClient.Instance!.ConnectToElympicsAsync(new ConnectionData()
        {
            AuthType = AuthType.ClientSecret,
            Region = new RegionData()
            {
                Name = "warsaw",
            }
        });
        playButton.interactable = true;
        rejoinButton.interactable = true;
        changeRegionButton.interactable = true;
        ElympicsLobbyClient.Instance.RoomsManager.JoinedRoom += OnRoomJoined;

        if (!ElympicsClonesManager.IsClone())
            return;
        halfRemotePlayerId.text = ElympicsGameConfig.GetHalfRemotePlayerIndex(0).ToString();
        halfRemotePlayerId.placeholder.GetComponent<Text>().enabled = true;


    }
    private void OnRoomJoined(JoinedRoomArgs obj)
    {
        Debug.Log("Joined room.");
        var room = _roomsManager.ListJoinedRooms().Where(x => x.RoomId == obj.RoomId).ToList()[0];
        if (room.IsMatchAvailable)
        {
            room.PlayAvailableMatch();
        }
    }

    private void ResetState()
    {
        _cts?.Cancel();
        _playButtonText.text = PlayOnlineText;
        _rejoinButtonText.text = RejoinOnlineText;
        _cts = null;
    }

    public void OnPlayLocalClicked() => ElympicsLobbyClient.Instance.PlayOffline();

    public void OnPlayHalfRemoteClicked()
    {
        var playerId = int.Parse(halfRemotePlayerId.text);
        ElympicsLobbyClient.Instance.PlayHalfRemote(playerId);
    }

    public void OnStartHalfRemoteServer() => ElympicsLobbyClient.Instance.StartHalfRemoteServer();

    public void OnPlayOnlineClicked()
    {
        if (_cts != null)
        {
            ResetState();
            return;
        }
        _cts = new CancellationTokenSource();
        ElympicsLobbyClient.Instance!.RoomsManager.StartQuickMatch("Default", null, null, _cts!.Token).Forget();
    }

    public void OnRejoinOnlineClicked()
    {
        var rooms = _roomsManager.ListJoinedRooms();
        if (rooms.Count > 0
            && rooms[0].IsMatchAvailable)
        {
            rooms[0].PlayAvailableMatch();
        }
    }

    public void OnChangeRegionClicked()
    {
        ElympicsLobbyClient.Instance!.ConnectToElympicsAsync(new ConnectionData()
        {
            Region = new RegionData()
            {
                Name = regionToJoin,
            }
        }).Forget();
    }
}
