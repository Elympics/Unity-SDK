using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
#if ELYMPICS_DEBUG
        internal ClientTickCalculatorNetworkDetailsToFile()
        {
            InitializeWriteToFile();
        }
#endif

        public void LogNetworkDetailsToFile(ClientTickCalculatorNetworkDetails details)
        {
#if ELYMPICS_DEBUG
            lock (_textToFileQueue)
            {
                _textToFileQueue.Enqueue($"[{DateTime.UtcNow:HH:mm:ss.fff}] {details}");
            }
#endif
        }

        private void InitializeWriteToFile()
        {
#if UNITY_EDITOR
            _folderPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, LogDirectoryName);
#elif ELYMPICS_DEBUG
			_folderPath = Path.Combine(Application.persistentDataPath, LogDirectoryName);
#endif
            _fileName = $"DetailedNetworkLogs_{DateTime.Now:yyyy_MM_dd___HH_mm_ss}.txt";

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (true)
                {
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    var anythingToSend = false;
                    lock (_textToFileQueue)
                    {
                        if (_textToFileQueue.Count > 0)
                        {
                            _sb.Clear();
                            for (var i = 0; i < _textToFileQueue.Count; i++)
                            {
                                var text = _textToFileQueue.Dequeue();
                                _sb.AppendLine(text);
                                _sb.AppendLine();
                            }

                            anythingToSend = true;
                        }
                    }

                    if (anythingToSend)
                        await WriteToFile(_sb.ToString(), _cancellationTokenSource.Token);

                    await TaskUtil.Delay(DelayInMs, _cancellationTokenSource.Token);
                }
            }, _cancellationTokenSource.Token);
        }


        private async Task WriteToFile(string text, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!Directory.Exists(_folderPath))
                    _ = Directory.CreateDirectory(_folderPath);

                var combinedPath = Path.Combine(_folderPath, _fileName);
                using var sw = File.AppendText(combinedPath);
                await sw.WriteAsync(text);
            }
            catch (Exception e)
            {
                Debug.LogError($"Something went wrong while writing log to file.\n{e}");
            }
        }

        public void DeInit()
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}
