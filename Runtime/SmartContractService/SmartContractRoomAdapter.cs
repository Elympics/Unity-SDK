using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
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
        bool IRoom.IsJoined
        {
            get => _room.IsJoined;
            set => _room.IsJoined = value;
        }
        public bool HasMatchmakingEnabled => _room.HasMatchmakingEnabled;
        public bool IsMatchAvailable => _room.IsMatchAvailable;
        public UniTask UpdateRoomParams(string? roomName = null, bool? isPrivate = null, IReadOnlyDictionary<string, string>? roomCustomData = null, IReadOnlyDictionary<string, string>? customMatchmakingData = null)
        {
            if (_scsService.CurrentChain is not null && customMatchmakingData is not null && !customMatchmakingData.ContainsKey(SmartContractServiceMatchMakingCustomData.BetAmountKey))
                throw new SmartContractServiceException($"New customData has to contains BetAmount key <color=red>{SmartContractServiceMatchMakingCustomData.BetAmountKey}</color>");

            return _room.UpdateRoomParams(roomName, isPrivate, roomCustomData, customMatchmakingData);
        }

        public UniTask ChangeTeam(uint? teamIndex) => _room.ChangeTeam(teamIndex);
        public async UniTask MarkYourselfReady(byte[]? gameEngineData, float[]? matchmakerData, CancellationToken ct = default)
        {
            if (_scsService.CurrentChain == null || (_room.State.MatchmakingData!.CustomData.TryGetValue(SmartContractServiceMatchMakingCustomData.BetAmountKey, out var betValue) && betValue == "0"))
            {
                await _room.MarkYourselfReady(gameEngineData, matchmakerData);
                return;
            }

            if (!_room.State.MatchmakingData!.CustomData.ContainsKey(SmartContractServiceMatchMakingCustomData.BetAmountKey))
                throw new SmartContractServiceException($"Matchmaking CustomData does not contains BetAmount key <color=red>{SmartContractServiceMatchMakingCustomData.BetAmountKey}</color>");

            if (!BigInteger.TryParse(_room.State.MatchmakingData!.CustomData[SmartContractServiceMatchMakingCustomData.BetAmountKey], out var betAmount))
                throw new SmartContractServiceException($"Could not parse betAmount to valid BigInteger value {_room.State.MatchmakingData.CustomData[SmartContractServiceMatchMakingCustomData.BetAmountKey]}");

            var getTicket = await _scsService.GetTicket(_room.RoomId, betAmount);
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
        public void Dispose() => _room.Dispose();
    }
}