using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Elympics.Core.Logger;
using UnityEngine;

namespace Elympics
{
    internal class ClientTickCalculatorNetworkDetailsToFile
    {
        private const int DelayInMs = 5;
        private const string LogDirectoryName = "CustomLogs";

        private readonly StringBuilder _sb = new();
        private readonly Queue<string> _textToFileQueue = new();

        private CancellationTokenSource _cancellationTokenSource;
        private string _fileName;

        private string _folderPath;

        internal ClientTickCalculatorNetworkDetailsToFile() => InitializeWriteToFile();

        [Conditional("ELYMPICS_DEBUG")]
        public void LogNetworkDetailsToFile(ClientTickCalculatorNetworkDetails details)
        {
            lock (_textToFileQueue)
                _textToFileQueue.Enqueue($"[{DateTime.UtcNow:HH:mm:ss.fff}] {details}");
        }

        [Conditional("ELYMPICS_DEBUG")]
        private void InitializeWriteToFile()
        {
#if UNITY_EDITOR
            _folderPath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, LogDirectoryName);
#else
			_folderPath = Path.Combine(Application.persistentDataPath, LogDirectoryName);
#endif
            _fileName = $"DetailedNetworkLogs_{DateTime.Now:yyyy_MM_dd___HH_mm_ss}.txt";

            _cancellationTokenSource = new CancellationTokenSource();
            WriteToFileLooped(_cancellationTokenSource.Token).Forget();
        }

        private async UniTaskVoid WriteToFileLooped(CancellationToken ct)
        {
            try
            {
                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    var anythingToSend = false;
                    lock (_textToFileQueue)
                        if (_textToFileQueue.Count > 0)
                        {
                            _ = _sb.Clear();
                            for (var i = 0; i < _textToFileQueue.Count; i++)
                            {
                                var text = _textToFileQueue.Dequeue();
                                _ = _sb.AppendLine(text)
                                    .AppendLine();
                            }

                            anythingToSend = true;
                        }

                    if (anythingToSend)
                        await WriteToFile(_sb.ToString(), ct);

                    await UniTask.Delay(DelayInMs, DelayType.Realtime, cancellationToken: ct);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task WriteToFile(string text, CancellationToken ct)
        {
            var combinedPath = Path.Combine(_folderPath, _fileName);
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!Directory.Exists(_folderPath))
                    _ = Directory.CreateDirectory(_folderPath);

                using var sw = File.AppendText(combinedPath);
                await sw.WriteAsync(text);
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(new ElympicsException($"Something went wrong while writing log to file at path: {combinedPath}", e));
            }
        }

        public void DeInit() => _cancellationTokenSource?.Cancel();
    }
}
