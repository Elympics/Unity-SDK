using System;
using System.Collections.Generic;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Elympics
{
	[CreateAssetMenu(fileName = "ElympicsGameConfig", menuName = "Elympics/GameConfig")]
	public class ElympicsGameConfig : ScriptableObject
	{
		private const int InputsToSendBufferSizeDefault = 3;

		[SerializeField] internal string gameName    = "Game";
		[SerializeField] internal string gameId      = "fe9b83a9-7d50-4299-859a-93fd313f420b";
		[SerializeField] internal string gameVersion = "1";
		[SerializeField] internal int    players     = 2;
		[SerializeField] internal string gameplayScene;
		[SerializeField] internal Object gameplaySceneAsset;

		[SerializeField] private bool botsInServer = true;

		[SerializeField] private bool useWeb;
		[SerializeField] private bool legacyMatchmakingClient;
		[SerializeField] private bool enableReconnect;
		[SerializeField] private ClientConnectionSettings connectionConfig = new ClientConnectionSettings();

		[SerializeField] private int  ticksPerSecond               = 30;
		[SerializeField] private int  snapshotSendingPeriodInTicks = 1;
		[SerializeField] private int  inputLagTicks                = 2;
		[SerializeField] private int  maxAllowedLagInTicks         = 15;
		[SerializeField] private bool prediction                   = true;
		[SerializeField] private int  predictionLimitInTicks       = 8;

		[SerializeField] private bool detailedNetworkLog;

		[SerializeField] private GameplaySceneDebugModeEnum mode = GameplaySceneDebugModeEnum.HalfRemote;

		[SerializeField] private HalfRemoteModeEnum    halfRemoteMode               = HalfRemoteModeEnum.Server;
		[SerializeField] private bool                  useWebInHalfRemote           = false;
		[SerializeField] private string                ipForHalfRemoteMode          = "127.0.0.1";
		[SerializeField] private int                   tcpPortForHalfRemoteMode     = 9101;
		[SerializeField] private int                   webPortForHalfRemoteMode     = 9102;
		[SerializeField] private int                   playerIndexForHalfRemoteMode = 1;
		[SerializeField] private InitialMatchData      testMatchData;
		[SerializeField] private List<InitialUserData> testPlayers;

		internal event Action DataChanged;

		public string GameName => gameName;
		public string GameId   => gameId;

		public string GameVersion
		{
			get => gameVersion;
			set => gameVersion = value;
		}

		public int    Players       => players;
		public string GameplayScene => gameplayScene;

		public bool BotsInServer => botsInServer;

		public bool UseWeb => GetUseWeb(useWeb);
		internal bool UseLegacyMatchmaking => legacyMatchmakingClient;
		public bool Prediction => prediction;
		public bool ReconnectEnabled => enableReconnect;
		public ClientConnectionSettings ConnectionConfig => connectionConfig;

		public int   TicksPerSecond               => ticksPerSecond;
		public int   SnapshotSendingPeriodInTicks => snapshotSendingPeriodInTicks;
		public float TickDuration                 => 1.0f / ticksPerSecond;
		public int   InputLagTicks                => inputLagTicks;

		public   bool                       DetailedNetworkLog           => detailedNetworkLog;
		internal GameplaySceneDebugModeEnum GameplaySceneDebugMode       => mode;
		internal HalfRemoteModeEnum         HalfRemoteMode               => GetHalfRemoteMode(halfRemoteMode);
		public   bool                       UseWebInHalfRemote           => GetUseWebInHalfRemote(useWebInHalfRemote);
		public   string                     IpForHalfRemoteMode          => ipForHalfRemoteMode;
		public   int                        TcpPortForHalfRemoteMode     => tcpPortForHalfRemoteMode;
		public   int                        WebPortForHalfRemoteMode     => webPortForHalfRemoteMode;
		public   int                        InputsToSendBufferSize       => InputsToSendBufferSizeDefault;
		public   int                        PredictionBufferSize         => inputLagTicks + snapshotSendingPeriodInTicks + maxAllowedLagInTicks;
		public   int                        TotalPredictionLimitInTicks  => inputLagTicks + snapshotSendingPeriodInTicks + predictionLimitInTicks;
		public   int                        PlayerIndexForHalfRemoteMode => GetHalfRemotePlayerIndex(playerIndexForHalfRemoteMode);
		public   InitialMatchData           TestMatchData                => testMatchData;
		public   List<InitialUserData>      TestPlayers                  => testPlayers;

		[field: NonSerialized] public HalfRemoteLagConfig         HalfRemoteLagConfig     { get; }      = new HalfRemoteLagConfig();
		[field: NonSerialized] public ReconciliationFrequencyEnum ReconciliationFrequency { get; set; } = ReconciliationFrequencyEnum.OnlyIfNeeded;

		internal void ProcessElympicsConfigDataChanged()
		{
			DataChanged?.Invoke();
		}

		public static bool GetUseWeb(bool defaultUseWeb)
		{
#if UNITY_WEBGL
			return true;
#else
			return defaultUseWeb;
#endif
		}

		public static HalfRemoteModeEnum GetHalfRemoteMode(HalfRemoteModeEnum defaultHalfRemoteMode) => IsOverridenInHalfRemoteByClone()
			? ElympicsClonesManager.IsBot() ? HalfRemoteModeEnum.Bot : HalfRemoteModeEnum.Client
			: defaultHalfRemoteMode;

		public static bool IsOverridenInHalfRemoteByClone() => ElympicsClonesManager.IsClone();

		public static int GetHalfRemotePlayerIndex(int defaultPlayerIndex) => IsOverridenInHalfRemoteByClone()
			? ElympicsClonesManager.GetCloneNumber()
			: defaultPlayerIndex;

		public static bool IsOverridenByWebGL()
		{
#if UNITY_WEBGL
			return true;
#else
			return false;
#endif
		}

		public static bool GetUseWebInHalfRemote(bool defaultUseWebInHalfRemote)
		{
#if UNITY_WEBGL
			return true;
#else
			return defaultUseWebInHalfRemote;
#endif
		}

#if UNITY_EDITOR
		internal void UpdateGameVersion(string newGameVersion)
		{
			gameVersion = newGameVersion;
			EditorUtility.SetDirty(this);
		}
#endif
		public enum GameplaySceneDebugModeEnum
		{
			LocalPlayerAndBots,
			HalfRemote,
			DebugOnlinePlayer,
		}

		public enum HalfRemoteModeEnum
		{
			Server,
			Client,
			Bot
		}

		public enum ReconciliationFrequencyEnum
		{
			OnlyIfNeeded = 0,
			Never,
			OnEverySnapshot
		}

		[Serializable]
		public class InitialUserData
		{
			public string  userId;
			public bool    isBot;
			public double  botDifficulty;
			public byte[]  gameEngineData;
			public float[] matchmakerData;
		}

		[Serializable]
		public class InitialMatchData
		{
			public string queueName;
			public string regionName;
		}
	}
}
