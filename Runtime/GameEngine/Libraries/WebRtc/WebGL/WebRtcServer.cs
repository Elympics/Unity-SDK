#nullable enable
using System;
using WebRtcWrapper;

namespace Elympics.GameEngine.Libraries.WebRtc
{
    internal class WebRtcServer : IWebRtcServer
    {
        public WebRtcServer() => throw new NotSupportedException();

        public void Dispose() => throw new NotImplementedException();
        public void Start(bool withReceiveThread = true) => throw new NotImplementedException();
        public void Stop() => throw new NotImplementedException();
        public IWebRtcServerClient CreateClient() => throw new NotImplementedException();
        public void ReceiveReliableOnce() => throw new NotImplementedException();
        public void ReceiveUnreliableOnce() => throw new NotImplementedException();
    }
}
