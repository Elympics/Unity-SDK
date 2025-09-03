using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using Elympics.Communication.Rooms.PublicModels;
using Elympics.Rooms.Models;

#nullable enable

namespace SCS
{
    internal class SmartContractRoomAdapter : IRoom
    {
        private readonly ISmartContractService _scsService;
        private readonly IRoom _room;

        private SmartContractRoomAdapter(IRoom room, ISmartContractService scsService)
        {
            _scsService = scsService;
            _room = room;
        }
        public static SmartContractRoomAdapter CreateInstance(IRoom room, ISmartContractService scsService) => new(room, scsService);

        public Guid RoomId => _room.RoomId;
        public RoomState State => _room.State;
        public bool IsDisposed => _room.IsDisposed;
        public bool IsJoined => _room.IsJoined;
        bool IRoom.IsJoined
        {
            get => IsJoined;
            set => _room.IsJoined = value;
        }

        public bool HasMatchmakingEnabled => _room.HasMatchmakingEnabled;
        public bool IsMatchAvailable => _room.IsMatchAvailable;
        public UniTask UpdateRoomParams(string? roomName = null, bool? isPrivate = null, IReadOnlyDictionary<string, string>? roomCustomData = null, IReadOnlyDictionary<string, string>? customMatchmakingData = null, CompetitivenessConfig? competitivenessConfig = null)
        {
            if (_scsService.CurrentChain is not null && customMatchmakingData is not null && !customMatchmakingData.ContainsKey(SmartContractServiceMatchMakingCustomData.BetAmountKey))
                throw new SmartContractServiceException($"New customData has to contains BetAmount key <color=red>{SmartContractServiceMatchMakingCustomData.BetAmountKey}</color>");

            return _room.UpdateRoomParams(roomName, isPrivate, roomCustomData, customMatchmakingData, competitivenessConfig);
        }

        public UniTask UpdateCustomPlayerData(Dictionary<string, string>? customPlayerData) => _room.UpdateCustomPlayerData(customPlayerData);

        public UniTask ChangeTeam(uint? teamIndex) => _room.ChangeTeam(teamIndex);
        public async UniTask MarkYourselfReady(byte[]? gameEngineData, float[]? matchmakerData, CancellationToken ct = default)
        {
            if (_scsService.CurrentChain == null || (_room.State.MatchmakingData!.CustomData.TryGetValue(SmartContractServiceMatchMakingCustomData.BetAmountKey, out var betValue) && betValue == "0") || _room.IsQuickMatch)
            {
                await _room.MarkYourselfReady(gameEngineData, matchmakerData);
                return;
            }

            //TODO: SmartContractService has to be aware relation between queue and SmartContractDeploymentId (check Lobby->MatchmakerGameBinModel).
            //Then remove this quickMatch check and check for queues. k.pieta 27.05.2024. For now we depend on backend checks.

            if (!_room.State.MatchmakingData!.CustomData.ContainsKey(SmartContractServiceMatchMakingCustomData.BetAmountKey))
                throw new SmartContractServiceException($"Matchmaking CustomData does not contains BetAmount key <color=red>{SmartContractServiceMatchMakingCustomData.BetAmountKey}</color>");

            if (!BigInteger.TryParse(_room.State.MatchmakingData!.CustomData[SmartContractServiceMatchMakingCustomData.BetAmountKey], out var betAmount))
                throw new SmartContractServiceException($"Could not parse betAmount to valid BigInteger value {_room.State.MatchmakingData.CustomData[SmartContractServiceMatchMakingCustomData.BetAmountKey]}");

            if (!_room.State.CustomData.TryGetValue(SmartContractServiceRoomCustomData.GameDataKey, out var gameData))
                gameData = string.Empty;
            var getTicket = await _scsService.GetTicket(_room.RoomId, betAmount, gameData);

            ct.ThrowIfCancellationRequested();
            var signedMessage = await _scsService.SignTypedDataMessage(getTicket.TypedData);
            ct.ThrowIfCancellationRequested();
            await _scsService.SendSignedTicket(getTicket.Nonce, signedMessage);
            ct.ThrowIfCancellationRequested();

            var setPlayerReadyResult = await _scsService.SetPlayerReady(_room.RoomId, betAmount);
            if (setPlayerReadyResult.Allow)
            {
                await _room.MarkYourselfReady(gameEngineData, matchmakerData);
                return;
            }

            ct.ThrowIfCancellationRequested();
            if (setPlayerReadyResult.TransactionsToSign != null)
                foreach (var transactionToSign in setPlayerReadyResult.TransactionsToSign)
                {
                    var depositAllowanceSignature = await _scsService.SetAllowance(transactionToSign.From, transactionToSign.To, transactionToSign.Data);
                    Utils.ThrowIfAllowanceNotSigned(depositAllowanceSignature);
                    ct.ThrowIfCancellationRequested();
                }
            setPlayerReadyResult = await _scsService.SetPlayerReady(_room.RoomId, betAmount);
            if (!setPlayerReadyResult.Allow)
                throw new SmartContractServiceException(setPlayerReadyResult.RejectReason);

            await _room.MarkYourselfReady(gameEngineData, matchmakerData);
        }
        public UniTask MarkYourselfUnready() => _room.MarkYourselfUnready();
        public UniTask StartMatchmaking() => _room.StartMatchmaking();
        public UniTask CancelMatchmaking(CancellationToken ct = default) => _room.CancelMatchmaking(ct);
        public void PlayAvailableMatch() => _room.PlayAvailableMatch();
        public UniTask Leave() => _room.Leave();
        void IRoom.UpdateState(RoomStateChanged roomState, in RoomStateDiff stateDiff) => _room.UpdateState(roomState, in stateDiff);
        void IRoom.UpdateState(PublicRoomState roomState) => _room.UpdateState(roomState);
        bool IRoom.IsQuickMatch => _room.IsQuickMatch;
        UniTask IRoom.StartMatchmakingInternal() => _room.StartMatchmakingInternal();
        public void Dispose() => _room.Dispose();
    }
}
