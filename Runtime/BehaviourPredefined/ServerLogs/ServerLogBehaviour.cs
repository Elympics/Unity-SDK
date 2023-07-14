using UnityEngine;

namespace Elympics
{
    public class ServerLogBehaviour : ElympicsMonoBehaviour, IInitializable, IReconciliationHandler
    {
        [Tooltip("Minimum level of collected logs")]
        [SerializeField] private Verbosity verbosity = Verbosity.Log;
        [SerializeField] private string serverLogPrefix = "[Elympics::Server]";

        private readonly ElympicsArray<ElympicsLog> _logs = new(5, () => new ElympicsLog());

        private int _currentLog;  // server-only
        private bool _inReconciliation;  // client-only

        public void Initialize()
        {
            if (!IsEnabledAndActive)
                return;

            if (Elympics.IsServer)
                Application.logMessageReceived += HandleLogReceived;
            else
                foreach (var log in _logs.Values)
                    log.ValueChanged += HandleLogValueChanged;
        }

        private void HandleLogReceived(string message, string stackTrace, LogType type)
        {
            var messageVerbosity = LogTypeToVerbosity(type);
            if (verbosity > messageVerbosity)
                return;
            if (message.StartsWith(serverLogPrefix))
                return;
            var joinedMessage = message;
            if (messageVerbosity >= Verbosity.Error && !string.IsNullOrEmpty(stackTrace))
                joinedMessage += $"\n{stackTrace}";
            _logs.Values[_currentLog].Value = (type, joinedMessage);
            _currentLog = (_currentLog + 1) % _logs.Values.Count;
        }

        private void HandleLogValueChanged((LogType, string) _, (LogType logType, string logMessage) newValue)
        {
            if (_inReconciliation)
                return;
            var message = $"{serverLogPrefix} {newValue.logMessage}";
            Debug.unityLogger.Log(newValue.logType, message);
        }

        private static Verbosity LogTypeToVerbosity(LogType logType)
        {
            return logType switch
            {
                LogType.Assert or LogType.Error or LogType.Exception => Verbosity.Error,
                LogType.Warning => Verbosity.Warning,
                LogType.Log => Verbosity.Log,
                _ => Verbosity.Log,
            };
        }

        // ReSharper disable once Unity.RedundantEventFunction ~dsygocki
        private void Start()
        { }

        public void OnPreReconcile() => _inReconciliation = true;
        public void OnPostReconcile() => _inReconciliation = false;
    }

    internal enum Verbosity
    {
        Log = 0,
        Warning = 1,
        Error = 2,
        None = 3
    }
}
