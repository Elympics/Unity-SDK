using System;
using System.Collections.Generic;
using System.Net;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#nullable enable

namespace Elympics
{
    [CreateAssetMenu(fileName = "ElympicsGameConfig", menuName = "Elympics/GameConfig")]
    public class ElympicsGameConfig : ScriptableObject
    {
        private const int InputsToSendBufferSizeDefault = 3;

        [SerializeField] internal string gameName = "Game";
        [SerializeField] internal string gameId = "fe9b83a9-7d50-4299-859a-93fd313f420b";
        [SerializeField] internal string gameVersion = "1";
        [SerializeField] internal int players = 2;
        [SerializeField] internal string gameplayScene = "";
        [SerializeField] internal SceneAsset? gameplaySceneAsset;

        [SerializeField] private bool botsInServer = true;

        [SerializeField] private bool useWeb;
        [SerializeField] private bool enableReconnect;
        [SerializeField] private ClientConnectionSettings connectionConfig = new();

        [SerializeField] private int ticksPerSecond = 30;

        [SerializeField] private float minClientTickRateFactor = 0.8f;

        [SerializeField] private float maxClientTickRateFactor = 1.25f;

        [SerializeField] private int snapshotSendingPeriodInTicks = 1;
        [SerializeField] private int inputLagTicks = 2;
        [SerializeField] private int inputToSendBufferSize = InputsToSendBufferSizeDefault;
        [SerializeField] private int maxAllowedLagInTicks = 15;
        [SerializeField] private int forceJumpThresholdInTicks = 6;
        [SerializeField] private bool prediction = true;
        [SerializeField] private int predictionLimitInTicks = 8;

        [SerializeField] private bool detailedNetworkLog;

        [SerializeField] private GameplaySceneDebugModeEnum mode = GameplaySceneDebugModeEnum.HalfRemote;

        [SerializeField] private HalfRemoteModeEnum halfRemoteMode = HalfRemoteModeEnum.Server;
        [SerializeField] private string ipForHalfRemoteMode = "127.0.0.1";
        [SerializeField] private ushort tcpPortForHalfRemoteMode = 9101;
        [SerializeField] private ushort webPortForHalfRemoteMode = 9102;
        [SerializeField] private bool recordSnapshots;
        [SerializeField] private string snapshotFilePath = "";
        [SerializeField] private int playerIndexForHalfRemoteMode = 1;
        [SerializeField] private InitialMatchData testMatchData = new();
        [SerializeField] private List<InitialUserData> testPlayers = new();

        internal event Action? DataChanged;

        public string GameName => gameName;
        public string GameId => gameId;

        public string GameVersion
        {
            get => gameVersion;
            set => gameVersion = value;
        }

        public int Players => players;
        public string GameplayScene => gameplayScene;

        public bool BotsInServer => botsInServer;

        public bool UseWeb =>
#if UNITY_WEBGL
            true;
#else
            ApplicationParameters.Parameters.ShouldUseWebRtc.Priority >= ApplicationParameters.Parameters.ShouldUseTcpUdp.Priority
                ? ApplicationParameters.Parameters.ShouldUseWebRtc.GetValue(useWeb)
                : !ApplicationParameters.Parameters.ShouldUseTcpUdp.GetValue(!useWeb);
#endif
        public bool Prediction => prediction;
        public bool ReconnectEnabled => enableReconnect;
        public ClientConnectionSettings ConnectionConfig => connectionConfig;

        public int TicksPerSecond => ticksPerSecond;
        public int SnapshotSendingPeriodInTicks => snapshotSendingPeriodInTicks;
        public float TickDuration => 1.0f / ticksPerSecond;
        public float MinTickRate => ticksPerSecond * minClientTickRateFactor;
        public float MaxTickRate => ticksPerSecond * maxClientTickRateFactor;
        public int InputLagTicks => inputLagTicks;

        public bool DetailedNetworkLog => detailedNetworkLog;
        internal GameplaySceneDebugModeEnum GameplaySceneDebugMode => mode;
        internal HalfRemoteModeEnum HalfRemoteMode => GetHalfRemoteMode(halfRemoteMode);
        public string IpForHalfRemoteMode => ApplicationParameters.Parameters.HalfRemoteIp.GetValue(IPAddress.Parse(ipForHalfRemoteMode)).ToString();
        public ushort PortForHalfRemoteMode => ApplicationParameters.Parameters.HalfRemotePort.GetValue(UseWeb ? WebPortForHalfRemoteMode : TcpPortForHalfRemoteMode);
        public ushort TcpPortForHalfRemoteMode => tcpPortForHalfRemoteMode;
        public ushort WebPortForHalfRemoteMode => webPortForHalfRemoteMode;
        public bool RecordSnapshots => recordSnapshots;
        public string SnapshotFilePath => snapshotFilePath;
        public int InputsToSendBufferSize => inputToSendBufferSize;
        public int ForceJumpThresholdInTicks => forceJumpThresholdInTicks;
        public int PredictionBufferSize => inputLagTicks + snapshotSendingPeriodInTicks + maxAllowedLagInTicks;
        public int TotalPredictionLimitInTicks => inputLagTicks + snapshotSendingPeriodInTicks + predictionLimitInTicks;
        public int PlayerIndexForHalfRemoteMode
        {
            get => GetHalfRemotePlayerIndex(ApplicationParameters.Parameters.HalfRemotePlayerIndex.GetValue(playerIndexForHalfRemoteMode));
            internal set => playerIndexForHalfRemoteMode = value;
        }
        public InitialMatchData TestMatchData => testMatchData;
        public List<InitialUserData> TestPlayers => testPlayers;

        [SerializeField] private HalfRemoteLagConfig halfRemoteLagConfig = new();
        [SerializeField] private ReconciliationFrequencyEnum reconciliationFrequency = ReconciliationFrequencyEnum.OnlyIfNeeded;
        public HalfRemoteLagConfig HalfRemoteLagConfig
        {
            get => halfRemoteLagConfig;
#if UNITY_EDITOR
            set => halfRemoteLagConfig = value;
#endif
        }

        public ReconciliationFrequencyEnum ReconciliationFrequency
        {
            get => reconciliationFrequency;
#if UNITY_EDITOR
            set => reconciliationFrequency = value;
#endif
        }

        internal void ProcessElympicsConfigDataChanged() => DataChanged?.Invoke();

        public static bool GetUseWeb(bool defaultUseWeb)
        {
#if UNITY_WEBGL
            return true;
#else
            return defaultUseWeb;
#endif
        }

        public static HalfRemoteModeEnum GetHalfRemoteMode(HalfRemoteModeEnum defaultHalfRemoteMode) => IsOverridenInHalfRemoteByClone() ? ElympicsClonesManager.IsBot() ? HalfRemoteModeEnum.Bot : HalfRemoteModeEnum.Client : defaultHalfRemoteMode;

        public static bool IsOverridenInHalfRemoteByClone() => ElympicsClonesManager.IsClone();

        public static int GetHalfRemotePlayerIndex(int defaultPlayerIndex) => IsOverridenInHalfRemoteByClone() ? ElympicsClonesManager.GetCloneNumber() : defaultPlayerIndex;

        public static bool IsOverridenByWebGL()
        {
#if UNITY_WEBGL
            return true;
#else
            return false;
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
            SnapshotReplay,
            SinglePlayer,
        }

        public enum HalfRemoteModeEnum
        {
            Server,
            Client,
            Bot,
        }

        public enum ReconciliationFrequencyEnum
        {
            OnlyIfNeeded = 0,
            Never,
            OnEverySnapshot,
        }

        [Serializable]
        public class InitialUserData
        {
            public string userId = "";
            public bool isBot;
            public double botDifficulty;
            public byte[] gameEngineData = Array.Empty<byte>();
            public float[] matchmakerData = Array.Empty<float>();
        }

        [Serializable]
        public class InitialMatchData
        {
            public string queueName = "";
            public string regionName = "";
        }
    }
}
