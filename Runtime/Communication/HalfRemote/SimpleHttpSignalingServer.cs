using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityConnectors.HalfRemote.Server;
using UnityEngine;

namespace Plugins.Elympics.Runtime.Communication.HalfRemote
{
    public class SimpleHttpSignalingServer
    {
        private readonly HttpListener _listener;
        private readonly IWebClientInitializer _webClientInitializer;
        private readonly string _uri;

        public SimpleHttpSignalingServer(IWebClientInitializer webClientInitializer, IPEndPoint endPoint)
        {
            _webClientInitializer = webClientInitializer;

            _listener = new HttpListener();
            _uri = $"http://*:{endPoint.Port}/";
            _listener.Prefixes.Add(_uri);
        }

        public void RunAsync(CancellationToken ct)
        {
            if (_uri.Contains("*"))
                Debug.LogWarning("Signaling server listening on all hosts. If it throws HttpListenerException: Access Denied. run Unity for half remote server as administrator");

            _listener.Start();
            Debug.Log($"Started listening on {_uri}");
            _ = ct.Register(() => _listener.Stop());

            _ = Task.Factory.StartNew(async () => await HandleConnections(ct).ContinueWith(_ => Debug.Log("Signaling server stopped")), TaskCreationOptions.LongRunning);
        }

        private async Task HandleConnections(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var ctx = await _listener.GetContextAsync();

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
                            using var writeStream = new StreamWriter(response.OutputStream, Encoding.ASCII);
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
        }
    }
}
