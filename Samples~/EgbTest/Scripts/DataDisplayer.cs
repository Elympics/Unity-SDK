using System;
using System.Linq;
using MatchTcpClients.Synchronizer;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEngine;
using UnityEngine.UI;

namespace Elympics.EgbTest
{
	public class DataDisplayer : MonoBehaviour, IClientHandler
	{
		[SerializeField] private InputField sentGeDataField;
		[SerializeField] private InputField sentMmDataField;
		[SerializeField] private InputField receivedGeDataField;
		[SerializeField] private InputField receivedMmDataField;

		private static readonly Color HighlightColor = new Color(1, 0.58f, 0.58f);

		private byte[] _sentGeData;
		private float[] _sentMmData;

		private void Start()
		{
			if (ElympicsLobbyClient.Instance != null)
			{
				_sentGeData = GameStarter.SentGeData;
				_sentMmData = GameStarter.SentMmData;
			}
			else
			{
				var config = ElympicsConfig.LoadCurrentElympicsGameConfig();
				var playerIndex = ElympicsClonesManager.IsClone() ? ElympicsClonesManager.GetCloneNumber() + 1 : 0;
				var testPlayerData = config.TestPlayers[playerIndex];
				_sentGeData = testPlayerData.gameEngineData;
				_sentMmData = testPlayerData.matchmakerData;
			}
			sentGeDataField.text = DataConverter.StringifyGameEngineData(_sentGeData);
			sentMmDataField.text = DataConverter.StringifyMatchmakerData(_sentMmData);
		}

		public void OnStandaloneClientInit(InitialMatchPlayerData data)
		{
			if (data.GameEngineData?.SequenceEqual(_sentGeData ?? Array.Empty<byte>()) is true)
				receivedGeDataField.text = sentGeDataField.text;
			else
			{
				receivedGeDataField.text = DataConverter.StringifyGameEngineData(data.GameEngineData);
				HighlightField(receivedGeDataField);
			}
			receivedGeDataField.readOnly = true;

			if (data.MatchmakerData?.SequenceEqual(_sentMmData ?? Array.Empty<float>()) is true)
				receivedMmDataField.text = sentMmDataField.text;
			else
			{
				receivedMmDataField.text = DataConverter.StringifyMatchmakerData(data.MatchmakerData);
				HighlightField(receivedMmDataField);
			}
			receivedMmDataField.readOnly = true;
		}

		private static void HighlightField(Selectable field)
		{
			var colors = field.colors;
			colors.normalColor = colors.selectedColor = HighlightColor;
			field.colors = colors;
		}

		public void OnClientsOnServerInit(InitialMatchPlayerDatas data)
		{
			Debug.LogError("This sample is meant for Debug Online and Online modes only.");
		}

		#region Unused callbacks

		public void OnConnected(TimeSynchronizationData data) { }

		public void OnConnectingFailed() { }

		public void OnDisconnectedByServer() { }

		public void OnDisconnectedByClient() { }

		public void OnSynchronized(TimeSynchronizationData data) { }

		public void OnAuthenticated(string userId) { }

		public void OnAuthenticatedFailed(string errorMessage) { }

		public void OnMatchJoined(string matchId) { }

		public void OnMatchJoinedFailed(string errorMessage) { }

		public void OnMatchEnded(string matchId) { }

		#endregion Unused callbacks
	}
}
