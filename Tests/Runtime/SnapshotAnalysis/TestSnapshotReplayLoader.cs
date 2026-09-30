#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Elympics.SnapshotAnalysis;
using Elympics.SnapshotAnalysis.Serialization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Elympics.Tests.SnapshotAnalysis
{
    [Category("SnapshotReplay")]
    public class TestSnapshotReplayLoader
    {
        private const string GameVersion = "7";

        private ElympicsGameConfig _config = null!;
        private string _directory = null!;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ElympicsGameConfig>();
            _config.GameVersion = GameVersion;
            _directory = Path.Combine(Path.GetTempPath(), "ElympicsReplayTests-" + Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
            Directory.Delete(_directory, true);
        }

        private static async UniTask<byte[]> ReplayBytes(string gameVersion)
        {
            var serializer = new LatestMessagePackSerializer();
            using var stream = new MemoryStream();
            await serializer.SerializeVersionToStream(stream, SerializationUtil.LatestVersion);
            await serializer.SerializeToStream(stream, new SnapshotSaverInitData("test", "game", "id", gameVersion, 1, "sdk", 0.03f, default, null, new List<InitialMatchPlayerDataGuid>()));
            return stream.ToArray();
        }

        private async UniTask<string> WriteReplay(string gameVersion, string name = "match")
        {
            var path = Path.Combine(_directory, name + EditorSnapshotAnalysisCollector.DefaultFileExtension);
            File.WriteAllBytes(path, await ReplayBytes(gameVersion));
            return path;
        }

        private UniTask<SnapshotReplayLoader.Result> Load(string path) =>
            SnapshotReplayLoader.LoadAsync(new SnapshotReplayLaunchData(path), _config, CancellationToken.None);

        [UnityTest]
        public IEnumerator LocalReplayIsLoaded() => UniTask.ToCoroutine(async () =>
        {
            var path = await WriteReplay(GameVersion);

            var result = await Load(path);

            Assert.That(result.Failure, Is.Null, result.Failure?.ToString());
            Assert.That(result.Retriever!.RetrieveInitData().GameVersion, Is.EqualTo(GameVersion));
        });

        [UnityTest]
        public IEnumerator LocalReplayWithoutExtensionAndByDirectoryIsLoaded() => UniTask.ToCoroutine(async () =>
        {
            var path = await WriteReplay(GameVersion);

            Assert.That((await Load(path[..^EditorSnapshotAnalysisCollector.DefaultFileExtension.Length])).Failure, Is.Null);
            Assert.That((await Load(_directory + Path.DirectorySeparatorChar)).Failure, Is.Null);
        });

        [UnityTest]
        public IEnumerator MissingLocalReplayIsInvalidPayload() => UniTask.ToCoroutine(async () =>
        {
            var result = await Load(Path.Combine(_directory, "missing"));
            Assert.That(result.Failure!.Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));
        });

        [UnityTest]
        public IEnumerator ReplayOfOtherGameVersionIsInvalidPayload() => UniTask.ToCoroutine(async () =>
        {
            var result = await Load(await WriteReplay("other"));

            Assert.That(result.Failure!.Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));
            Assert.That(result.Failure.message, Does.Contain("version"));
        });

        #region URL

        /// <summary>Serves one response on a free localhost port. Desktop only.</summary>
        private sealed class OneShotServer : IDisposable
        {
            private readonly HttpListener _listener = new();
            public string Url { get; }

            public OneShotServer(int status, byte[] body)
            {
                var port = FreePort();
                Url = $"http://localhost:{port}/replay{EditorSnapshotAnalysisCollector.DefaultFileExtension}";
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Start();
                _ = Task.Run(async () =>
                {
                    var context = await _listener.GetContextAsync();
                    context.Response.StatusCode = status;
                    await context.Response.OutputStream.WriteAsync(body, 0, body.Length);
                    context.Response.Close();
                });
            }

            private static int FreePort()
            {
                var socket = new TcpListener(IPAddress.Loopback, 0);
                socket.Start();
                var port = ((IPEndPoint)socket.LocalEndpoint).Port;
                socket.Stop();
                return port;
            }

            public void Dispose() => _listener.Close();
        }

        [UnityTest]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        public IEnumerator ReplayFromUrlIsLoaded() => UniTask.ToCoroutine(async () =>
        {
            using var server = new OneShotServer(200, await ReplayBytes(GameVersion));

            var result = await Load(server.Url);

            Assert.That(result.Failure, Is.Null, result.Failure?.ToString());
            Assert.That(result.Retriever!.RetrieveInitData().GameVersion, Is.EqualTo(GameVersion));
        });

        [UnityTest]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        public IEnumerator ReplayUrlWithErrorStatusIsConnectionFailed() => UniTask.ToCoroutine(async () =>
        {
            using var server = new OneShotServer(404, Array.Empty<byte>());
            Assert.That((await Load(server.Url)).Failure!.Code, Is.EqualTo(ElympicsLaunchErrorCode.ConnectionFailed));
        });

        [UnityTest]
        [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
        public IEnumerator UnreadableReplayFromUrlIsInvalidPayload() => UniTask.ToCoroutine(async () =>
        {
            using var server = new OneShotServer(200, new byte[] { 1, 2, 3 });
            Assert.That((await Load(server.Url)).Failure!.Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));
        });

        #endregion
    }
}
