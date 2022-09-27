using System.Linq;
using System.Text;
using Elympics;
using UnityEngine;

namespace MatchEvents
{
	public class EndGameInputHandler : ElympicsMonoBehaviour, IInputHandler, IServerHandler, IUpdatable
	{
		public static bool ShouldGameEnd { get; set; }

		private int _totalPlayers;

		public void OnInputForClient(IInputWriter inputSerializer)
		{
			inputSerializer.Write(ShouldGameEnd);
		}

		public void OnInputForBot(IInputWriter inputSerializer)
		{
		}

		public void ElympicsUpdate()
		{
			for (var i = 0; i < _totalPlayers; i++)
				if (ElympicsBehaviour.TryGetInput(ElympicsPlayer.FromIndex(i), out var inputDeserializer))
				{
					inputDeserializer.Read(out bool shouldGameEnd);
					if (!shouldGameEnd)
						continue;
					Debug.Log("Ending game...");
					Elympics.EndGame(new ResultMatchPlayerDatas(
						Enumerable.Range(0, _totalPlayers).Select(x => new ResultMatchPlayerData
						{
							MatchmakerData = new float[] { x, 0 },
							GameEngineData = Encoding.ASCII.GetBytes("Ended on input")
						}).ToList()
					));
				}
		}

		public void OnServerInit(InitialMatchPlayerDatas initialMatchPlayerDatas)
		{
			_totalPlayers = initialMatchPlayerDatas.Count;
		}

		public void OnPlayerDisconnected(ElympicsPlayer player)
		{
		}

		public void OnPlayerConnected(ElympicsPlayer player)
		{
		}
	}
}
