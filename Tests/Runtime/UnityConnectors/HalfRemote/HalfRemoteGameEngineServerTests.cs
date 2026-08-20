using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Models;
using Elympics.GameEngine.Libraries.WebRtc;
using MatchTcpLibrary.TransportLayer.Interfaces;
using NUnit.Framework;
using Plugins.Elympics.Runtime.Communication.HalfRemote;
using Proto.ProtoClient.NetworkClient;
using UnityConnectors.HalfRemote;
using UnityEngine;
using UnityEngine.TestTools;

namespace Elympics.Tests.UnityConnectors.HalfRemote
{
    [TestFixture]
    public class HalfRemoteGameEngineServerTests
    {
        private const string UserId = "p0";

        [UnityTest]
        public IEnumerator TcpClientTest() => UniTask.ToCoroutine(async () =>
        {
            const int tcpPort = 7890;
            const int webPort = 7891;

            var tcpClient = new TcpClient();

            UniTask<HalfRemoteMatchClient> ConnectTcpClient()
            {
                tcpClient.Connect(IPAddress.Loopback, tcpPort);
                return UniTask.FromResult(new HalfRemoteMatchClient(UserId, new ProtoNetworkStreamClient(tcpClient.GetStream())));
            }

            void CloseTcpClient() => tcpClient.Close();

            await ConnectionTest(ConnectTcpClient, CloseTcpClient, new IPEndPoint(IPAddress.Loopback, tcpPort), new IPEndPoint(IPAddress.Loopback, webPort));
        });

        [UnityTest]
        public IEnumerator WebRtcTest() => UniTask.ToCoroutine(async () =>
        {
            const int tcpPort = 7894;
            const int webPort = 7895;

            var httpClient = new SimpleHttpSignalingClient(new Uri($"http://{IPAddress.Loopback}:{webPort}/v2/doSignaling/{Guid.Empty}"));
            var webRtcClient = WebRtcFactory.CreateClient(WebRtcConfig.Default);

            var reliableChannel = webRtcClient.CreateDataChannel(INetworkClient.ReliableLabel, true);
            var unreliableChannel = webRtcClient.CreateDataChannel(INetworkClient.UnreliableLabel, false);

            async UniTask<HalfRemoteMatchClient> ConnectWebRtc()
            {
                var offer = await webRtcClient.CreateOffer(false);
                if (string.IsNullOrEmpty(offer))
                    throw new ArgumentException("Offer is empty");

                Debug.Log(offer);
                var answer = await httpClient.PostOfferAsync(new OfferWithCandidates
                {
                    offer = offer,
                    candidates = Array.Empty<string>(),
                });
                Debug.Log(answer);

                await webRtcClient.OnAnswer(answer.answer);

                return new HalfRemoteMatchClient(UserId, reliableChannel, unreliableChannel);
            }

            void CloseWebSocket() => webRtcClient.Close();

            await ConnectionTest(ConnectWebRtc, CloseWebSocket, new IPEndPoint(IPAddress.Loopback, tcpPort), new IPEndPoint(IPAddress.Loopback, webPort));
        });

        private async UniTask ConnectionTest(Func<UniTask<HalfRemoteMatchClient>> clientConnect, Action clientClose, IPEndPoint tcpListenEndpoint, IPEndPoint webListenEndpoint, CancellationToken ct = default)
        {
            // Arrange
            var playerConnected = false;
            var reliableServerDataReceived = 0;
            var unreliableServerDataReceived = 0;
            var reliableClientDataReceived = 0;
            var unreliableClientDataReceived = 0;
            var matchEnded = false;

            // SERVER
            var gameEngine = new GameEngineStub();
            gameEngine.PlayerConnected += userId =>
            {
                playerConnected = true;
                Debug.Log($"Server player connected {userId}");
            };
            gameEngine.InGameDataFromPlayerReliableReceived += (data, userId) =>
            {
                reliableServerDataReceived++;
                Debug.Log($"Reliable server data from player {userId}, {data.Length}");
            };
            gameEngine.InGameDataFromPlayerUnreliableReceived += (data, userId) =>
            {
                unreliableServerDataReceived++;
                Debug.Log($"Unreliable server data from player {userId}, {data.Length}");
            };

            var connector = new HalfRemoteGameEngineProtoConnector(gameEngine, tcpListenEndpoint, webListenEndpoint, () => { });
            var signalingServerCts = new CancellationTokenSource();
            var signalingServer = new SimpleHttpSignalingServer(connector, webListenEndpoint);
            signalingServer.RunAsync(signalingServerCts.Token);

            connector.ReliableClientConnected += (clientId) => Debug.Log($"Reliable client connected {clientId}");
            connector.ReliableClientReceivingError += (clientId, error) => Debug.Log($"Reliable client error {clientId} - {error}");
            connector.ReliableClientReceivingEnded += (clientId) => Debug.Log($"Reliable client ended {clientId}");
            connector.UnreliableClientConnected += (clientId) => Debug.Log($"Unreliable client connected {clientId}");
            connector.UnreliableClientReceivingError += (clientId, error) => Debug.Log($"Unreliable client error {clientId} - {error}");
            connector.UnreliableClientReceivingEnded += (clientId) => Debug.Log($"Unreliable client ended {clientId}");
            connector.ListeningError += ((string source, string error) x) => Debug.Log($"{x.source} Listening error - {x.error}");
            connector.ListeningEnded += (source) => Debug.Log($"{source} Listening ended");
            connector.Listen();

            await UniTask.Delay(1000, DelayType.Realtime, cancellationToken: ct);

            // CLIENT
            var client = await clientConnect.Invoke();

            await UniTask.Delay(1000, DelayType.Realtime, cancellationToken: ct);
            client.InGameDataForPlayerOnReliableChannelGenerated += (data, userId) =>
            {
                reliableClientDataReceived++;
                Debug.Log($"Reliable client data for player {userId}, {data.Length}");
            };
            client.InGameDataForPlayerOnUnreliableChannelGenerated += (data, userId) =>
            {
                unreliableClientDataReceived++;
                Debug.Log($"Unreliable client data for player {userId}, {data.Length}");
            };
            client.MatchEnded += _ => matchEnded = true;

            // Act
            client.PlayerConnected();

            const int unreliableDataNumberToSend = 10;
            const int reliableDataNumberToSend = 5;

            async UniTask SendUnreliableTask()
            {
                for (var i = 0; i < unreliableDataNumberToSend; i++)
                {
                    gameEngine.GenerateInGameDataForPlayerOnUnreliableChannel(new byte[10], UserId);
                    client.SendInputUnreliable(new byte[10]);

                    await UniTask.Delay(10, DelayType.Realtime, cancellationToken: ct);
                }
            }

            async UniTask SendReliableTask()
            {
                for (var i = 0; i < reliableDataNumberToSend; i++)
                {
                    gameEngine.GenerateInGameDataForPlayerOnReliableChannel(new byte[10], UserId);
                    client.SendInputReliable(new byte[10]);

                    await UniTask.Delay(10, DelayType.Realtime, cancellationToken: ct);
                }
            }

            await UniTask.WhenAll(SendUnreliableTask(), SendReliableTask());
            gameEngine.EndGame(null);

            clientClose.Invoke();
            await UniTask.Delay(100, DelayType.Realtime, cancellationToken: ct);
            connector.Dispose();
            signalingServerCts.Cancel();

            await UniTask.Delay(500, DelayType.Realtime, cancellationToken: ct);

            // Assert
            Assert.IsTrue(playerConnected);
            Assert.IsTrue(matchEnded);
            Assert.AreEqual(reliableDataNumberToSend, reliableServerDataReceived);
            Assert.AreEqual(reliableDataNumberToSend, reliableClientDataReceived);
            // Previously, these lines checked if both unreliableClientDataReceived
            //  and unreliableServerDataReceived are equal to unreliableDataNumberToSend.
            // Of course, because the channel is UNRELIABLE it means that such thing should not be asserted.
            // In contrary, here I'm asserting that at least one in ten messages have arrived.
            // This seems to be a chance high enough.
            Assert.That(unreliableClientDataReceived, Is.GreaterThan(0));
            Assert.That(unreliableServerDataReceived, Is.GreaterThan(0));
        }
    }
}
