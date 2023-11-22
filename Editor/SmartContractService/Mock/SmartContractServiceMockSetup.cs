using System;
using System.Collections.Generic;
using System.Numerics;
using Cysharp.Threading.Tasks;
using NSubstitute;
using SCS.InternalModels.Player;

#nullable enable

namespace SCS
{
    public static class SmartContractServiceMockSetup
    {
        public static readonly SmartContractServiceMockBackend SmartContractServiceMockBackend;

        private const string SenderAddress = "0xF85B254627F1c40E918CFe6ba17200957e5A58F0";

        private static readonly IScsWebRequest ScsWebRequest;
        private static readonly IScsWebRequest Scs = Substitute.For<IScsWebRequest>();
        private static bool allTransactionSignedByPlayer;
        public static bool HappyPathForMarkPlayerReady { get; private set; }

        // source https://github.com/Thundernerd/Unity3D-NSubstitute/blob/main/Editor/NSubstitute.dll
        static SmartContractServiceMockSetup()
        {
            SmartContractServiceMockBackend = new SmartContractServiceMockBackend();
            ScsWebRequest = CreateMockWebSocket();
        }

        internal static IScsWebRequest MockScsWebRequest() => ScsWebRequest;

        private static IScsWebRequest CreateMockWebSocket()
        {
            _ = Scs.GetTicket(Arg.Any<GetTicketRequest>()).Returns(_ =>
            {
                const string messageToSign = "{\n      \"types\": {\n        \"EIP712Domain\": [\n          {\n            \"name\": \"name\",\n            \"type\": \"string\"\n          },\n          {\n            \"name\": \"version\",\n            \"type\": \"string\"\n          },\n          {\n            \"name\": \"chainId\",\n            \"type\": \"uint256\"\n          },\n          {\n            \"name\": \"verifyingContract\",\n            \"type\": \"address\"\n          }\n        ],\n        \"Person\": [\n          {\n            \"name\": \"name\",\n            \"type\": \"string\"\n          },\n          {\n            \"name\": \"wallet\",\n            \"type\": \"address\"\n          }\n        ],\n        \"Mail\": [\n          {\n            \"name\": \"from\",\n            \"type\": \"Person\"\n          },\n          {\n            \"name\": \"to\",\n            \"type\": \"Person\"\n          },\n          {\n            \"name\": \"contents\",\n            \"type\": \"string\"\n          }\n        ]\n      },\n      \"primaryType\": \"Mail\",\n      \"domain\": {\n        \"name\": \"Ether Mail\",\n        \"version\": \"1\",\n        \"chainId\": 5,\n        \"verifyingContract\": \"0xCcCCccccCCCCcCCCCCCcCcCccCcCCCcCcccccccC\"\n      },\n      \"message\": {\n        \"from\": {\n          \"name\": \"Cow\",\n          \"wallet\": \"0xCD2a3d9F938E13CD947Ec05AbC7FE734Df8DD826\"\n        },\n        \"to\": {\n          \"name\": \"Bob\",\n          \"wallet\": \"0xbBbBBBBbbBBBbbbBbbBbbbbBBbBbbbbBbBbbBBbB\"\n        },\n        \"contents\": \"Hello, Bob!\"\n      }\n    }";
                return UniTask.FromResult(new GetTicketResponse("1", messageToSign));
            });

            _ = Scs.SendSignedTicket(Arg.Any<SendSignedTicketRequest>()).Returns(UniTask.CompletedTask);

            _ = Scs.AddDeposit(Arg.Any<AddDepositRequest>()).Returns(_ =>
            {

                IReadOnlyList<TransactionToSign> listToSign = new List<TransactionToSign>
                {
                    new()
                    {
                        From = SenderAddress,
                        To = "0x97250C2f8E466C5dFb7fa2BF09F71fBDc0702174",
                        Data = "0x095ea7b3000000000000000000000000d48ec1148b3b22c4e4bb82fce23359fb37d7302f0000000000000000000000000000000000000000000000000000000000000064",
                    },
                };
                return UniTask.FromResult(listToSign);
            });

            _ = Scs.GetDepositStates(Arg.Any<string>()).Returns(_ =>
            {
                IReadOnlyList<DepositState> deposits = new List<DepositState>
                {
                    new()
                    {
                        TokenAddress = "MockAddress",
                        ActualAmount = BigInteger.One,
                        AvailableAmount = BigInteger.Zero,
                    },
                };

                return UniTask.FromResult(deposits);
            });

            _ = Scs.GetUserTransactionList(Arg.Any<string>(), Arg.Any<int>()).Returns(callInfo =>
            {

                var allGames = callInfo.Args()[0] == null;
                var limit = (int)callInfo.Args()[1];

                var deposits = new List<FinalizedTransaction>();
                for (var i = 0; i < limit; i++)
                    deposits.Add(new FinalizedTransaction
                    {
                        MatchId = Guid.NewGuid().ToString(),
                        GameId = allGames ? $"GameId_{i}" : "CurrentGameId",
                        GameName = allGames ? $"GameName_{i}" : "CurrentGameName",
                        VersionName = allGames ? $"GameVersion_{i}" : "CurrentGameVersion",
                        Result = 1,
                        Amount = new BigInteger(i),
                        State = TransactionState.Finished,
                        ChainId = 1,
                        TransactionId = $"TransactionID_{i}",
                    });
                return UniTask.FromResult((IReadOnlyList<FinalizedTransaction>)deposits);
            });

            _ = Scs.SetPlayerReady(Arg.Any<Guid>(), Arg.Any<BigInteger>(), Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(_ =>
            {
                var response = new SetPlayerReadyResponse(HappyPathForMarkPlayerReady || allTransactionSignedByPlayer, HappyPathForMarkPlayerReady ? string.Empty : "Allowance has not been set", HappyPathForMarkPlayerReady
                    ? null
                    : new[]
                    {
                        new TransactionToSignResult
                        {
                            From = SenderAddress,
                            To = "0x97250C2f8E466C5dFb7fa2BF09F71fBDc0702174",
                            Data = "0x095ea7b3000000000000000000000000d48ec1148b3b22c4e4bb82fce23359fb37d7302f0000000000000000000000000000000000000000000000000000000000000064",
                        },
                    });
                allTransactionSignedByPlayer = true;
                return UniTask.FromResult(response);
            });
            return Scs;
        }

        public static void ToggleHappyPathForMarkPlayerReady() => HappyPathForMarkPlayerReady = !HappyPathForMarkPlayerReady;

    }
}
