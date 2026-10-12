using System;
using System.Net.Http;
using System.Text;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Models;
using UnityEngine;

namespace Elympics.Tests.UnityConnectors.HalfRemote
{
    internal class SimpleHttpSignalingClient
    {
        private readonly Uri _uri;
        private readonly HttpClient _httpClient;

        public SimpleHttpSignalingClient(Uri uri)
        {
            _uri = uri;
            _httpClient = new HttpClient();
        }

        public async UniTask<SignalingResponse> PostOfferAsync(OfferWithCandidates offer)
        {
            var requestContent = new StringContent(JsonUtility.ToJson(offer), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_uri, requestContent);
            var responseText = await response.Content.ReadAsStringAsync();
            return JsonUtility.FromJson<SignalingResponse>(responseText);
        }
    }
}
