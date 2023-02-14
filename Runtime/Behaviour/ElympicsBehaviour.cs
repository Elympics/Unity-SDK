using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

		[SerializeField] internal bool           forceNetworkId          = false;
		[SerializeField] internal int            networkId               = UndefinedNetworkId;
		[SerializeField] internal ElympicsPlayer predictableFor          = ElympicsPlayer.World;
		[SerializeField] internal bool           isUpdatableForNonOwners = false;
		[SerializeField] internal ElympicsPlayer visibleFor              = ElympicsPlayer.All;
		[SerializeField]
		internal ElympicsBehaviourStateChangeFrequencyStage[] stateFrequencyStages =
		{
			new ElympicsBehaviourStateChangeFrequencyStage(500, 30),
			new ElympicsBehaviourStateChangeFrequencyStage(1000, 200),
			new ElympicsBehaviourStateChangeFrequencyStage(1000, 1000)
		};

		private string _prefabName = null;

		private ElympicsComponentsContainer						_componentsContainer;
		private List<ElympicsVar>								_backingFields;
		private Dictionary<ElympicsVar, string>					_backingFieldsNames;
		private List<(string, List<ElympicsVar>)>				_backingFieldsByComponents;
		private ElympicsBehaviourStateChangeFrequencyCalculator	_behaviourStateChangeFrequencyCalculator;

		internal bool HasAnyState => _componentsContainer.Observables.Length > 0;
		internal bool HasAnyInput => _componentsContainer.InputHandler != null;

		public int NetworkId
		{
			get => networkId;
			internal set => networkId = value;
		}

		public string PrefabName
		{
			get => _prefabName;
			internal set => _prefabName = value;
		}

		public ElympicsPlayer PredictableFor
		{
			get => predictableFor;
			internal set => predictableFor = value;
		}

		internal ElympicsBase ElympicsBase { get; private set; }
		public   bool         IsPredictableTo(ElympicsPlayer player) => predictableFor == ElympicsPlayer.All || player == predictableFor || player == ElympicsPlayer.World;
		public   bool         IsOwnedBy(ElympicsPlayer player) => IsPredictableTo(player);
		internal bool         IsVisibleTo(ElympicsPlayer player) => visibleFor == ElympicsPlayer.All || player == visibleFor || player == ElympicsPlayer.World;

		private MemoryStream      _memoryStream1;
		private MemoryStream      _memoryStream2;
		private BinaryReader      _binaryReader1;
		private BinaryReader      _binaryReader2;
		private BinaryWriter      _binaryWriter1;
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
		/// <param name="inputDeserializer">Input deserializer. Use its <c>Read</c> methods to parse data from the received input.</param>
		/// <param name="absenceTick"> How many ticks will be predicted due to lack of input from player.</param>
		/// <returns>If there is any input to retrieve for the given player.</returns>
		/// <seealso cref="IInputHandler.OnInputForClient"/>
		/// <seealso cref="IInputHandler.OnInputForBot"/>
		public bool TryGetInput(ElympicsPlayer player, out IInputReader inputReader, int absenceTick = DefaultAbsenceTickParameter)
		{
			if (ElympicsBase.CurrentCallContext != ElympicsBase.CallContext.ElympicsUpdate)
				throw new ElympicsException($"You cannot use {nameof(TryGetInput)} outside of {nameof(ElympicsBase.elympicsBehavioursManager.ElympicsUpdate)}");
			if (!HasAnyInput)
				throw new ElympicsException($"{nameof(TryGetInput)} can be called only in classes implementing {nameof(IInputHandler)} interface");
			if (!_inputReader.AllBytesRead())
				throw new ReadNotEnoughException(this);

			inputReader = null;
			if (_tickBasedInputByPlayer.TryGetValue(player, out var tickBasedInput) && ElympicsBase.Tick - tickBasedInput.Tick <= absenceTick)
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
            if (!forceNetworkId && (_previousForceNetworkIdState || networkId == UndefinedNetworkId))
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
            return FindObjectsOfType<ElympicsBehaviour>()
                .Where(behaviour => behaviour != this)
                .Select(behaviour => behaviour.NetworkId)
                .Contains(networkId);
        }

        internal void UpdateSerializedNetworkId()
        {
            networkId = NetworkIdEnumerator.Instance.MoveNextAndGetCurrent();
            EditorUtility.SetDirty(this);
        }

        private void OnDrawGizmos()
        {
        }
#endif

		internal void InitializeInternal(ElympicsBase elympicsBase)
		{
			_memoryStream1 = new MemoryStream();
			_memoryStream2 = new MemoryStream();
			_binaryReader1 = new BinaryReader(_memoryStream1);
			_binaryReader2 = new BinaryReader(_memoryStream2);
			_binaryWriter1 = new BinaryWriter(_memoryStream1);

			ElympicsBase = elympicsBase;

			_behaviourStateChangeFrequencyCalculator = new ElympicsBehaviourStateChangeFrequencyCalculator(stateFrequencyStages, AreStatesEqual);

			_componentsContainer = new ElympicsComponentsContainer(this);

			var previousCallContext = ElympicsBase.CurrentCallContext;
			ElympicsBase.CurrentCallContext = ElympicsBase.CallContext.Initialize;
			foreach (var initializable in _componentsContainer.Initializables)
				initializable.Initialize();
			ElympicsBase.CurrentCallContext = previousCallContext;

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
						var value = field.GetValue(observable) as ElympicsVar;
						if (value != null)
						{
							ElympicsBase.CurrentCallContext = ElympicsBase.CallContext.Initialize;
							value.Initialize(elympicsBase);
							ElympicsBase.CurrentCallContext = previousCallContext;

							if (value.EnabledSynchronization)
							{
								_backingFields.Add(value);
								_backingFieldsNames.Add(value, field.Name);
								componentVars.Add(value);
							}
						}
						else
							Debug.LogError($"Cannot synchronize ElympicsVar {field.Name} in {field.DeclaringType}, because it's null");
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
			_memoryStream1.Seek(0, SeekOrigin.Begin);
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

		internal bool UpdateCurrentStateAndCheckIfSendCanBeSkipped(byte[] currentState)
		{
			return _behaviourStateChangeFrequencyCalculator.UpdateNextStateAndCheckIfSendCanBeSkipped(currentState);
		}

		internal void ApplyState(byte[] data, bool ignoreTolerance = false)
		{
			_memoryStream1.Write(data, 0, data.Length);
			_memoryStream1.Seek(0, SeekOrigin.Begin);
			foreach (var backingField in _backingFields)
				backingField.Deserialize(_binaryReader1, ignoreTolerance);
			_memoryStream1.Seek(0, SeekOrigin.Begin);

			foreach (var synchronizable in _componentsContainer.SerializationHandlers)
				synchronizable.OnPostStateDeserialize();
		}

		internal bool AreStatesEqual(byte[] data1, byte[] data2)
		{
			_memoryStream1.Write(data1, 0, data1.Length);
			_memoryStream1.Seek(0, SeekOrigin.Begin);
			_memoryStream2.Write(data2, 0, data2.Length);
			_memoryStream2.Seek(0, SeekOrigin.Begin);

			// bool areEqual = _backingFields.All(backingField => backingField.Equals(_binaryReader1, _binaryReader2));
			// todo use in future for debug mode ~pprzestrzelski 06.06.2022
			var areEqual = true;
			foreach (var backingField in _backingFields)
			{
				if (!backingField.Equals(_binaryReader1, _binaryReader2))
				{
					if (!ElympicsBase.IsServer)
						Debug.LogWarning($"State not equal on field {_backingFieldsNames[backingField]}", this);
					areEqual = false;
				}
			}
			_memoryStream1.Seek(0, SeekOrigin.Begin);
			_memoryStream2.Seek(0, SeekOrigin.Begin);
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
			foreach (var backingField in _backingFields)
				backingField.Commit();
		}

		internal void ElympicsUpdate()
		{
			if (!isUpdatableForNonOwners && !IsPredictableTo(ElympicsBase.Player))
				return;

			var previousCallContext = ElympicsBase.CurrentCallContext;
			foreach (var updatable in _componentsContainer.Updatables)
				try
				{
					ElympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
					updatable.ElympicsUpdate();
				}
				catch (Exception e) when (e is EndOfStreamException || e is ReadNotEnoughException)
				{
					Debug.LogException(e);
					Debug.LogError("An exception occured when applying inputs. This might be a result of faulty code or a hacking attempt.");
				}
				finally
				{
					ElympicsBase.CurrentCallContext = previousCallContext;
				}
		}

		internal void OnPreReconcile()
		{
			foreach (var reconciliationHandler in _componentsContainer.ReconciliationHandlers)
				reconciliationHandler.OnPreReconcile();
		}

		internal void OnPostReconcile()
		{
			foreach (var reconciliationHandler in _componentsContainer.ReconciliationHandlers)
				reconciliationHandler.OnPostReconcile();
		}

		#region ClientCallbacks

		internal void OnStandaloneClientInit(InitialMatchPlayerData data)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnStandaloneClientInit(data);
		}

		internal void OnClientsOnServerInit(InitialMatchPlayerDatas data)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnClientsOnServerInit(data);
		}

		internal void OnConnected(TimeSynchronizationData data)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnConnected(data);
		}

		internal void OnConnectingFailed()
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnConnectingFailed();
		}

		internal void OnDisconnectedByServer()
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnDisconnectedByServer();
		}

		internal void OnDisconnectedByClient()
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnDisconnectedByClient();
		}

		internal void OnSynchronized(TimeSynchronizationData data)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnSynchronized(data);
		}

		internal void OnAuthenticated(string userId)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnAuthenticated(userId);
		}

		internal void OnAuthenticatedFailed(string errorMessage)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnAuthenticatedFailed(errorMessage);
		}

		internal void OnMatchJoined(string matchId)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnMatchJoined(matchId);
		}

		internal void OnMatchJoinedFailed(string errorMessage)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnMatchJoinedFailed(errorMessage);
		}

		internal void OnMatchEnded(string matchId)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnMatchEnded(matchId);
		}

		#endregion

		#region BotCallbacks

		internal void OnStandaloneBotInit(InitialMatchPlayerData initialMatchData)
		{
			foreach (var handler in _componentsContainer.BotHandlers)
				handler.OnStandaloneBotInit(initialMatchData);
		}

		internal void OnBotsOnServerInit(InitialMatchPlayerDatas initialMatchDatas)
		{
			foreach (var handler in _componentsContainer.BotHandlers)
				handler.OnBotsOnServerInit(initialMatchDatas);
		}

		#endregion

		#region ServerCallbacks

		internal void OnServerInit(InitialMatchPlayerDatas initialMatchData)
		{
			foreach (var handler in _componentsContainer.ServerHandlers)
				handler.OnServerInit(initialMatchData);
		}

		internal void OnPlayerConnected(ElympicsPlayer player)
		{
			foreach (var handler in _componentsContainer.ServerHandlers)
				handler.OnPlayerConnected(player);
		}

		internal void OnPlayerDisconnected(ElympicsPlayer player)
		{
			foreach (var handler in _componentsContainer.ServerHandlers)
				handler.OnPlayerDisconnected(player);
		}

		#endregion

		public bool Equals(ElympicsBehaviour other)
		{
			if (other != null)
			{
				return this.networkId == other.networkId;
			}

			return false;
		}
	}
}
