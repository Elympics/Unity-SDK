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
		[SerializeField] internal AsyncEventsDispatcher     asyncEventsDispatcher;

		[Tooltip("Attach GameObjects that you want to be destroyed together with this system")]
		[SerializeField]
		private GameObject[] linkedLogic;

		private readonly Stopwatch _elympicsUpdateStopwatch = new Stopwatch();
		private          long      _fixedUpdatesCounter;
		private          double    _timer;

		private DateTime _previousUtcForDeltaTime = DateTime.UtcNow;
		private double   MaxDeltaTime => 3 * TickDuration;

		protected virtual double MaxUpdateTimeWarningThreshold => ElympicsUpdateDuration;
		private protected DateTime           TickStartUtc;
		private protected ElympicsGameConfig Config;

		internal CallContext CurrentCallContext     { get; set; } = CallContext.None;
		internal double      ElympicsUpdateDuration { get; private protected set; }

		internal bool Initialized { get; private set; }

		internal void SetInitialized()
		{
			Initialized = true;
			Instance = this;
		}

		private static ElympicsBase instance;

		private static ElympicsBase Instance
		{
			get => instance;
			set
			{
				instance = value;
				TickAnalysis?.Detach();
				instance?.TryAttachTickAnalysis();
			}
		}

		#region TickAnalysis

		private static ITickAnalysis tickAnalysis;

		internal static ITickAnalysis TickAnalysis
		{
			private protected get => tickAnalysis;
			set
			{
				tickAnalysis?.Detach();
				tickAnalysis = value;
				Instance?.TryAttachTickAnalysis();
			}
		}

		private protected virtual void TryAttachTickAnalysis()
		{
			TickAnalysis?.Attach(snapshot => elympicsBehavioursManager.ApplySnapshot(snapshot, ignoreTolerance: true));
		}

		#endregion TickAnalysis

		internal void InitializeInternal(ElympicsGameConfig elympicsGameConfig)
		{
			Config = elympicsGameConfig;
			ElympicsUpdateDuration = TickDuration;
			_previousUtcForDeltaTime = DateTime.UtcNow;
		}

		public void Destroy()
		{
			foreach (var linkedLogicObject in linkedLogic)
				DestroyImmediate(linkedLogicObject);
			DestroyImmediate(gameObject);
		}

		private void Update()
		{
			if (!ShouldDoElympicsUpdate())
				return;

			var currentUtc = DateTime.UtcNow;
			_timer += CalculateDeltaBasedOnUtcNow(currentUtc);

			while (_timer >= ElympicsUpdateDuration)
			{
				_timer -= ElympicsUpdateDuration;

				_elympicsUpdateStopwatch.Stop();
				if (Config.DetailedNetworkLog)
					LogFixedUpdateThrottle();
				_elympicsUpdateStopwatch.Reset();
				_elympicsUpdateStopwatch.Start();

				// Calculate ideal tick start time based on whats left in timer
				TickStartUtc = currentUtc.Subtract(TimeSpan.FromSeconds(_timer));
				ElympicsFixedUpdate();

				_elympicsUpdateStopwatch.Stop();
				if (Config.DetailedNetworkLog)
					LogElympicsTickThrottle();

				ElympicsLateFixedUpdate();
				_fixedUpdatesCounter++;
			}

			_elympicsUpdateStopwatch.Start();
		}

		private double CalculateDeltaBasedOnUtcNow(DateTime currentUtc)
		{
			var deltaTime = (currentUtc - _previousUtcForDeltaTime).TotalSeconds;
			deltaTime = Math.Min(deltaTime, MaxDeltaTime);
			_previousUtcForDeltaTime = currentUtc;
			return deltaTime;
		}

		protected ElympicsSnapshotWithMetadata CreateLocalSnapshotWithMetadata()
		{
			var localSnapshotWithMetadata = elympicsBehavioursManager.GetLocalSnapshotWithMetadata();
			localSnapshotWithMetadata.Tick = Tick;
			localSnapshotWithMetadata.TickStartUtc = TickStartUtc;
			localSnapshotWithMetadata.TickEndUtc = DateTime.UtcNow;
			localSnapshotWithMetadata.FixedUpdateNumber = _fixedUpdatesCounter;
			return localSnapshotWithMetadata;
		}

		private void LogFixedUpdateThrottle()
		{
			if (_elympicsUpdateStopwatch.Elapsed.TotalSeconds > MaxUpdateTimeWarningThreshold * 1.2)
				Debug.LogWarning(GetFixedUpdateThrottleMessage(_elympicsUpdateStopwatch.Elapsed.TotalMilliseconds, 120));
			else if (_elympicsUpdateStopwatch.Elapsed.TotalSeconds > MaxUpdateTimeWarningThreshold * 1.9)
				Debug.LogError(GetFixedUpdateThrottleMessage(_elympicsUpdateStopwatch.Elapsed.TotalMilliseconds, 190));
		}

		private string GetFixedUpdateThrottleMessage(double elapsedMs, int percent) => $"[Elympics] Throttle on tick {Tick}! Total fixed update time {elapsedMs:F} ms, more than {percent}% time of {Config.TickDuration * 1000:F} ms tick";

		private void LogElympicsTickThrottle()
		{
			if (_elympicsUpdateStopwatch.Elapsed.TotalSeconds > MaxUpdateTimeWarningThreshold * 0.66)
				Debug.LogWarning(GetElympicsTickThrottleMessage(_elympicsUpdateStopwatch.Elapsed.TotalMilliseconds, 66));
			else if (_elympicsUpdateStopwatch.Elapsed.TotalSeconds > MaxUpdateTimeWarningThreshold)
				Debug.LogError(GetElympicsTickThrottleMessage(_elympicsUpdateStopwatch.Elapsed.TotalMilliseconds, 100));
		}

		private string GetElympicsTickThrottleMessage(double elapsedMs, int percent) => $"[Elympics] Throttle on tick {Tick}! Total elympics tick time {elapsedMs:F} ms, more than {percent}% time of {Config.TickDuration * 1000:F} ms tick";

		public bool TryGetBehaviour(int networkId, out ElympicsBehaviour elympicsBehaviour)
		{
			return elympicsBehavioursManager.TryGetBehaviour(networkId, out elympicsBehaviour);
		}

		protected virtual  bool ShouldDoElympicsUpdate() => true;
		protected abstract void ElympicsFixedUpdate();

		protected virtual void ElympicsLateFixedUpdate()
		{
		}

		#region ClientCallbacks

		protected void OnStandaloneClientInit(InitialMatchPlayerDataGuid data) => Enqueue(() => elympicsBehavioursManager.OnStandaloneClientInit(data));
		protected void OnSynchronized(TimeSynchronizationData data)            => Enqueue(() => elympicsBehavioursManager.OnSynchronized(data));
		protected void OnDisconnectedByServer()                                => Enqueue(elympicsBehavioursManager.OnDisconnectedByServer);
		protected void OnDisconnectedByClient()                                => Enqueue(elympicsBehavioursManager.OnDisconnectedByClient);
		protected void OnConnected(TimeSynchronizationData data)               => Enqueue(() => elympicsBehavioursManager.OnConnected(data));
		protected void OnConnectingFailed()                                    => Enqueue(elympicsBehavioursManager.OnConnectingFailed);
		protected void OnAuthenticated(Guid userId)                            => Enqueue(() => elympicsBehavioursManager.OnAuthenticated(userId));
		protected void OnAuthenticatedFailed(string errorMessage)              => Enqueue(() => elympicsBehavioursManager.OnAuthenticatedFailed(errorMessage));
		protected void OnMatchJoined(Guid matchId)                             => Enqueue(() => elympicsBehavioursManager.OnMatchJoined(matchId));
		protected void OnMatchEnded(Guid matchId)                              => Enqueue(() => elympicsBehavioursManager.OnMatchEnded(matchId));
		protected void OnMatchJoinedFailed(string errorMessage)                => Enqueue(() => elympicsBehavioursManager.OnMatchJoinedFailed(errorMessage));

		#endregion

		#region BotCallbacks

		protected void OnStandaloneBotInit(InitialMatchPlayerDataGuid initialMatchData) => Enqueue(() => elympicsBehavioursManager.OnStandaloneBotInit(initialMatchData));

		#endregion

		#region ServerCallbacks

		protected void OnPlayerConnected(ElympicsPlayer player)                   => Enqueue(() => elympicsBehavioursManager.OnPlayerConnected(player));
		protected void OnPlayerDisconnected(ElympicsPlayer player)                => Enqueue(() => elympicsBehavioursManager.OnPlayerDisconnected(player));

		#endregion

		protected void Enqueue(Action action) => asyncEventsDispatcher.Enqueue(action);


		#region IElympics

		public virtual ElympicsPlayer Player => ElympicsPlayer.Invalid;

		public virtual bool IsBot    => false;
		public virtual bool IsServer => false;
		public virtual bool IsClient => false;

		public float TickDuration   => Config.TickDuration;
		public int   TicksPerSecond => Config.TicksPerSecond;

		public long Tick { get; internal set; }

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
