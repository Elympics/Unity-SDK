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

		[SerializeField] internal bool           forceNetworkId = false;
		[SerializeField] internal int            networkId      = UndefinedNetworkId;
		[SerializeField] internal ElympicsPlayer predictableFor = ElympicsPlayer.World;
		[SerializeField] internal ElympicsPlayer visibleFor     = ElympicsPlayer.All;
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
		internal bool HasAnyInput => _componentsContainer.InputHandlers.Length > 0;

		public int NetworkId
		{
			get => networkId;
			internal set => networkId = value;
		}

		internal ElympicsBase ElympicsBase { get; private set; }
		public   bool         IsPredictableTo(ElympicsPlayer player) => predictableFor == ElympicsPlayer.All || player == predictableFor || player == ElympicsPlayer.World;
		internal bool         IsVisibleTo(ElympicsPlayer player)     => visibleFor == ElympicsPlayer.All || player == visibleFor || player == ElympicsPlayer.World;

		private MemoryStream _memoryStream1;
		private MemoryStream _memoryStream2;
		private BinaryReader _binaryReader1;
		private BinaryReader _binaryReader2;
		private BinaryWriter _binaryWriter1;

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

			foreach (var initializable in _componentsContainer.Initializables)
				initializable.Initialize();

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
							value.Initialize(elympicsBase);

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
			_memoryStream1.Write(data);
			_memoryStream1.Seek(0, SeekOrigin.Begin);
			foreach (var backingField in _backingFields)
				backingField.Deserialize(_binaryReader1, ignoreTolerance);
			_memoryStream1.Seek(0, SeekOrigin.Begin);

			foreach (var synchronizable in _componentsContainer.SerializationHandlers)
				synchronizable.OnPostStateDeserialize();
		}

		internal bool AreStatesEqual(byte[] data1, byte[] data2)
		{
			_memoryStream1.Write(data1);
			_memoryStream1.Seek(0, SeekOrigin.Begin);
			_memoryStream2.Write(data2);
			_memoryStream2.Seek(0, SeekOrigin.Begin);

			bool areEqual = _backingFields.All(backingField => backingField.Equals(_binaryReader1, _binaryReader2));
			_memoryStream1.Seek(0, SeekOrigin.Begin);
			_memoryStream2.Seek(0, SeekOrigin.Begin);
			return areEqual;
		}

		internal void GetInputForClient(BinaryInputWriter inputWriter)
		{
			foreach (var handler in _componentsContainer.InputHandlers)
				handler.GetInputForClient(inputWriter);
		}

		internal void GetInputForBot(BinaryInputWriter inputWriter)
		{
			foreach (var handler in _componentsContainer.InputHandlers)
				handler.GetInputForBot(inputWriter);
		}

		internal void ApplyInput(ElympicsPlayer player, BinaryInputReader inputReader)
		{
			foreach (var handler in _componentsContainer.InputHandlers)
				handler.ApplyInput(player, inputReader);
		}

		internal void ElympicsUpdate()
		{
			foreach (var predictable in _componentsContainer.Updatables)
				predictable.ElympicsUpdate();
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
	}
}
