#nullable enable

using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Elympics.Tests
{
    [Category("Launcher")]
    public class TestIceServers
    {
        private static readonly Guid UserId = Guid.NewGuid();

        private static IceServer CreateTurnServer() => new()
        {
            urls = new[] { "turn:turn.example.test:3478?transport=udp", "turns:turn.example.test:5349?transport=tcp", "turn:turn.example.test:53?transport=udp" },
            username = "fake-user",
            credential = "fake-credential",
        };

        private static OnlineLaunchData ParseOnlineJson(string json)
        {
            var dto = JsonUtility.FromJson<OnlineLaunchDataDto>(json);
            Assert.IsTrue(dto.TryToLaunchData(out var data, out _));
            return (OnlineLaunchData)data!;
        }

        private static string CreateJoinJson(string extra) =>
            $"{{\"serverAddress\":\"/gs\",\"transport\":\"\",\"matchId\":\"{Guid.NewGuid()}\",\"userSecret\":\"s\",\"userId\":\"{UserId}\",\"matchedPlayers\":[\"{UserId}\"]{extra}}}";

        [Test]
        public void JoinDataCarriesIceServers()
        {
            var data = ParseOnlineJson(CreateJoinJson(",\"iceServers\":[{\"urls\":[\"turn:a.example.test:3478\"],\"username\":\"u\",\"credential\":\"c\"}]"));

            Assert.AreEqual(1, data.IceServers!.Count);
            Assert.AreEqual(new[] { "turn:a.example.test:3478" }, data.IceServers[0].urls);
            Assert.AreEqual("u", data.IceServers[0].username);
            Assert.AreEqual("c", data.IceServers[0].credential);
        }

        [Test]
        public void JoinDataWithoutIceServersLeavesThemEmpty()
        {
            var data = ParseOnlineJson(CreateJoinJson(""));

            Assert.IsTrue(data.IceServers == null || data.IceServers.Count == 0);
        }

        [Test]
        public void ProvidedIceServersAreUsedWithoutAskingTheGameServer()
        {
            // The base address is unreachable on purpose: the provided servers must make the request unnecessary.
            var client = new HttpSignalingClient(new Uri("http://127.0.0.1:1/v2"), Guid.NewGuid(), default, new[] { CreateTurnServer() });

            var servers = client.FetchIceServersAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();

            Assert.AreEqual(1, servers.Length);
            Assert.AreEqual("fake-credential", servers[0].credential);
        }

        [Test]
        public void BrowserBlockedPortIsRemoved()
        {
            var servers = HttpSignalingClient.RemoveBrowserBlockedUrls(new[] { CreateTurnServer() });

            Assert.AreEqual(2, servers[0].urls.Length);
            Assert.IsFalse(servers[0].urls.Any(url => url.Contains(":53?")));
        }

        [Test]
        public void ServerWithOnlyBlockedUrlsIsDropped()
        {
            var server = new IceServer { urls = new[] { "turn:turn.example.test:53?transport=udp" }, username = "u", credential = "c" };

            Assert.IsEmpty(HttpSignalingClient.RemoveBrowserBlockedUrls(new[] { server }));
        }

        [Test]
        public void StunServerWithoutCredentialsIsKept()
        {
            var server = new IceServer { urls = new[] { "stun:stun.example.test:3478" } };

            Assert.AreEqual(1, HttpSignalingClient.RemoveBrowserBlockedUrls(new[] { server }).Length);
        }
    }
}
