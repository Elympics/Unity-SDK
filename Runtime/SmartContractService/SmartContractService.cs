using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using SCS.InternalModels.Player;
using UnityEngine;

#nullable enable

namespace SCS
{
    [DefaultExecutionOrder(ElympicsExecutionOrders.SmartContractService)]
    public class SmartContractService : MonoBehaviour, ISmartContractService
    {
        private const string ServiceName = "scs";

        private IRoomsManager? _roomsManager;
        private ElympicsLobbyClient? _lobby;
        private IWallet? _currentWallet;
        private IScsWebRequest _scsWebRequest = null!;
        private ElympicsGameConfig? _gameConfig;
        public ChainConfig? CurrentChain => ThrowIfDisposedOrReturnChainConfig();
        private ChainConfig? _currentChain;
        private bool Initilized => _currentChain is not null;
        private TimeSpan _getChainTimeout = TimeSpan.FromSeconds(15);
        private CancellationTokenSource? _cts = null;
        public static SmartContractService Instance { get; private set; }
        private void Awake()
        {
            if (Instance != null)
            {
                ElympicsLogger.LogError("SmartContractService instance already exists. Destroying object.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this);


            var elympicsConfig = ElympicsConfig.Load()
                ?? throw new InvalidOperationException($"No {nameof(ElympicsConfig)} instance found.");
            _gameConfig = elympicsConfig.GetCurrentGameConfig()
                ?? throw new InvalidOperationException($"No {nameof(ElympicsGameConfig)} instance found.");
            // ReSharper disable once Unity.NoNullCoalescing
            _lobby = ElympicsLobbyClient.Instance
                ?? throw new InvalidOperationException($"No {nameof(ElympicsLobbyClient)} instance found.");

            _scsWebRequest = new ScsWebRequest(_lobby, elympicsConfig.GetV2Endpoint(ServiceName).ToString());

            _roomsManager = _lobby.RoomsManager;
            _roomsManager.RoomSetUp += OnRoomSetUp;
        }
        public async UniTask Initialize()
        {
            if (Initilized)
            {
                ElympicsLogger.LogWarning("Chain config has already been initialized.");
                return;
            }
            if (_cts is not null)
            {
                ElympicsLogger.LogWarning("Chain config is currently being downloaded.");
                return;
            }
            try
            {
                _cts = new CancellationTokenSource(_getChainTimeout);
                var configResponse = await _scsWebRequest.GetChainConfig(_gameConfig!.GameId, _cts.Token);

                _currentChain = new ChainConfig
                {
                    ChainId = configResponse.ChainId.ToString(),
                    ChainName = configResponse.ChainName,
                    NativeCurrencyName = configResponse.NativeCurrencyName,
                    NativeCurrencySymbol = configResponse.NativeCurrencySymbol,
                    NativeCurrencyDecimals = configResponse.NativeCurrencyDecimals,
                    PublicRpcUrl = configResponse.PublicRpcUrl,
                    Contracts = configResponse.SmartContracts.ConvertAll(x => x.Map())
                };
            }
            catch (ElympicsException exception)
            {
                _ = ElympicsLogger.LogException(exception);
            }
            finally
            {
                _cts = null;
            }
        }
        public UniTask<GetConfigResponse> GetChainConfigForGame(string gameId) => _scsWebRequest.GetChainConfig(gameId);
        public void RegisterWallet(IWallet wallet) => _currentWallet = wallet;

        public UniTask<IReadOnlyList<FinalizedTransaction>> GetUserTransactionsList(string? gameId, int limit = 5) => _scsWebRequest.GetUserTransactionList(gameId, limit);
        public UniTask<IReadOnlyList<DepositState>> GetDepositState(string gameId) => _scsWebRequest.GetDepositStates(gameId);

        public async UniTask AddDeposit(string gameId, BigInteger amount)
        {
            ThrowIfNoWallet();
            var addRequest = new AddDepositRequest
            {
                GameId = gameId,
                Amount = amount.ToString()
            };
            var transactionsToSign = await _scsWebRequest.AddDeposit(addRequest);
            if (transactionsToSign != null)
            {
                foreach (var transaction in transactionsToSign)
                {
                    var result = await _currentWallet!.SendTransaction(new SendTransactionWalletRequest(transaction.From, transaction.To, transaction.Data));
                    Utils.ThrowIfAllowanceNotSigned(result);
                }
            }
        }

        #region Internal Implementation

        UniTask<string> ISmartContractService.SetAllowance(string from, string to, string data)
        {
            ThrowIfNoWallet();
            return _currentWallet!.SendTransaction(new SendTransactionWalletRequest
            {
                From = from,
                To = to,
                Data = data
            });
        }

        //todo change Guid.Empty to true value of game version id ~pzdanowski 05.01.2024
        UniTask<SetPlayerReadyResponse> ISmartContractService.SetPlayerReady(Guid roomId, BigInteger betAmount) => _scsWebRequest.SetPlayerReady(roomId, betAmount, new Guid(_gameConfig!.gameId), Guid.Empty);

        UniTask<GetTicketResponse> ISmartContractService.GetTicket(Guid roomId, BigInteger betAmount, string gameData)
        {
            ThrowIfNoWallet();
            var ticketRequest = new GetTicketRequest(
                roomId.ToString(),
                _gameConfig!.GameId,
                _gameConfig.GameName,
                _gameConfig.GameVersion,
                Guid.Empty.ToString(),
                gameData,
                betAmount.ToString());
            return _scsWebRequest.GetTicket(ticketRequest);
        }

        UniTask ISmartContractService.SendSignedTicket(string nonce, string signedMessage)
        {
            var request = new SendSignedTicketRequest
            {
                Nonce = nonce,
                Signature = signedMessage
            };
            return _scsWebRequest.SendSignedTicket(request);
        }

        UniTask<string> ISmartContractService.SignTypedDataMessage(string messageToSign)
        {
            ThrowIfNoWallet();
            return _currentWallet!.SignTypedDataV4(messageToSign);
        }

        #endregion
        private IRoom OnRoomSetUp(IRoom room) => SmartContractRoomAdapter.CreateInstance(room, this);

        private void ThrowIfNoWallet()
        {
            if (_currentWallet == null)
                throw new SmartContractServiceException("Please register Wallet to Smart Contract Service");
        }
        private ChainConfig ThrowIfDisposedOrReturnChainConfig()
        {
            ThrowIfNotInitilized();
            return _currentChain!.Value;
        }
        private void ThrowIfNotInitilized()
        {
            if (!Initilized)
                throw new SmartContractServiceException(
                    "Smart contract service not initilized.\n " +
                    $"Please use the {nameof(Initialize)} method or ensure that the game ID is paired with the ChainConfig.");
        }
        private void OnDestroy()
        {
            if (_roomsManager != null)
                _roomsManager.RoomSetUp -= OnRoomSetUp;
        }
    }
}
