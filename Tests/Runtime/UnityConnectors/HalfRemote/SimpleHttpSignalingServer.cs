using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityConnectors.HalfRemote.Server;
using UnityEngine;

namespace Elympics.Tests.UnityConnectors.HalfRemote
{
    public class SimpleHttpSignalingServer
    {
        private readonly HttpListener _listener;
        private readonly IWebClientInitializer _webClientInitializer;

        public SimpleHttpSignalingServer(IWebClientInitializer webClientInitializer, IPEndPoint endPoint)
        {
            _webClientInitializer = webClientInitializer;

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{endPoint.Address}:{endPoint.Port}/");
        }

        public async void RunAsync(CancellationToken ct)
        {
            try
            {
                await HandleConnections(ct);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private async UniTask HandleConnections(CancellationToken ct)
        {
            _listener.Start();
            while (!ct.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await _listener.GetContextAsync();
                }
                catch (ObjectDisposedException)
                {
                    Debug.LogWarning("Listener disposed");
                    break;
                }

                var request = ctx.Request;
                var response = ctx.Response;

                switch (request.HttpMethod)
                {
                    case "POST":
                    {
                        if (request.Url.AbsolutePath != "/doSignaling")
                        {
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                        }
                        else
                        {
                            response.AddHeader("Access-Control-Allow-Origin", "*");

                            string offer;
                            using (var readStream = new StreamReader(request.InputStream, Encoding.ASCII))
                                offer = await readStream.ReadToEndAsync();

                            var answer = await _webClientInitializer.InitClientAndCreateAnswer(offer);

                            response.StatusCode = (int)HttpStatusCode.OK;
                            response.AddHeader("Content-Type", "application/json");
                            using (var writeStream = new StreamWriter(response.OutputStream, Encoding.ASCII))
                                await writeStream.WriteAsync(answer);
                        }

                        break;
                    }
                    case "OPTIONS":
                        response.AddHeader("Access-Control-Allow-Origin", "*");
                        response.AddHeader("Access-Control-Allow-Methods", "POST, OPTIONS");
                        response.AddHeader("Access-Control-Allow-Headers", "content-type, x-requested-with");
                        response.AddHeader("Access-Control-Max-Age", "86400");
                        response.StatusCode = (int)HttpStatusCode.NoContent;
                        break;
                    default:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        break;
                }

                response.Close();
            }

            _listener.Stop();
        }
    }
}
