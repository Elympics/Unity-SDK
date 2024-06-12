using System;
using System.Numerics;
using Elympics;
using UnityEngine;

#nullable enable

namespace SCS.Tests
{
    [RequireComponent(typeof(SmartContractService))]
    public class ScsClientTest : MonoBehaviour
    {
        private ISmartContractService _scs = default!;
        private IRoomsManager _roomsManager = default!;


        [SerializeField] private string gameIdForChainConfig;
        private void Start()
        {
            _scs = GetComponent<SmartContractService>();
            var wallet = GetComponent<IWallet>();
            _scs.RegisterWallet(wallet);
            _roomsManager = GetComponent<ElympicsLobbyClient>().RoomsManager;
        }

        public async void GetTicketRequest()
        {
            try
            {
                if (_roomsManager.ListJoinedRooms().Count > 0)
                {
                    var guid = _roomsManager.ListJoinedRooms()[0].RoomId;
                    var amount = BigInteger.Parse("1000000000000000000000000");
                    var result = await _scs.GetTicket(guid, amount);
                    Debug.Log($"[ScsClientTest] Got Ticket {result.TypedData}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Message:{e.Message} {Environment.NewLine} Stack: {e.StackTrace}");
            }
        }

        public async void GetAndSignTicketRequest()
        {
            try
            {
                var roomId = Guid.NewGuid();
                Debug.Log($"RoomId: {roomId}");
                var amount = new BigInteger(100);
                var result = await _scs.GetTicket(roomId, amount);
                Debug.Log($"[ScsClientTest] Got Ticket {result.TypedData}");
                var signature = await _scs.SignTypedDataMessage(result.TypedData);
                Debug.Log($"[ScsClientTest] Got Signature {signature}");
                await _scs.SendSignedTicket(result.Nonce, signature);
            }
            catch (Exception e)
            {
                Debug.LogError($"Message:{e.Message} {Environment.NewLine} Stack: {e.StackTrace}");
            }
        }

        public async void ScsPlayerReadyFlow()
        {
            var roomId = Guid.NewGuid();
            Debug.Log($"RoomId: {roomId}");
            var amount = new BigInteger(100);
            var result = await _scs.GetTicket(roomId, amount);
            Debug.Log($"[ScsClientTest] Got Ticket {result.TypedData}");
            var signature = await _scs.SignTypedDataMessage(result.TypedData);
            Debug.Log($"[ScsClientTest] Got Signature {signature}");
            await _scs.SendSignedTicket(result.Nonce, signature);

            Debug.Log($"[ScsClientTest] Setting player ready for roomId {roomId}");
            var onReady = await _scs.SetPlayerReady(roomId, amount);
            if (!onReady.Allow)
            {
                Debug.Log($"[ScsClientTest] Transactions to sign: {onReady.TransactionsToSign.Length}");
                foreach (var transactionToSign in onReady.TransactionsToSign)
                {
                    try
                    {
                        var depositAllowanceSignature = await _scs.SetAllowance(transactionToSign.From, transactionToSign.To, transactionToSign.Data);
                        Debug.Log($"[ScsClientTest] Deposit signature: {depositAllowanceSignature}");
                        Utils.ThrowIfAllowanceNotSigned(depositAllowanceSignature);
                        ElympicsLogger.Log($"Signature of allowance: {depositAllowanceSignature}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ScsClientTest] Exception {e}");
                    }
                }
            }
            var onReadyAgain = await _scs.SetPlayerReady(roomId, amount);
            if (!onReadyAgain.Allow)
                Debug.LogError($"[ScsClientTest] Can't be ready after fulfilling all transactions.");
            else
                Debug.Log($"[ScsClientTest] I can become ready.");
        }

        public async void GetChainconfigConfigFromSCS()
        {
            try
            {
                var result = await _scs.GetChainConfigForGame(gameIdForChainConfig);
                Debug.Log($"[ScsClientTest] got chainconfig for gameId {gameIdForChainConfig}\n{result}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ScsClientTest] Error when retrieving chainConfig for gameId: {gameIdForChainConfig}: \n {e}");
            }
        }
    }
}
