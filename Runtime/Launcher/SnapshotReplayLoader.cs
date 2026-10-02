#nullable enable

using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.SnapshotAnalysis.Retrievers;
using Elympics.SnapshotAnalysis.Serialization;
using UnityEngine.Networking;

namespace Elympics
{
    internal static class SnapshotReplayLoader
    {
        public readonly struct Result
        {
            public SnapshotAnalysisRetriever? Retriever { get; }
            public ElympicsLaunchFailure? Failure { get; }

            private Result(SnapshotAnalysisRetriever? retriever, ElympicsLaunchFailure? failure)
            {
                Retriever = retriever;
                Failure = failure;
            }

            public static Result Loaded(SnapshotAnalysisRetriever retriever) => new(retriever, null);
            public static Result Failed(ElympicsLaunchErrorCode code, string message) => new(null, new ElympicsLaunchFailure(code, message));
        }

        public static bool LocalReplayExists(string path, out string error) => EditorSnapshotAnalysisRetriever.TryResolvePath(path, out _, out error);

        /// <summary>
        /// A failed download is <see cref="ElympicsLaunchErrorCode.ConnectionFailed"/>;
        /// an unreadable replay or one of another game version is <see cref="ElympicsLaunchErrorCode.InvalidPayload"/>.
        /// </summary>
        /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled during the download.</exception>
        public static async UniTask<Result> LoadAsync(SnapshotReplayLaunchData data, ElympicsGameConfig gameConfig, CancellationToken ct)
        {
            SnapshotAnalysisRetriever retriever;
            if (data.IsUrl)
            {
                byte[] bytes;
                try
                {
                    bytes = await DownloadAsync(data.Path, ct);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    return Result.Failed(ElympicsLaunchErrorCode.ConnectionFailed, $"Could not download replay {data.Path}: {e.Message}");
                }
                try
                {
                    retriever = new InMemorySnapshotAnalysisRetriever(SnapshotDeserializer.DeserializeSnapshots(new MemoryStream(bytes)));
                }
                catch (Exception e)
                {
                    return Result.Failed(ElympicsLaunchErrorCode.InvalidPayload, $"Could not read replay {data.Path}: {Describe(e)}");
                }
            }
            else
            {
                try
                {
                    retriever = new EditorSnapshotAnalysisRetriever(data.Path);
                }
                catch (Exception e)
                {
                    return Result.Failed(ElympicsLaunchErrorCode.InvalidPayload, $"Could not read replay {data.Path}: {Describe(e)}");
                }
            }
            return TryCheckVersion(retriever, gameConfig, out var failure) ? Result.Loaded(retriever) : Result.Failed(failure!.Code, failure.message);
        }

        /// <summary>Inner exceptions carry the actual cause of a failed read.</summary>
        private static string Describe(Exception e)
        {
            var message = e.Message;
            for (var inner = e.InnerException; inner != null; inner = inner.InnerException)
                message += $" {inner.GetType().Name}: {inner.Message}";
            return message;
        }

        private static async UniTask<byte[]> DownloadAsync(string url, CancellationToken ct)
        {
            using var request = UnityWebRequest.Get(url);
            // UniTask throws UnityWebRequestException for connection errors and non-2xx responses.
            _ = await request.SendWebRequest().WithCancellation(ct);
            return request.downloadHandler.data;
        }

        internal static bool TryCheckVersion(SnapshotAnalysisRetriever retriever, ElympicsGameConfig gameConfig, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            var replayVersion = retriever.RetrieveInitData().GameVersion;
            if (replayVersion == gameConfig.GameVersion)
                return true;
            failure = new ElympicsLaunchFailure(ElympicsLaunchErrorCode.InvalidPayload,
                $"Game version mismatch. The replay was recorded with game version {replayVersion} and the current game version is {gameConfig.GameVersion}. "
                + "Use a matching version of the game to watch this replay.");
            return false;
        }
    }
}
