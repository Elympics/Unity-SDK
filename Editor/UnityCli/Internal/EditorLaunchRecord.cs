#nullable enable

using System;
using UnityEditor;
using UnityEngine;

namespace Elympics.Editor.UnityCli
{
    /// <summary>Survives domain reloads (SessionState).</summary>
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
                if (string.IsNullOrEmpty(json))
                    return null;
                try
                {
                    return JsonUtility.FromJson<Record>(json);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }
        }

        public static void Start(string requestId, string mode, string gameId) =>
            Save(new Record { requestId = requestId, mode = mode, gameId = gameId, result = Pending });

        public static void Fail(string requestId, string message)
        {
            if (Last is not { } record || record.requestId != requestId || IsTerminal(record.result))
                return;
            record.result = Failed;
            record.message = message;
            Save(record);
        }

        internal static void Restore(Record? record)
        {
            if (record == null)
                SessionState.EraseString(Key);
            else
                Write(record);
        }

        private static bool IsTerminal(string result) => result is Ended or Failed or TimedOut;

        private static void Save(Record record)
        {
            record.updatedAt = DateTime.UtcNow.ToString("o");
            Write(record);
        }

        private static void Write(Record record) => SessionState.SetString(Key, JsonUtility.ToJson(record));

        internal static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
                Subscribe();
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                var record = Last;
                if (record is { result: Pending })
                {
                    record.result = Failed;
                    record.message = "Play mode did not start.";
                    Save(record);
                }
            }
            else if (change == PlayModeStateChange.ExitingPlayMode)
            {
                Unsubscribe();
                var record = Last;
                if (record is { result: Pending or Accepted })
                {
                    record.result = Failed;
                    record.message = "Play mode stopped before the match started.";
                    Save(record);
                }
                else if (record is { result: Running })
                {
                    record.result = Ended;
                    record.message = "Play mode stopped before the match ended.";
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
            if (Last is not { } record || IsTerminal(record.result))
                return;
            var forThisLaunch = requestId == record.requestId;
            switch (type)
            {
                case ElympicsHostProtocol.Events.LaunchAccepted when forThisLaunch && record.result == Pending:
                    record.result = Accepted;
                    break;
                case ElympicsHostProtocol.Events.LaunchSucceeded when forThisLaunch && record.result is Pending or Accepted:
                    record.result = Running;
                    record.matchId = string.IsNullOrEmpty(payloadJson) ? "" : JsonUtility.FromJson<ElympicsLaunchSucceededInfo>(payloadJson).matchId;
                    break;
                case ElympicsHostProtocol.Events.LaunchFailed when forThisLaunch && record.result is Pending or Accepted:
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
