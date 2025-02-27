using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using UnityEngine;

namespace Elympics
{
    internal class SinglePlayerLogMonitor
    {
        private readonly string _gameVersion;
        private readonly string _jwt;
        private readonly ElympicsLoggerContext _logger;
        private const string LogSuffix = "LogFile.txt";
        private readonly string _logFilePath;
        private readonly string _logName;
        private readonly ConcurrentQueue<string> _logBuffer;
        private readonly string _url;
        private const string UrlPath = "server/logs";

        public SinglePlayerLogMonitor(string matchId, string gameVersion, string jwt, ElympicsConfig config, CancellationToken ct)
        {
            _gameVersion = gameVersion;
            _jwt = jwt;
            _logName = matchId + "_" + LogSuffix;
            _logFilePath = Path.Combine(Application.persistentDataPath, _logName);
            _url = string.Join("/", config.ElympicsApiEndpoint, UrlPath);
            _logBuffer = new ConcurrentQueue<string>();
            Application.logMessageReceived += OnLogMessage;
            UniTask.Void(WriteToFile, ct);
        }

        private async UniTaskVoid WriteToFile(CancellationToken ct)
        {
            while (true)
            {

                await WriteToFile(_logFilePath);

                if (!ct.IsCancellationRequested)
                    continue;

                await Finish();
                break;
            }
        }
        private async ValueTask Finish()
        {
            if (_logBuffer.Count != 0)
                await WriteToFile(_logFilePath);

            //await SendLogs();
        }
        private async UniTask WriteToFile(string filePath)
        {
            await using var writer = new StreamWriter(filePath, true);
            while (_logBuffer.TryDequeue(out var message))
                await writer.WriteLineAsync(message);
        }

        //private async UniTask SendLogs()
        //{
        //    var formData = new List<IMultipartFormSection>();
        //    formData.Add(new MultipartFormFileSection(Routes.GamesUploadRequestFilesFieldName, File.ReadAllBytes(_logFilePath), _logName, "multipart/form-data"));
        //    formData.Add(new MultipartFormDataSection("gameVersion", _gameVersion));
        //    var uri = new Uri(_url);
        //    var request = UnityWebRequest.Post(uri, formData);
        //    request.SetRequestHeader("Authorization", $"Bearer {_jwt}");
        //    request.SetSdkVersionHeader();
        //    request.SetTestCertificateHandlerIfNeeded();
        //    request.SendWebRequest().ToUniTask().Forget();
        //}

        private void OnLogMessage(string condition, string stacktrace, LogType type)
        {
            _logBuffer.Enqueue(condition);
        }
    }
}
