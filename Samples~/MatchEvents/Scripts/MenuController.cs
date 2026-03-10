using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using Elympics.Models.Authentication;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEngine;
using UnityEngine.UI;

namespace MatchEvents
{
    public class MenuController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button rejoinButton;
        [SerializeField] private Button changeRegionButton;
        [SerializeField] private InputField halfRemotePlayerId;
        [SerializeField] private string regionToJoin = ElympicsRegions.Warsaw;

        private const string PlayOnlineText = "Play Online";
        private const string CancelMatchmakingText = "Cancel matchmaking";

        private IRoomsManager _roomsManager;
        private Text _playButtonText;
        private CancellationTokenSource _cts;
        private string _closestRegion;

        private async void Start()
        {
            try
            {
                _roomsManager = ElympicsLobbyClient.Instance!.RoomsManager;
                _playButtonText = playButton.GetComponentInChildren<Text>();

                if (!ElympicsLobbyClient.Instance.IsAuthenticated)
                    await ElympicsLobbyClient.Instance!.ConnectToElympicsAsync(new ConnectionData
                    {
                        AuthType = AuthType.ClientSecret,
                        Region = new RegionData
                        {
                            Name = await GetRegionToJoin(),
                        }
                    });
                playButton.interactable = true;
                rejoinButton.interactable = true;
                changeRegionButton.interactable = true;
                ElympicsLobbyClient.Instance.RoomsManager.JoinedRoom += OnRoomJoined;
                ResetState();

                if (!ElympicsClonesManager.IsClone())
                    return;
                halfRemotePlayerId.text = ElympicsGameConfig.GetHalfRemotePlayerIndex(0).ToString();
                halfRemotePlayerId.placeholder.GetComponent<Text>().enabled = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void ResetState()
        {
            _cts?.Cancel();
            _playButtonText.text = PlayOnlineText;
            _cts = null;
        }

        public void OnPlayLocalClicked()
        {
            ElympicsLobbyClient.Instance!.PlayOffline();
        }

        public void OnPlayHalfRemoteClicked()
        {
            var playerId = int.Parse(halfRemotePlayerId.text);
            ElympicsLobbyClient.Instance!.PlayHalfRemote(playerId);
        }

        public void OnStartHalfRemoteServer()
        {
            ElympicsLobbyClient.Instance!.StartHalfRemoteServer();
        }

        public async void OnPlayOnlineClicked()
        {
            try
            {
                if (_cts != null)
                {
                    ResetState();
                    return;
                }

                _cts = new CancellationTokenSource();
                _playButtonText.text = CancelMatchmakingText;
                await ElympicsLobbyClient.Instance!.RoomsManager.StartQuickMatch("Default", null, null, null, null,
                    null, null, _cts!.Token);
            }
            catch (OperationCanceledException)
            { }
            catch (Exception e)
            {
                Debug.LogException(e);
                ResetState();
            }
        }

        public void OnRejoinOnlineClicked()
        {
            var room = _roomsManager.CurrentRoom;
            if (room is { IsMatchAvailable: true })
                room.PlayAvailableMatch();
        }

        private void OnRoomJoined(JoinedRoomArgs obj)
        {
            Debug.Log("Joined room.");
            var room = _roomsManager.CurrentRoom!;
            if (room.IsMatchAvailable)
                room.PlayAvailableMatch();
        }

        private async UniTask ChooseClosestRegion()
        {
            var availableRegions = await ElympicsRegions.GetAvailableRegions();
            _closestRegion = (await ElympicsCloudPing.ChooseClosestRegion(availableRegions)).Region;
            if (string.IsNullOrEmpty(_closestRegion))
                _closestRegion = ElympicsRegions.Warsaw;
            Debug.Log($"Selected region: {_closestRegion}");
        }

        private async UniTask<string> GetRegionToJoin()
        {
            var region = regionToJoin;
            if (!string.IsNullOrEmpty(region))
                return region;

            Debug.Log($"{nameof(regionToJoin)} serialized field empty, checking for the closest region...");
            await ChooseClosestRegion();
            return _closestRegion;
        }

        public async void OnChangeRegionClicked()
        {
            try
            {
                await ElympicsLobbyClient.Instance!.ConnectToElympicsAsync(new ConnectionData
                {
                    Region = new RegionData
                    {
                        Name = await GetRegionToJoin(),
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
