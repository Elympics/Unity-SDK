using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using MatchTcpClients.Synchronizer;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Elympics
{
	public abstract class ElympicsBase : MonoBehaviour, IElympics
	{
		[SerializeField] internal ElympicsBehavioursManager elympicsBehavioursManager;
		[SerializeField] internal ElympicsLateFixedUpdate   elympicsLateFixedUpdate;
		[SerializeField] internal AsyncEventsDispatcher     asyncEventsDispatcher;

		[Tooltip("Attach gameobjects that you want to be destroyed together with this system")]
		[SerializeField]
		private GameObject[] linkedLogic;

		private readonly Stopwatch _elympicsUpdateStopwatch = new Stopwatch();

		private protected ElympicsGameConfig Config;

		internal CallContext CurrentCallContext { get; set; } = CallContext.None;

		internal void InitializeInternal(ElympicsGameConfig elympicsGameConfig)
		{
			Config = elympicsGameConfig;
		}

		public void Destroy()
		{
			foreach (var linkedLogicObject in linkedLogic)
				DestroyImmediate(linkedLogicObject);
			DestroyImmediate(gameObject);
		}

		private void FixedUpdate()
		{
			if (!ShouldDoFixedUpdate())
				return;

			_elympicsUpdateStopwatch.Stop();
			if (Config.DetailedNetworkLog)
				LogFixedUpdateThrottle();
			_elympicsUpdateStopwatch.Reset();
			_elympicsUpdateStopwatch.Start();

			DoFixedUpdate();

			_elympicsUpdateStopwatch.Stop();
			if (Config.DetailedNetworkLog)
				LogElympicsTickThrottle();
			_elympicsUpdateStopwatch.Start();

			elympicsLateFixedUpdate.LateFixedUpdateAction = LateFixedUpdate;
		}

		private void LogFixedUpdateThrottle()
		{
			if (_elympicsUpdateStopwatch.Elapsed.TotalSeconds > Config.TickDuration * 1.2)
				Debug.LogWarning(GetFixedUpdateThrottleMessage(_elympicsUpdateStopwatch.Elapsed.TotalMilliseconds, 120));
			else if (_elympicsUpdateStopwatch.Elapsed.TotalSeconds > Config.TickDuration * 1.9)
				Debug.LogError(GetFixedUpdateThrottleMessage(_elympicsUpdateStopwatch.Elapsed.TotalMilliseconds, 190));
		}

		private string GetFixedUpdateThrottleMessage(double elapsedMs, int percent) => $"[Elympics] Throttle on tick {Tick}! Total fixed update time {elapsedMs:F} ms, more than {percent}% time of {Config.TickDuration * 1000:F} ms tick";

		private void LogElympicsTickThrottle()
		{
			if (_elympicsUpdateStopwatch.Elapsed.TotalSeconds > Config.TickDuration * 0.66)
				Debug.LogWarning(GetElympicsTickThrottleMessage(_elympicsUpdateStopwatch.Elapsed.TotalMilliseconds, 66));
			else if (_elympicsUpdateStopwatch.Elapsed.TotalSeconds > Config.TickDuration)
				Debug.LogError(GetElympicsTickThrottleMessage(_elympicsUpdateStopwatch.Elapsed.TotalMilliseconds, 100));
		}

		private string GetElympicsTickThrottleMessage(double elapsedMs, int percent) => $"[Elympics] Throttle on tick {Tick}! Total elympics tick time {elapsedMs:F} ms, more than {percent}% time of {Config.TickDuration * 1000:F} ms tick";

		public bool TryGetBehaviour(int networkId, out ElympicsBehaviour elympicsBehaviour)
		{
			return elympicsBehavioursManager.TryGetBehaviour(networkId, out elympicsBehaviour);
		}

		protected virtual  bool ShouldDoFixedUpdate() => true;
		protected abstract void DoFixedUpdate();

		protected virtual void LateFixedUpdate()
		{
		}

		#region ConnectionCallbacks

		protected void OnStandaloneClientInit(InitialMatchPlayerData data) => Enqueue(() => elympicsBehavioursManager.OnStandaloneClientInit(data));
		protected void OnClientsOnServerInit(InitialMatchPlayerDatas data) => Enqueue(() => elympicsBehavioursManager.OnClientsOnServerInit(data));
		protected void OnSynchronized(TimeSynchronizationData data)        => Enqueue(() => elympicsBehavioursManager.OnSynchronized(data));
		protected void OnDisconnectedByServer()                            => Enqueue(elympicsBehavioursManager.OnDisconnectedByServer);
		protected void OnDisconnectedByClient()                            => Enqueue(elympicsBehavioursManager.OnDisconnectedByClient);
		protected void OnConnected(TimeSynchronizationData data)           => Enqueue(() => elympicsBehavioursManager.OnConnected(data));
		protected void OnConnectingFailed()                                => Enqueue(elympicsBehavioursManager.OnConnectingFailed);
		protected void OnAuthenticated(string userId)                      => Enqueue(() => elympicsBehavioursManager.OnAuthenticated(userId));
		protected void OnAuthenticatedFailed(string errorMessage)          => Enqueue(() => elympicsBehavioursManager.OnAuthenticatedFailed(errorMessage));
		protected void OnMatchJoined(string matchId)                       => Enqueue(() => elympicsBehavioursManager.OnMatchJoined(matchId));
		protected void OnMatchEnded(string matchId)                        => Enqueue(() => elympicsBehavioursManager.OnMatchEnded(matchId));
		protected void OnMatchJoinedFailed(string errorMessage)            => Enqueue(() => elympicsBehavioursManager.OnMatchJoinedFailed(errorMessage));

		#endregion

		#region BotCallbacks

		protected void OnStandaloneBotInit(InitialMatchPlayerData initialMatchData) => Enqueue(() => elympicsBehavioursManager.OnStandaloneBotInit(initialMatchData));
		protected void OnBotsOnServerInit(InitialMatchPlayerDatas initialMatchData) => Enqueue(() => elympicsBehavioursManager.OnBotsOnServerInit(initialMatchData));

		#endregion

		#region ServerCallbacks

		protected void OnServerInit(InitialMatchPlayerDatas initialMatchData) => Enqueue(() => elympicsBehavioursManager.OnServerInit(initialMatchData));
		protected void OnPlayerConnected(ElympicsPlayer player)               => Enqueue(() => elympicsBehavioursManager.OnPlayerConnected(player));
		protected void OnPlayerDisconnected(ElympicsPlayer player)            => Enqueue(() => elympicsBehavioursManager.OnPlayerDisconnected(player));

		#endregion

		protected void Enqueue(Action action) => asyncEventsDispatcher.Enqueue(action);


		#region IElympics

		public virtual ElympicsPlayer Player => ElympicsPlayer.Invalid;

		public virtual bool IsBot    => false;
		public virtual bool IsServer => false;
		public virtual bool IsClient => false;

		public float TickDuration   => Config.TickDuration;
		public int   TicksPerSecond => Config.TicksPerSecond;

		public long  Tick { get; protected set; }

		#region Client

		public virtual IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct)    => throw new SupportedOnlyByClientException();
		public virtual IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct) => throw new SupportedOnlyByClientException();
		public virtual void        Disconnect()                                                                    => throw new SupportedOnlyByClientException();

		#endregion

		#region Server

		public virtual void EndGame(ResultMatchPlayerDatas result = null) => throw new SupportedOnlyByServerException();

		#endregion

		#endregion

		internal enum CallContext
		{
			None,
			ElympicsUpdate,
			Initialize
		}
	}
}
