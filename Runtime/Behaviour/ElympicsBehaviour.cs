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

		internal static CallContext CurrentCallContext { get; private set; } = CallContext.None;

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

		private ElympicsComponentsContainer                     _componentsContainer;
		private List<ElympicsVar>                               _backingFields;
		private Dictionary<ElympicsVar, string>                 _backingFieldsNames;
		private ElympicsBehaviourStateChangeFrequencyCalculator _behaviourStateChangeFrequencyCalculator;

		internal bool HasAnyState => _componentsContainer.Observables.Length > 0;
		internal bool HasAnyInput => _componentsContainer.InputHandler != null;

		public int NetworkId
		{
			get => networkId;
			internal set => networkId = value;
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

		private Dictionary<ElympicsPlayer, byte[]> _inputByPlayer;
		internal void ClearInputs()
        {
			_inputByPlayer.Clear();
        }

		/// <summary>
		/// Retrieves received input for a player.
		/// </summary>
		/// <param name="player">Identifier of a player that the input is retrieved for.</param>
		/// <param name="inputDeserializer">Input deserializer. Use its <c>Read</c> methods to parse data from the received input.</param>
		/// <returns>If there is any input to retrieve for the given player.</returns>
		/// <seealso cref="IInputHandler.OnInputForClient"/>
		/// <seealso cref="IInputHandler.OnInputForBot"/>
		public bool TryGetInput(ElympicsPlayer player, out IInputReader inputReader)
		{
			if (CurrentCallContext != CallContext.ElympicsUpdate)
				throw new ElympicsException($"You cannot use {nameof(TryGetInput)} outside of {nameof(ElympicsBase.elympicsBehavioursManager.ElympicsUpdate)}");
			if (!HasAnyInput)
				throw new ElympicsException($"{nameof(TryGetInput)} can be called only in classes implementing {nameof(IInputHandler)} interface");
			if (!_inputReader.AllBytesRead())
				throw new ReadNotEnoughException(this);

			inputReader = null;
			if (!_inputByPlayer.ContainsKey(player))
				return false;
			_inputReader.FeedDataForReading(_inputByPlayer[player]);
			inputReader = _inputReader;
			return true;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (!forceNetworkId && networkId == UndefinedNetworkId)
				UpdateSerializedNetworkId();

			_behaviourStateChangeFrequencyCalculator?.ResetStateUpdateFrequencyStage();
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

			CurrentCallContext = CallContext.Initialize;
			foreach (var initializable in _componentsContainer.Initializables)
				initializable.Initialize();
			CurrentCallContext = CallContext.None;

			var elympicsVarType = typeof(ElympicsVar);
			_backingFields = new List<ElympicsVar>();
			_backingFieldsNames = new Dictionary<ElympicsVar, string>();
			foreach (var observable in _componentsContainer.Observables)
			{
				foreach (var field in observable.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
				{
					if (elympicsVarType.IsAssignableFrom(field.FieldType))
					{
						var value = field.GetValue(observable) as ElympicsVar;
						if (value != null)
						{
							CurrentCallContext = CallContext.Initialize;
							value.Initialize(elympicsBase);
							CurrentCallContext = CallContext.None;

							if (value.EnabledSynchronization)
							{
								_backingFields.Add(value);
								_backingFieldsNames.Add(value, field.Name);
							}
						}
						else
							Debug.LogError($"Cannot synchronize ElympicsVar {field.Name} in {field.DeclaringType}, because it's null");
					}
				}
			}

			_inputReader = new BinaryInputReader();
			_inputByPlayer = new Dictionary<ElympicsPlayer, byte[]>();
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

		internal void SetCurrentInput(ElympicsPlayer player, byte[] rawInput)
		{
			_inputByPlayer[player] = rawInput;
		}

		internal void ElympicsUpdate()
		{
			if (!isUpdatableForNonOwners && !IsPredictableTo(ElympicsBase.Player))
				return;

			foreach (var updatable in _componentsContainer.Updatables)
				try
				{
					CurrentCallContext = CallContext.ElympicsUpdate;
					updatable.ElympicsUpdate();
				}
				catch (Exception e) when (e is EndOfStreamException || e is ReadNotEnoughException)
                {
					Debug.LogException(e);
					Debug.LogError("An exception occured when applying inputs. This might be a result of faulty code or a hacking attempt.");
				}
				finally
				{
					CurrentCallContext = CallContext.None;
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

		public void OnStandaloneClientInit(InitialMatchPlayerData data)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnStandaloneClientInit(data);
		}

		public void OnClientsOnServerInit(InitialMatchPlayerDatas data)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnClientsOnServerInit(data);
		}

		public void OnConnected(TimeSynchronizationData data)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnConnected(data);
		}

		public void OnConnectingFailed()
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnConnectingFailed();
		}

		public void OnDisconnectedByServer()
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnDisconnectedByServer();
		}

		public void OnDisconnectedByClient()
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnDisconnectedByClient();
		}

		public void OnSynchronized(TimeSynchronizationData data)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnSynchronized(data);
		}

		public void OnAuthenticated(string userId)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnAuthenticated(userId);
		}

		public void OnAuthenticatedFailed(string errorMessage)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnAuthenticatedFailed(errorMessage);
		}

		public void OnMatchJoined(string matchId)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnMatchJoined(matchId);
		}

		public void OnMatchJoinedFailed(string errorMessage)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnMatchJoinedFailed(errorMessage);
		}

		public void OnMatchEnded(string matchId)
		{
			foreach (var handler in _componentsContainer.ClientHandlers)
				handler.OnMatchEnded(matchId);
		}

		#endregion

		#region BotCallbacks

		public void OnStandaloneBotInit(InitialMatchPlayerData initialMatchData)
		{
			foreach (var handler in _componentsContainer.BotHandlers)
				handler.OnStandaloneBotInit(initialMatchData);
		}

		public void OnBotsOnServerInit(InitialMatchPlayerDatas initialMatchDatas)
		{
			foreach (var handler in _componentsContainer.BotHandlers)
				handler.OnBotsOnServerInit(initialMatchDatas);
		}

		#endregion

		#region ServerCallbacks

		public void OnServerInit(InitialMatchPlayerDatas initialMatchData)
		{
			foreach (var handler in _componentsContainer.ServerHandlers)
				handler.OnServerInit(initialMatchData);
		}

		public void OnPlayerConnected(ElympicsPlayer player)
		{
			foreach (var handler in _componentsContainer.ServerHandlers)
				handler.OnPlayerConnected(player);
		}

		public void OnPlayerDisconnected(ElympicsPlayer player)
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

		internal enum CallContext
		{
			None,
			ElympicsUpdate,
			Initialize
		}
	}
}
