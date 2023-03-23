using System;
using UnityEngine;
using UnityEngine.UI;

namespace Elympics.EgbTest
{
	public class GameStarter : MonoBehaviour
	{
		[SerializeField] private InputField sentGeDataField;
		[SerializeField] private InputField sentMmDataField;
		[SerializeField] private Button playOnlineButton;

		public static byte[] SentGeData { get; private set; }
		public static float[] SentMmData { get; private set; }

		private void Start()
		{
			playOnlineButton.onClick.AddListener(OnPlayClicked);
			sentGeDataField.onEndEdit.AddListener(ValidateGeData);
			sentMmDataField.onEndEdit.AddListener(ValidateMmData);
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
			ElympicsLobbyClient.Instance.PlayOnline(SentMmData, SentGeData, queueName: "Solo", regionName: "warsaw");
			playOnlineButton.onClick.RemoveListener(OnPlayClicked);
		}
	}
}
