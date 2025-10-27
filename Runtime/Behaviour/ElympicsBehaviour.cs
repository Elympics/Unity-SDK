using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using MatchTcpClients.Synchronizer;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Elympics
{
    /// <summary>
    /// Used for object state synchronization. Allows scripts attached to the object to implement interfaces inheriting from <see cref="IObservable"/>.
    /// </summary>
    [ExecuteInEditMode]
    public sealed class ElympicsBehaviour : MonoBehaviour, IEquatable<ElympicsBehaviour>
    {
        internal const int UndefinedNetworkId = -1;
        private const int DefaultAbsenceTickParameter = 0;

        [SerializeField] internal bool forceNetworkId;
        [SerializeField] internal int networkId = UndefinedNetworkId;
        [SerializeField] internal ElympicsPlayer predictableFor = ElympicsPlayer.World;
        [SerializeField] internal bool isUpdatableForNonOwners;
        [SerializeField] internal ElympicsPlayer visibleFor = ElympicsPlayer.All;

        [SerializeField]
        internal ElympicsBehaviourStateChangeFrequencyStage[] stateFrequencyStages =
        {
            new(500, 30),
            new(1000, 200),
            new(1000, 1000)
        };

        private ElympicsComponentsContainer _componentsContainer;

        internal RpcMethodsContainer RpcMethods { get; } = new();
        private bool _isReconciling;
        private bool _isInvokingRpc;

        private List<ElympicsVar> _backingFields;
        private Dictionary<ElympicsVar, string> _backingFieldsNames;
        private List<(string, List<ElympicsVar>)> _backingFieldsByComponents;
        private ElympicsBehaviourStateChangeFrequencyCalculator _behaviourStateChangeFrequencyCalculator;

        internal bool HasAnyState => _componentsContainer.Observables.Length > 0;
        internal bool HasAnyInput => _componentsContainer.InputHandler != null;

        public int NetworkId
        {
            get => networkId;
            internal set => networkId = value;
        }

        [UsedImplicitly] // from generated IL code
        public void ThrowIfRpcContextNotValid(ElympicsRpcProperties _, MethodInfo method)
        {
            if (ElympicsBase.CurrentCallContext is ElympicsBase.CallContext.RpcInvoking or ElympicsBase.CallContext.Initialize)
                return;
            if (!ElympicsBase.Config.Prediction)
                return;
            if (ElympicsBase.CurrentCallContext is ElympicsBase.CallContext.ElympicsUpdate)
                return;
            throw new ElympicsException($"Error calling {method.DeclaringType?.FullName}.{method.Name}: " + $"RPC cannot be scheduled outside of {nameof(IUpdatable.ElympicsUpdate)} " + $"or {nameof(IInitializable.Initialize)}");
        }

        [UsedImplicitly] // from generated IL code
        public bool ShouldRpcBeCaptured(ElympicsRpcProperties properties, MethodInfo method)
        {
            if (_isReconciling)
            {
                ElympicsLogger.LogWarning($"RPC {method.Name} will not be captured during reconciliation.");
                return false;
            }
            if (_isInvokingRpc)
                return false;
            if (ElympicsBase.IsLocalMode)
                return false;
            if (ElympicsBase.IsServer
                && ElympicsBase.IsBot
                && properties.Direction == ElympicsRpcDirection.PlayerToServer)
                return false;
            if ((properties.Direction == ElympicsRpcDirection.PlayerToServer && !(ElympicsBase.IsClient || ElympicsBase.IsBot))
                || (properties.Direction == ElympicsRpcDirection.ServerToPlayers && !ElympicsBase.IsServer))
                throw new RpcDirectionMismatchException(properties, method);
            return true;
        }

        [UsedImplicitly] // from generated IL code
        public bool ShouldRpcBeInvoked(ElympicsRpcProperties properties, MethodInfo methodInfo)
        {
            if (_isReconciling)
            {
                ElympicsLogger.LogWarning($"RPC {methodInfo.Name} will not be invoked during reconciliation.");
                return false;
            }
            if (_isInvokingRpc)
            {
                _isInvokingRpc = false;
                return true;
            }
            // TODO: The following two cases short-circuit RPCs so they are not queued for later bulk execution.
            // TODO: This may introduce unwanted behavior. ~dsygocki 2023-08-07
            if (ElympicsBase.IsLocalMode)
                return true;
            if (ElympicsBase.IsServer
                && ElympicsBase.IsBot
                && properties.Direction == ElympicsRpcDirection.PlayerToServer)
                return true;
            return false;
        }

        [UsedImplicitly] // from generated IL code
        public void OnRpcCaptured(ElympicsRpcProperties _, MethodInfo method, object target, params object[] arguments)
        {
            var rpcMethod = new RpcMethod(method, target);
            var methodId = RpcMethods.GetIdOf(rpcMethod);
            var rpcMessage = new ElympicsRpcMessage(NetworkId, methodId, arguments);
            ElympicsBase.QueueRpcMessageToSend(rpcMessage);
        }

        // this is NOT called from generated IL code
        internal void OnRpcInvoked(ushort methodId, params object[] arguments)
        {
            var rpcMethod = RpcMethods[methodId];
            _isInvokingRpc = true;
            try
            {
                using (ElympicsBase.SetTemporaryCallContext(ElympicsBase.CallContext.RpcInvoking))
                    rpcMethod.Call(arguments);
            }
            finally
            {
                _isInvokingRpc = false;
            }
        }

        public string PrefabName { get; internal set; }

        public ElympicsPlayer PredictableFor
        {
            get => predictableFor;
            internal set => predictableFor = value;
        }

        /// <summary>
        /// Provides Elympics-specific game instance data and methods.
        /// </summary>
        public IElympics Elympics => ElympicsBase;

        internal ElympicsBase ElympicsBase { get; private set; }
        public bool IsPredictableTo(ElympicsPlayer player) => predictableFor == ElympicsPlayer.All || player == predictableFor || player == ElympicsPlayer.World;
        public bool IsOwnedBy(ElympicsPlayer player) => IsPredictableTo(player);
        internal bool IsVisibleTo(ElympicsPlayer player) => visibleFor == ElympicsPlayer.All || player == visibleFor || player == ElympicsPlayer.World;

        private MemoryStream _memoryStream1;
        private MemoryStream _memoryStream2;
        private BinaryReader _binaryReader1;
        private BinaryReader _binaryReader2;
        private BinaryWriter _binaryWriter1;
        private BinaryInputReader _inputReader;

        private Dictionary<ElympicsPlayer, (long Tick, byte[] Data)> _tickBasedInputByPlayer;
        internal void ClearInputs()
        {
            _tickBasedInputByPlayer.Clear();
        }

        /// <summary>
        /// Retrieves received input for a player.
        /// </summary>
        /// <param name="player">Identifier of a player that the input is retrieved for.</param>
        /// <param name="inputReader">Input deserializer. Use its <c>Read</c> methods to parse data from the received input.</param>
        /// <param name="absenceTick"> How many ticks will be predicted due to lack of input from player.</param>
        /// <returns>If there is any input to retrieve for the given player.</returns>
        /// <seealso cref="IInputHandler.OnInputForClient"/>
        /// <seealso cref="IInputHandler.OnInputForBot"/>
        public bool TryGetInput(ElympicsPlayer player, out IInputReader inputReader, int absenceTick = DefaultAbsenceTickParameter)
        {
            if (ElympicsBase.CurrentCallContext != ElympicsBase.CallContext.ElympicsUpdate)
                throw new ElympicsException($"You cannot use {nameof(TryGetInput)} outside of {nameof(ElympicsBase.ElympicsBehavioursManager.ElympicsUpdate)}");
            if (!HasAnyInput)
                throw new ElympicsException($"{nameof(TryGetInput)} can be called only in classes implementing {nameof(IInputHandler)} interface");
            if (!_inputReader.AllBytesRead())
                throw new ReadNotEnoughException(this);

            inputReader = null;
            if (_tickBasedInputByPlayer.TryGetValue(player, out var tickBasedInput)
                && ElympicsBase.Tick - tickBasedInput.Tick <= absenceTick)
            {
                _inputReader.FeedDataForReading(tickBasedInput.Data);
                inputReader = _inputReader;
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private bool _previousForceNetworkIdState;

        private void OnValidate()
        {
            if (!forceNetworkId
                && (_previousForceNetworkIdState || networkId == UndefinedNetworkId))
                UpdateSerializedNetworkId();

            _behaviourStateChangeFrequencyCalculator?.ResetStateUpdateFrequencyStage();
            _previousForceNetworkIdState = forceNetworkId;
        }

        private void OnEnable()
        {
            if (!forceNetworkId && IsMyNetworkIdTaken())
                UpdateSerializedNetworkId();

            _behaviourStateChangeFrequencyCalculator?.ResetStateUpdateFrequencyStage();
        }

        private bool IsMyNetworkIdTaken()
        {
            return FindObjectsOfType<ElympicsBehaviour>().Where(behaviour => behaviour != this).Select(behaviour => behaviour.NetworkId).Contains(networkId);
        }

        internal void UpdateSerializedNetworkId()
        {
            networkId = NetworkIdEnumerator.Instance.MoveNextAndGetCurrent();
            EditorUtility.SetDirty(this);
        }

        private void OnDrawGizmos()
        { }
#endif

        internal void InitializeInternal(ElympicsBase elympicsBase)
        {
            _memoryStream1 = new MemoryStream();
            _memoryStream2 = new MemoryStream();
            _binaryReader1 = new BinaryReader(_memoryStream1);
            _binaryReader2 = new BinaryReader(_memoryStream2);
            _binaryWriter1 = new BinaryWriter(_memoryStream1);

            ElympicsBase = elympicsBase;

            _behaviourStateChangeFrequencyCalculator = new ElympicsBehaviourStateChangeFrequencyCalculator(stateFrequencyStages, AreStatesEqual, elympicsBase.Config);

            _componentsContainer = new ElympicsComponentsContainer(this);

            foreach (var observable in _componentsContainer.Observables)
                RpcMethods.CollectFrom(observable);

            using (ElympicsBase.SetTemporaryCallContext(ElympicsBase.CallContext.Initialize))
                foreach (var initializable in _componentsContainer.Initializables)
                    initializable.Initialize();

            var elympicsVarType = typeof(ElympicsVar);
            _backingFields = new List<ElympicsVar>();
            _backingFieldsNames = new Dictionary<ElympicsVar, string>();
            _backingFieldsByComponents = new List<(string, List<ElympicsVar>)>();
            foreach (var observable in _componentsContainer.Observables)
            {
                var componentVars = new List<ElympicsVar>();
                foreach (var field in observable.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (elympicsVarType.IsAssignableFrom(field.FieldType))
                    {
                        if (field.GetValue(observable) is ElympicsVar value)
                        {
                            using (ElympicsBase.SetTemporaryCallContext(ElympicsBase.CallContext.Initialize))
                                value.Initialize(elympicsBase);

                            if (value.EnabledSynchronization)
                            {
                                _backingFields.Add(value);
                                _backingFieldsNames.Add(value, field.Name);
                                componentVars.Add(value);
                            }
                        }
                        else
                            ElympicsLogger.LogError($"Cannot synchronize {nameof(ElympicsVar)} {field.Name} " + $"in {field.DeclaringType}, because it hasn't been initialized " + "(its value is null).");
                    }
                }
                if (componentVars.Count > 0)
                    _backingFieldsByComponents.Add((observable.GetType().Name, componentVars));
            }

            _inputReader = new BinaryInputReader();
            _tickBasedInputByPlayer = new Dictionary<ElympicsPlayer, (long Tick, byte[] Data)>();
        }

        private void OnDestroy()
        {
            _inputReader?.Dispose();
        }

        internal byte[] GetState()
        {
            foreach (var synchronizable in _componentsContainer.SerializationHandlers)
                synchronizable.OnPreStateSerialize();

            foreach (var backingField in _backingFields)
                backingField.Serialize(_binaryWriter1);

            var bytes = _memoryStream1.ToArray();
            _ = _memoryStream1.Seek(0, SeekOrigin.Begin);
            return bytes;
        }

        // returns name list of components and their ElympicsVars with values
        internal List<(string, List<(string, string)>)> GetStateMetadata()
        {
            var metadata = new List<(string, List<(string, string)>)>();
            foreach (var (componentName, vars) in _backingFieldsByComponents)
            {
                var varsWithValues = vars.Select(elympicsVar => (_backingFieldsNames[elympicsVar], elympicsVar.ToString())).ToList();

                metadata.Add((componentName, varsWithValues));
            }

            return metadata;
        }

        internal bool UpdateCurrentStateAndCheckIfSendCanBeSkipped(byte[] currentState, long tick)
        {
            return _behaviourStateChangeFrequencyCalculator.UpdateNextStateAndCheckIfSendCanBeSkipped(currentState, tick);
        }

        internal void ApplyState(byte[] data, bool ignoreTolerance = false)
        {
            _memoryStream1.Write(data, 0, data.Length);
            _ = _memoryStream1.Seek(0, SeekOrigin.Begin);
            foreach (var backingField in _backingFields)
                backingField.Deserialize(_binaryReader1, ignoreTolerance);
            _ = _memoryStream1.Seek(0, SeekOrigin.Begin);

            foreach (var synchronizable in _componentsContainer.SerializationHandlers)
                synchronizable.OnPostStateDeserialize();
        }

        internal bool AreStatesEqual(byte[] data1, byte[] data2, long tick)
        {
            _memoryStream1.Write(data1, 0, data1.Length);
            _ = _memoryStream1.Seek(0, SeekOrigin.Begin);
            _memoryStream2.Write(data2, 0, data2.Length);
            _ = _memoryStream2.Seek(0, SeekOrigin.Begin);

            // bool areEqual = _backingFields.All(backingField => backingField.Equals(_binaryReader1, _binaryReader2));
            // todo use in future for debug mode ~pprzestrzelski 06.06.2022
            var areEqual = true;
            foreach ((var componentName, var backingFields) in _backingFieldsByComponents)
            {
                foreach (var backingField in backingFields)
                {
                    if (!backingField.Equals(_binaryReader1, _binaryReader2, out var difference1, out var difference2))
                    {
                        if (!ElympicsBase.IsServer)
                            ElympicsLogger.LogWarning($"State not equal on field {_backingFieldsNames[backingField]} of {componentName} component attached to {gameObject.name} (network ID: {networkId}) in history tick {tick}. Last simulated tick: {Elympics.Tick}. State in history: {difference1} received state: {difference2}.", this);
                        areEqual = false;
                    }
                }
            }
            _ = _memoryStream1.Seek(0, SeekOrigin.Begin);
            _ = _memoryStream2.Seek(0, SeekOrigin.Begin);
            return areEqual;
        }

        internal void OnInputForClient(BinaryInputWriter inputWriter)
        {
            _componentsContainer.InputHandler?.OnInputForClient(inputWriter);
        }

        internal void OnInputForBot(BinaryInputWriter inputWriter)
        {
            _componentsContainer.InputHandler?.OnInputForBot(inputWriter);
        }

        internal void SetCurrentInput(ElympicsPlayer player, long inputTick, byte[] rawInput)
        {
            _tickBasedInputByPlayer[player] = (inputTick, rawInput);
        }

        internal void CommitVars()
        {
            using (ElympicsBase.SetTemporaryCallContext(ElympicsBase.CallContext.ValueChanged))
                foreach (var backingField in _backingFields)
                    backingField.Commit();
        }

        internal void ElympicsUpdate()
        {
            if (!isUpdatableForNonOwners
                && !IsPredictableTo(ElympicsBase.Player))
                return;

            using (ElympicsBase.SetTemporaryCallContext(ElympicsBase.CallContext.ElympicsUpdate))
                foreach (var updatable in _componentsContainer.Updatables)
                    try
                    {
                        updatable.ElympicsUpdate();
                    }
                    catch (Exception e) when (e is EndOfStreamException or ReadNotEnoughException)
                    {
                        _ = ElympicsLogger.LogException("An exception occured when applying inputs", e);
                    }
        }

        internal void OnRender(in RenderData data)
        {
            foreach (var render in _componentsContainer.Renderers)
                render.Render(data);
        }

        internal void OnPreReconcile()
        {
            _isReconciling = true;
            foreach (var reconciliationHandler in _componentsContainer.ReconciliationHandlers)
                reconciliationHandler.OnPreReconcile();
        }

        internal void OnPostReconcile()
        {
            foreach (var reconciliationHandler in _componentsContainer.ReconciliationHandlers)
                reconciliationHandler.OnPostReconcile();
            _isReconciling = false;
        }

        #region ClientCallbacks

        internal void InitializedByServer()
        {
            using (ElympicsBase.SetTemporaryCallContext(ElympicsBase.CallContext.Initialize))
                foreach (var initializable in _componentsContainer.Initializables)
                    initializable.InitializedByServer();
        }

        internal void OnStandaloneClientInit(InitialMatchPlayerDataGuid data)
        {
            foreach (var handler in _componentsContainer.ClientHandlersGuid)
                handler.OnStandaloneClientInit(data);
#pragma warning disable CS0618
            foreach (var handler in _componentsContainer.ClientHandlers)
                handler.OnStandaloneClientInit(new InitialMatchPlayerData(data));
#pragma warning restore CS0618
        }

        internal void OnClientsOnServerInit(InitialMatchPlayerDatasGuid data)
        {
            foreach (var handler in _componentsContainer.ClientHandlersGuid)
                handler.OnClientsOnServerInit(data);
#pragma warning disable CS0618
            foreach (var handler in _componentsContainer.ClientHandlers)
                handler.OnClientsOnServerInit(new InitialMatchPlayerDatas(data));
#pragma warning restore CS0618
        }

        internal void OnConnected(TimeSynchronizationData data)
        {
            foreach (var handler in _componentsContainer.ClientHandlersGuid)
                handler.OnConnected(data);
#pragma warning disable CS0618
            foreach (var handler in _componentsContainer.ClientHandlers)
                handler.OnConnected(data);
#pragma warning restore CS0618
        }

        internal void OnConnectingFailed()
        {
            foreach (var handler in _componentsContainer.ClientHandlersGuid)
                handler.OnConnectingFailed();
#pragma warning disable CS0618
            foreach (var handler in _componentsContainer.ClientHandlers)
                handler.OnConnectingFailed();
#pragma warning restore CS0618
        }

        internal void OnDisconnectedByServer()
        {
            foreach (var handler in _componentsContainer.ClientHandlersGuid)
                handler.OnDisconnectedByServer();
#pragma warning disable CS0618
            foreach (var handler in _componentsContainer.ClientHandlers)
                handler.OnDisconnectedByServer();
#pragma warning restore CS0618
        }

        internal void OnDisconnectedByClient()
        {
            foreach (var handler in _componentsContainer.ClientHandlersGuid)
                handler.OnDisconnectedByClient();
#pragma warning disable CS0618
            foreach (var handler in _componentsContainer.ClientHandlers)
                handler.OnDisconnectedByClient();
#pragma warning restore CS0618
        }

        internal void OnSynchronized(TimeSynchronizationData data)
        {
            foreach (var handler in _componentsContainer.ClientHandlersGuid)
                handler.OnSynchronized(data);
#pragma warning disable CS0618
            foreach (var handler in _componentsContainer.ClientHandlers)
                handler.OnSynchronized(data);
#pragma warning restore CS0618
        }

        internal void OnAuthenticated(Guid userId)
        {
            foreach (var handler in _componentsContainer.ClientHandlersGuid)
                handler.OnAuthenticated(userId);
            foreach (var handler in _componentsContainer.ClientHandlers)
                handler.OnAuthenticated(userId.ToString());
        }

        internal void OnAuthenticatedFailed(string errorMessage)
        {
            foreach (var handler in _componentsContainer.ClientHandlersGuid)
                handler.OnAuthenticatedFailed(errorMessage);
            foreach (var handler in _componentsContainer.ClientHandlers)
                handler.OnAuthenticatedFailed(errorMessage);
        }

        internal void OnMatchJoined(Guid matchId)
        {
            foreach (var handler in _componentsContainer.ClientHandlersGuid)
                handler.OnMatchJoined(matchId);
            foreach (var handler in _componentsContainer.ClientHandlers)
                handler.OnMatchJoined(matchId.ToString());
        }

        internal void OnMatchJoinedFailed(string errorMessage)
        {
            foreach (var handler in _componentsContainer.ClientHandlersGuid)
                handler.OnMatchJoinedFailed(errorMessage);
            foreach (var handler in _componentsContainer.ClientHandlers)
                handler.OnMatchJoinedFailed(errorMessage);
        }

        internal void OnMatchEnded(Guid matchId)
        {
            foreach (var handler in _componentsContainer.ClientHandlersGuid)
                handler.OnMatchEnded(matchId);
            foreach (var handler in _componentsContainer.ClientHandlers)
                handler.OnMatchEnded(matchId.ToString());
        }

        #endregion

        #region BotCallbacks

        internal void OnStandaloneBotInit(InitialMatchPlayerDataGuid initialMatchData)
        {
            foreach (var handler in _componentsContainer.BotHandlersGuid)
                handler.OnStandaloneBotInit(initialMatchData);
#pragma warning disable CS0618
            foreach (var handler in _componentsContainer.BotHandlers)
                handler.OnStandaloneBotInit(new InitialMatchPlayerData(initialMatchData));
#pragma warning restore CS0618
        }

        internal void OnBotsOnServerInit(InitialMatchPlayerDatasGuid initialMatchDatas)
        {
            foreach (var handler in _componentsContainer.BotHandlersGuid)
                handler.OnBotsOnServerInit(initialMatchDatas);
#pragma warning disable CS0618
            foreach (var handler in _componentsContainer.BotHandlers)
                handler.OnBotsOnServerInit(new InitialMatchPlayerDatas(initialMatchDatas));
#pragma warning restore CS0618
        }

        #endregion

        #region ServerCallbacks

        internal void OnServerInit(InitialMatchPlayerDatasGuid initialMatchData)
        {
            foreach (var handler in _componentsContainer.ServerHandlersGuid)
                handler.OnServerInit(initialMatchData);
#pragma warning disable CS0618
            foreach (var handler in _componentsContainer.ServerHandlers)
                handler.OnServerInit(new InitialMatchPlayerDatas(initialMatchData));
#pragma warning restore CS0618
        }

        internal void OnPlayerConnected(ElympicsPlayer player)
        {
            foreach (var handler in _componentsContainer.ServerHandlersGuid)
                handler.OnPlayerConnected(player);
#pragma warning disable CS0618
            foreach (var handler in _componentsContainer.ServerHandlers)
                handler.OnPlayerConnected(player);
#pragma warning restore CS0618
        }

        internal void OnPlayerDisconnected(ElympicsPlayer player)
        {
            foreach (var handler in _componentsContainer.ServerHandlersGuid)
                handler.OnPlayerDisconnected(player);
#pragma warning disable CS0618
            foreach (var handler in _componentsContainer.ServerHandlers)
                handler.OnPlayerDisconnected(player);
#pragma warning restore CS0618
        }

        #endregion

        public bool Equals(ElympicsBehaviour other) => other != null && networkId == other.networkId;
    }
}
