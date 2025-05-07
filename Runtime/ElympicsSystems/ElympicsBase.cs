using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
    public abstract class ElympicsBase : MonoBehaviour, IElympics
    {
        internal ElympicsBehavioursManager ElympicsBehavioursManager;
        [SerializeField] internal AsyncEventsDispatcher asyncEventsDispatcher;

        [Tooltip("Attach GameObjects that you want to be destroyed together with this system")]
        [SerializeField]
        private GameObject[] linkedLogic;

        internal readonly ElympicsRpcMessageList RpcMessagesToSend = new();
        internal readonly List<ElympicsRpcMessageList> RpcMessagesToInvoke = new();
        private static readonly object RpcMessagesToInvokeLock = new();
        private readonly List<ElympicsRpcMessageList> _rpcMessagesToInvokeInCurrentTick = new();

        private readonly Stopwatch _elympicsUpdateStopwatch = new();
        private double _timer;

        private DateTime _previousUtcForDeltaTime = DateTime.UtcNow;
        private double MaxDeltaTime => 3 * TickDuration;

        protected virtual double MaxUpdateTimeWarningThreshold => ElympicsUpdateDuration;
        internal DateTime TickStartUtc { get; set; }
        internal DateTime TickEndUtc { get; set; }
        internal ElympicsGameConfig Config { get; private set; }

        internal CallContext CurrentCallContext { get; set; } = CallContext.None;
        internal double ElympicsUpdateDuration { get; private protected set; }

        internal bool Initialized { get; private set; }

        protected void SetInitialized()
        {
            Initialized = true;
            ElympicsLogger.Log($"{nameof(ElympicsBase)} ({GetType().Name}) initialized successfully for player ID: {Player}");
        }

        protected void SetDeInitialized()
        {
            Initialized = false;
            ElympicsLogger.Log($"{nameof(ElympicsBase)} ({GetType().Name}) DeInitialized for player ID: {Player}");
        }

        internal void InitializeInternal(ElympicsGameConfig elympicsGameConfig, ElympicsBehavioursManager elympicsBehavioursManager)
        {
            ElympicsLogger.Log($"Initializing {nameof(ElympicsBase)} ({GetType().Name})...");
            ElympicsBehavioursManager = elympicsBehavioursManager;
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
            var elympicsUpdateCalled = false;
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
                TickEndUtc = TickStartUtc + _elympicsUpdateStopwatch.Elapsed;
                if (Config.DetailedNetworkLog)
                    LogElympicsTickThrottle();

                ElympicsLateFixedUpdate();
                elympicsUpdateCalled = true;
            }
            var alpha = _timer / ElympicsUpdateDuration;
            var renderData = new RenderData()
            {
                Alpha = Convert.ToSingle(alpha),
                FirstFrame = elympicsUpdateCalled
            };
            ElympicsRenderUpdate(renderData);
            _elympicsUpdateStopwatch.Reset();
            _elympicsUpdateStopwatch.Start();
        }

        private double CalculateDeltaBasedOnUtcNow(DateTime currentUtc)
        {
            var deltaTime = (currentUtc - _previousUtcForDeltaTime).TotalSeconds;
            deltaTime = Math.Min(deltaTime, MaxDeltaTime);
            _previousUtcForDeltaTime = currentUtc;
            return deltaTime;
        }

        internal void InvokeQueuedRpcMessages()
        {
            _rpcMessagesToInvokeInCurrentTick.Clear();
            lock (RpcMessagesToInvokeLock)
                for (var i = RpcMessagesToInvoke.Count - 1; i >= 0; i--)
                {
                    if (RpcMessagesToInvoke[i].Tick > Tick)
                        continue;
                    _rpcMessagesToInvokeInCurrentTick.Add(RpcMessagesToInvoke[i]);
                    RpcMessagesToInvoke.RemoveAt(i);
                }
            foreach (var rpcMessageList in _rpcMessagesToInvokeInCurrentTick)
                foreach (var rpcMessage in rpcMessageList.Messages)
                    if (TryGetBehaviour(rpcMessage.NetworkId, out var behaviour))
                        behaviour.OnRpcInvoked(rpcMessage.MethodId, rpcMessage.Arguments);
        }

        internal void SendQueuedRpcMessages()
        {
            if (RpcMessagesToSend.Messages.Count == 0)
                return;
            RpcMessagesToSend.Tick = Tick;
            SendRpcMessageList(RpcMessagesToSend);
            RpcMessagesToSend.Messages.Clear();
        }

        private void LogFixedUpdateThrottle()
        {
            if (_elympicsUpdateStopwatch.Elapsed.TotalSeconds > MaxUpdateTimeWarningThreshold * 1.9)
                ElympicsLogger.LogError(GetFixedUpdateThrottleMessage(_elympicsUpdateStopwatch.Elapsed.TotalMilliseconds, 190));
            else if (_elympicsUpdateStopwatch.Elapsed.TotalSeconds > MaxUpdateTimeWarningThreshold * 1.2)
                ElympicsLogger.LogWarning(GetFixedUpdateThrottleMessage(_elympicsUpdateStopwatch.Elapsed.TotalMilliseconds, 120));
        }

        private string GetFixedUpdateThrottleMessage(double elapsedMs, int percent) =>
            $"Throttle on tick {Tick}! Total fixed update time {elapsedMs:F} ms, more than {percent}% time of {Config.TickDuration * 1000:F} ms tick";

        private void LogElympicsTickThrottle()
        {
            if (_elympicsUpdateStopwatch.Elapsed.TotalSeconds > MaxUpdateTimeWarningThreshold)
                ElympicsLogger.LogError(GetElympicsTickThrottleMessage(_elympicsUpdateStopwatch.Elapsed.TotalMilliseconds, 100));
            else if (_elympicsUpdateStopwatch.Elapsed.TotalSeconds > MaxUpdateTimeWarningThreshold * 0.66)
                ElympicsLogger.LogWarning(GetElympicsTickThrottleMessage(_elympicsUpdateStopwatch.Elapsed.TotalMilliseconds, 66));
        }

        private string GetElympicsTickThrottleMessage(double elapsedMs, int percent) =>
            $"Throttle on tick {Tick}! Total elympics tick time {elapsedMs:F} ms, more than {percent}% time of {Config.TickDuration * 1000:F} ms tick";

        public bool TryGetBehaviour(int networkId, out ElympicsBehaviour elympicsBehaviour)
        {
            return ElympicsBehavioursManager.TryGetBehaviour(networkId, out elympicsBehaviour);
        }

        protected virtual bool ShouldDoElympicsUpdate() => true;
        internal abstract void ElympicsFixedUpdate();

        public void QueueRpcMessageToSend(ElympicsRpcMessage rpcMessage) => RpcMessagesToSend.Messages.Add(rpcMessage);
        internal abstract void SendRpcMessageList(ElympicsRpcMessageList rpcMessageList);

        protected void QueueRpcMessagesToInvoke(ElympicsRpcMessageList rpcMessageList)
        {
            lock (RpcMessagesToInvokeLock)
                RpcMessagesToInvoke.Add(rpcMessageList);
        }

        protected virtual void ElympicsLateFixedUpdate()
        { }

        protected virtual void ElympicsRenderUpdate(in RenderData renderData) { }

        #region ClientCallbacks

        protected void OnStandaloneClientInit(InitialMatchPlayerDataGuid data) => Enqueue(() => ElympicsBehavioursManager.OnStandaloneClientInit(data));
        protected void OnSynchronized(TimeSynchronizationData data) => Enqueue(() => ElympicsBehavioursManager.OnSynchronized(data));
        protected void OnDisconnectedByServer() => Enqueue(ElympicsBehavioursManager.OnDisconnectedByServer);
        protected void OnDisconnectedByClient() => Enqueue(ElympicsBehavioursManager.OnDisconnectedByClient);
        protected void OnConnected(TimeSynchronizationData data) => Enqueue(() => ElympicsBehavioursManager.OnConnected(data));
        protected void OnConnectingFailed() => Enqueue(ElympicsBehavioursManager.OnConnectingFailed);
        protected void OnAuthenticated(Guid userId) => Enqueue(() => ElympicsBehavioursManager.OnAuthenticated(userId));
        protected void OnAuthenticatedFailed(string errorMessage) => Enqueue(() => ElympicsBehavioursManager.OnAuthenticatedFailed(errorMessage));
        protected void OnMatchJoined(Guid matchId) => Enqueue(() => ElympicsBehavioursManager.OnMatchJoined(matchId));
        protected void OnMatchEnded(Guid matchId) => Enqueue(() => ElympicsBehavioursManager.OnMatchEnded(matchId));
        protected void OnMatchJoinedFailed(string errorMessage) => Enqueue(() => ElympicsBehavioursManager.OnMatchJoinedFailed(errorMessage));

        #endregion

        #region BotCallbacks

        protected void OnStandaloneBotInit(InitialMatchPlayerDataGuid initialMatchData) => Enqueue(() => ElympicsBehavioursManager.OnStandaloneBotInit(initialMatchData));

        #endregion

        #region ServerCallbacks

        protected void OnPlayerConnected(ElympicsPlayer player) => Enqueue(() => ElympicsBehavioursManager.OnPlayerConnected(player));
        protected void OnPlayerDisconnected(ElympicsPlayer player) => Enqueue(() => ElympicsBehavioursManager.OnPlayerDisconnected(player));

        #endregion

        protected void Enqueue(Action action) => asyncEventsDispatcher.Enqueue(action);


        #region IElympics

        public virtual ElympicsPlayer Player => ElympicsPlayer.Invalid;

        public virtual bool IsBot => false;
        public virtual bool IsServer => false;
        public virtual bool IsClient => false;

        public virtual bool IsReplay => false;
        public bool IsClientOrBot => IsClient || IsBot;
        internal bool IsLocalMode => IsServer && IsClient; // assuming there is only one client (and Unlimited Bots Work)

        public float TickDuration => Config.TickDuration;
        public int TicksPerSecond => Config.TicksPerSecond;

        public abstract long Tick { get; }

        #region Client

        public virtual IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct) => throw new SupportedOnlyByClientException();
        public virtual IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct) => throw new SupportedOnlyByClientException();
        public virtual void Disconnect() => throw new SupportedOnlyByClientException();

        #endregion

        #region Server

        public virtual void EndGame(ResultMatchPlayerDatas result = null) => throw new SupportedOnlyByServerException();

        #endregion

        #endregion

        internal enum CallContext
        {
            None,
            RpcInvoking,
            ValueChanged,
            ElympicsUpdate,
            Initialize,
        }
    }
}
