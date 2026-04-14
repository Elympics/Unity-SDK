using System;
using System.Net.Http;
using System.Text;
using Cysharp.Threading.Tasks;

namespace Elympics.Tests.UnityConnectors.HalfRemote
{
    public class SimpleHttpSignalingClient
    {
        private readonly Uri _uri;
        private readonly HttpClient _httpClient;

        public SimpleHttpSignalingClient(Uri uri)
        {
            _uri = uri;
            _httpClient = new HttpClient();
        }

        public async UniTask<string> PostOfferAsync(string offer)
        {
            var response = await _httpClient.PostAsync(_uri, new StringContent(offer, Encoding.ASCII, "application/json"));
            return await response.Content.ReadAsStringAsync();
        }
    }
}
