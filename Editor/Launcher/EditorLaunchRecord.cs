#nullable enable

using System;
using UnityEditor;
using UnityEngine;

namespace Elympics.Editor.Launcher
{
    /// <summary>
    /// The outcome of the last launch started by a tool (e.g. a CLI command), kept in <see cref="SessionState"/> so that it survives the domain reload
    /// of entering play mode. Callers poll it, because the launch finishes after the call that started it has returned.
    /// </summary>
    [InitializeOnLoad]
    internal static class EditorLaunchRecord
    {
        private const string Key = "Elympics.MatchLauncher.LastLaunch";

        public const string Pending = "pending";
        public const string Accepted = "accepted";
        public const string Running = "running";
        public const string Ended = "ended";
        public const string Failed = "failed";
        public const string TimedOut = "timed_out";

        [Serializable]
        public class Record
        {
            public string requestId = "";
            public string mode = "";
            public string gameId = "";
            public string result = "";
            public string code = "";
            public string message = "";
            public string matchId = "";
            public string updatedAt = "";
        }

        private static ElympicsMatchLauncher? subscribed;

        static EditorLaunchRecord()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (EditorApplication.isPlaying)
                EditorApplication.delayCall += Subscribe;
        }

        public static Record? Last
        {
            get
            {
                var json = SessionState.GetString(Key, "");
                return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<Record>(json);
            }
        }

        public static void Start(string requestId, string mode, string gameId) =>
            Save(new Record { requestId = requestId, mode = mode, gameId = gameId, result = Pending });

        internal static void Restore(Record? record)
        {
            if (record == null)
                SessionState.EraseString(Key);
            else
                SessionState.SetString(Key, JsonUtility.ToJson(record));
        }

        private static void Save(Record record)
        {
            record.updatedAt = DateTime.UtcNow.ToString("o");
            SessionState.SetString(Key, JsonUtility.ToJson(record));
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
                // The launch queue sends the pending launch in a delay call too; subscribing first is enough, as events are emitted later.
                Subscribe();
            else if (change == PlayModeStateChange.ExitingPlayMode)
            {
                Unsubscribe();
                if (Last is { result: Pending or Accepted } record)
                {
                    record.result = Failed;
                    record.message = "Play mode stopped before the match started.";
                    Save(record);
                }
            }
        }

        private static void Subscribe()
        {
            Unsubscribe();
            subscribed = ElympicsMatchLauncher.Current;
            if (subscribed != null)
                subscribed.HostEventEmitted += OnHostEvent;
        }

        private static void Unsubscribe()
        {
            if (subscribed != null)
                subscribed.HostEventEmitted -= OnHostEvent;
            subscribed = null;
        }

        internal static void OnHostEvent(string type, string requestId, string payloadJson)
        {
            if (Last is not { } record)
                return;
            var forThisLaunch = requestId == record.requestId;
            switch (type)
            {
                case ElympicsHostProtocol.Events.LaunchAccepted when forThisLaunch:
                    record.result = Accepted;
                    break;
                case ElympicsHostProtocol.Events.LaunchSucceeded when forThisLaunch:
                    record.result = Running;
                    record.matchId = string.IsNullOrEmpty(payloadJson) ? "" : JsonUtility.FromJson<ElympicsLaunchSucceededInfo>(payloadJson).matchId;
                    break;
                case ElympicsHostProtocol.Events.LaunchFailed when forThisLaunch:
                    var failure = string.IsNullOrEmpty(payloadJson) ? new ElympicsLaunchFailure() : JsonUtility.FromJson<ElympicsLaunchFailure>(payloadJson);
                    record.result = failure.code == nameof(ElympicsLaunchErrorCode.Timeout) ? TimedOut : Failed;
                    record.code = failure.code;
                    record.message = failure.message;
                    break;
                case ElympicsHostProtocol.Events.MatchEnded or ElympicsHostProtocol.Events.MatchEndTimedOut when record.result == Running:
                    record.result = Ended;
                    break;
                default:
                    return;
            }
            Save(record);
        }
    }
}
