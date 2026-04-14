using System;
using System.Collections;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.GameEngine.Libraries.WebRtc;
using NUnit.Framework;
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

            var tcpClient = new System.Net.Sockets.TcpClient();

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

            var httpClient = new SimpleHttpSignalingClient(new Uri($"http://{IPAddress.Loopback}:{webPort}/doSignaling"));
            var webRtcClient = new UnityWebRtcClient(WebRtcConfig.Default);

            async UniTask<HalfRemoteMatchClient> ConnectWebRtc()
            {
                var tcs = new UniTaskCompletionSource<object>();

                string offer = null;
                webRtcClient.OfferCreated += s =>
                {
                    offer = s;
                    _ = tcs.TrySetResult(null);
                };
                webRtcClient.CreateOffer(false);
                _ = await tcs.Task;

                if (string.IsNullOrEmpty(offer))
                    throw new ArgumentException("Offer is empty");

                Debug.Log(offer);
                var answer = await httpClient.PostOfferAsync(offer);
                Debug.Log(answer);

                webRtcClient.OnAnswer(answer);

                return new HalfRemoteMatchClient(UserId, webRtcClient);
            }

            void CloseWebSocket() => webRtcClient.Close();

            await ConnectionTest(ConnectWebRtc, CloseWebSocket, new IPEndPoint(IPAddress.Loopback, tcpPort), new IPEndPoint(IPAddress.Loopback, webPort));
        });

        private async UniTask ConnectionTest(Func<UniTask<HalfRemoteMatchClient>> clientConnect, Action clientClose, IPEndPoint tcpListenEndpoint, IPEndPoint webListenEndpoint)
        {
            // Arrange
            var playerConnected = false;
            var reliableServerDataReceived = 0;
            var unreliableServerDataReceived = 0;
            var reliableClientDataReceived = 0;
            var unreliableClientDataReceived = 0;

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

            var connector = new HalfRemoteGameEngineProtoConnector(gameEngine, tcpListenEndpoint, webListenEndpoint);
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

            await UniTask.Delay(1000, DelayType.Realtime);

            // CLIENT
            var client = await clientConnect.Invoke();

            await UniTask.Delay(1000, DelayType.Realtime);
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

                    await UniTask.Delay(10, DelayType.Realtime);
                }
            }

            async UniTask SendReliableTask()
            {
                for (var i = 0; i < reliableDataNumberToSend; i++)
                {
                    gameEngine.GenerateInGameDataForPlayerOnReliableChannel(new byte[10], UserId);
                    client.SendInputReliable(new byte[10]);

                    await UniTask.Delay(10, DelayType.Realtime);
                }
            }

            await UniTask.WhenAll(SendUnreliableTask(), SendReliableTask());


            clientClose.Invoke();
            await UniTask.Delay(100, DelayType.Realtime);
            connector.Dispose();
            signalingServerCts.Cancel();

            await UniTask.Delay(1000, DelayType.Realtime);

            // Assert
            Assert.IsTrue(playerConnected);
            Assert.AreEqual(unreliableDataNumberToSend, unreliableServerDataReceived);
            Assert.AreEqual(unreliableDataNumberToSend, unreliableClientDataReceived);
            Assert.AreEqual(reliableDataNumberToSend, reliableServerDataReceived);
            Assert.AreEqual(reliableDataNumberToSend, reliableClientDataReceived);
        }
    }
}
