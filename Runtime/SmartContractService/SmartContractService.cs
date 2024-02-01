using System;
using System.Collections.Generic;
using System.Numerics;
using Cysharp.Threading.Tasks;
using Elympics;
using SCS.InternalModels.Player;
using UnityEngine;

#nullable enable

namespace SCS
{
    public class SmartContractService : MonoBehaviour, ISmartContractService
    {
        private const string ServiceName = "scs";

        internal static IScsWebRequest? ScsWebRequestOverride;

        [SerializeField] private SmartContractServiceConfig? config;
        private IRoomsManager? _roomsManager;
        private ElympicsLobbyClient? _lobby;
        private IWallet? _currentWallet;
        private IScsWebRequest _scsWebRequest = null!;
        private ElympicsGameConfig? _gameConfig;
        public ChainConfig? CurrentChain { get; private set; }

        private void Awake()
        {
            var elympicsConfig = ElympicsConfig.Load()
                ?? throw new InvalidOperationException($"No {nameof(ElympicsConfig)} instance found.");
            _gameConfig = elympicsConfig.GetCurrentGameConfig()
                ?? throw new InvalidOperationException($"No {nameof(ElympicsGameConfig)} instance found.");
            // ReSharper disable once Unity.NoNullCoalescing
            _lobby = ElympicsLobbyClient.Instance
                ?? throw new InvalidOperationException($"No {nameof(ElympicsLobbyClient)} instance found.");
            _scsWebRequest = ScsWebRequestOverride
                ?? new ScsWebRequest(_lobby, elympicsConfig.GetV2Endpoint(ServiceName).ToString());
            _roomsManager = _lobby.RoomsManager;
            if (config != null)
                CurrentChain = config.GetChainConfigForGameId(_gameConfig.GameId);

            _roomsManager.RoomSetUp += OnRoomSetUp;
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

        UniTask<GetTicketResponse> ISmartContractService.GetTicket(Guid roomId, BigInteger betAmount)
        {
            ThrowIfNoWallet();
            var ticketRequest = new GetTicketRequest(
                roomId.ToString(),
                _gameConfig!.GameId,
                Guid.Empty.ToString(),
                string.Empty,//TODO need to understand what is the purpose of this. For now empty string is ok. k.pieta 30.11.2023
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

        private void OnDestroy()
        {
            if (_roomsManager != null)
                _roomsManager.RoomSetUp -= OnRoomSetUp;
        }
    }
}
