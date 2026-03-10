using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Elympics.Models.Authentication;
using UnityEngine;
using UnityEngine.UI;

namespace Elympics.EgbTest
{
    public class GameStarter : MonoBehaviour
    {
        private const string EgbGameplayGameName = "SampleEgbTest";

        [SerializeField] private InputField sentGeDataField;
        [SerializeField] private InputField sentMmDataField;
        [SerializeField] private Button playOnlineButton;

        public static byte[] SentGeData { get; private set; }
        public static float[] SentMmData { get; private set; }

        private IRoomsManager _roomsManager;

        private async void Start()
        {
            try
            {
                _roomsManager = ElympicsLobbyClient.Instance!.RoomsManager;

                var config = ElympicsConfig.Load();
                config.SwitchGame(config.AvailableGames.Select((x, i) => (Index: i, Name: x.GameName))
                    .First(x => x.Name == EgbGameplayGameName).Index);

                playOnlineButton.onClick.AddListener(OnPlayClicked);
                sentGeDataField.onEndEdit.AddListener(ValidateGeData);
                sentMmDataField.onEndEdit.AddListener(ValidateMmData);
                playOnlineButton.interactable = ElympicsLobbyClient.Instance!.IsAuthenticated;

                await ElympicsLobbyClient.Instance!.ConnectToElympicsAsync(new ConnectionData
                {
                    AuthType = AuthType.ClientSecret,
                    Region = new RegionData
                    {
                        Name = "warsaw",
                    }
                });

                ElympicsLobbyClient.Instance.RoomsManager.JoinedRoom += OnRoomJoined;
                playOnlineButton.interactable = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void ValidateGeData(string text)
        {
            try
            {
                SentGeData = DataConverter.ParseGameEngineData(sentGeDataField.text);
            }
            catch
            {
                Debug.LogWarning($"Invalid GameEngine data format (should be base64):\n{text}");
                SentGeData = Array.Empty<byte>();
                sentGeDataField.text = null;
            }
        }

        private void ValidateMmData(string text)
        {
            try
            {
                SentMmData = DataConverter.ParseMatchmakerData(sentMmDataField.text);
            }
            catch
            {
                Debug.LogWarning($"Invalid Matchmaker data format (should be JS-style float array):\n{text}");
                SentMmData = Array.Empty<float>();
                sentMmDataField.text = null;
            }
        }

        private void OnPlayClicked()
        {
            playOnlineButton.interactable = false;
            playOnlineButton.onClick.RemoveListener(OnPlayClicked);
            ElympicsLobbyClient.Instance!.RoomsManager.StartQuickMatch("Solo", SentGeData, SentMmData).Forget();
        }

        private void OnRoomJoined(JoinedRoomArgs obj)
        {
            Debug.Log("Joined room.");
            var room = _roomsManager.CurrentRoom!;
            if (room.IsMatchAvailable)
                room.PlayAvailableMatch();
        }
    }
}
