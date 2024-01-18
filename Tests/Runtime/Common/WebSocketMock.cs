using System;
using HybridWebSocket;

#nullable enable

namespace Elympics.Tests.Common
{
    public class WebSocketMock : IWebSocket
    {
        public event WebSocketOpenEventHandler? OnOpen;
        public event WebSocketMessageEventHandler? OnMessage;
        public event WebSocketErrorEventHandler? OnError;
        public event WebSocketCloseEventHandler? OnClose;

        public void Connect() => ConnectCalled?.Invoke();

        public void Close(WebSocketCloseCode code = WebSocketCloseCode.Normal, string? reason = null) =>
            CloseCalled?.Invoke(code, reason);
        public void Send(byte[] data) => SendCalled?.Invoke(data);
        public WebSocketState GetState() => State;

        public WebSocketState State { get; set; }

        public void InvokeOnOpen() => OnOpen?.Invoke();
        public void InvokeOnMessage(byte[] data) => OnMessage?.Invoke(data);
        public void InvokeOnError(string errorMsg) => OnError?.Invoke(errorMsg);
        public void InvokeOnClose(WebSocketCloseCode closeCode, string reason) => OnClose?.Invoke(closeCode, reason);

        public event Action? ConnectCalled;
        public event Action<WebSocketCloseCode, string?>? CloseCalled;
        public event Action<byte[]>? SendCalled;

        public void Reset()
        {
            ConnectCalled = null;
            CloseCalled = null;
            SendCalled = null;
        }
    }
}
