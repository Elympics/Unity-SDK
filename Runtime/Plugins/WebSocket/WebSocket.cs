/*
 * unity-websocket-webgl
 *
 * @author Jiri Hybek <jiri@hybek.cz>
 * @copyright 2018 Jiri Hybek <jiri@hybek.cz>
 * @license Apache 2.0 - See LICENSE file distributed with this source code.
 */

/*
 * With modifications licensed to Elympics Sp. z o.o.
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using AOT;

namespace HybridWebSocket
{
	/// <summary>
    /// Handler for WebSocket Open event.
    /// </summary>
    public delegate void WebSocketOpenEventHandler();

    /// <summary>
    /// Handler for message received from WebSocket.
    /// </summary>
    public delegate void WebSocketMessageEventHandler(byte[] data);

    /// <summary>
    /// Handler for an error event received from WebSocket.
    /// </summary>
    public delegate void WebSocketErrorEventHandler(string errorMsg);

    /// <summary>
    /// Handler for WebSocket Close event.
    /// </summary>
    public delegate void WebSocketCloseEventHandler(WebSocketCloseCode closeCode, string reason);

    /// <summary>
    /// Enum representing WebSocket connection state
    /// </summary>
    public enum WebSocketState
    {
        Connecting,
        Open,
        Closing,
        Closed
    }

    /// <summary>
    /// Web socket close codes.
    /// </summary>
    public enum WebSocketCloseCode
    {
        /* Do NOT use NotSet - it's only purpose is to indicate that the close code cannot be parsed. */
        NotSet = 0,
        Normal = 1000,
        Away = 1001,
        ProtocolError = 1002,
        UnsupportedData = 1003,
        Undefined = 1004,
        NoStatus = 1005,
        Abnormal = 1006,
        InvalidData = 1007,
        PolicyViolation = 1008,
        TooBig = 1009,
        MandatoryExtension = 1010,
        ServerError = 1011,
        TlsHandshakeFailure = 1015
    }

    /// <summary>
    /// WebSocket class interface shared by both native and JSLIB implementation.
    /// </summary>
    public interface IWebSocket
    {
        /// <summary>
        /// Open WebSocket connection
        /// </summary>
        void Connect();

        /// <summary>
        /// Close WebSocket connection with optional status code and reason.
        /// </summary>
        /// <param name="code">Close status code.</param>
        /// <param name="reason">Reason string.</param>
        void Close(WebSocketCloseCode code = WebSocketCloseCode.Normal, string reason = null);

        /// <summary>
        /// Send binary data over the socket.
        /// </summary>
        /// <param name="data">Payload data.</param>
        void Send(byte[] data);

        /// <summary>
        /// Return WebSocket connection state.
        /// </summary>
        /// <returns>The state.</returns>
        WebSocketState GetState();

        /// <summary>
        /// Occurs when the connection is opened.
        /// </summary>
        event WebSocketOpenEventHandler OnOpen;

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        event WebSocketMessageEventHandler OnMessage;

        /// <summary>
        /// Occurs when an error was reported from WebSocket.
        /// </summary>
        event WebSocketErrorEventHandler OnError;

        /// <summary>
        /// Occurs when the socked was closed.
        /// </summary>
        event WebSocketCloseEventHandler OnClose;
    }

    /// <summary>
    /// Various helpers to work mainly with enums and exceptions.
    /// </summary>
    public static class WebSocketHelpers
    {
	    /// <summary>
        /// Safely parse close code enum from int value.
        /// </summary>
        /// <returns>The close code enum.</returns>
        /// <param name="closeCode">Close code as int.</param>
        public static WebSocketCloseCode ParseCloseCodeEnum(int closeCode)
        {
	        if (Enum.IsDefined(typeof(WebSocketCloseCode), closeCode))
                return (WebSocketCloseCode)closeCode;
            return WebSocketCloseCode.Undefined;
        }

        /*
         * Return error message based on int code
         *

         */
        /// <summary>
        /// Return an exception instance based on int code.
        ///
        /// Used for resolving JSLIB errors to meaninfull messages.
        /// </summary>
        /// <returns>Instance of an exception.</returns>
        /// <param name="errorCode">Error code.</param>
        /// <param name="inner">Inner exception</param>
        public static WebSocketException GetErrorMessageFromCode(int errorCode, Exception inner)
        {
            switch(errorCode)
            {
                case -1: return new WebSocketUnexpectedException("WebSocket instance not found.", inner);
                case -2: return new WebSocketInvalidStateException("WebSocket is already connected or in connecting state.", inner);
                case -3: return new WebSocketInvalidStateException("WebSocket is not connected.", inner);
                case -4: return new WebSocketInvalidStateException("WebSocket is already closing.", inner);
                case -5: return new WebSocketInvalidStateException("WebSocket is already closed.", inner);
                case -6: return new WebSocketInvalidStateException("WebSocket is not in open state.", inner);
                case -7: return new WebSocketInvalidArgumentException("Cannot close WebSocket. An invalid code was specified or reason is too long.", inner);
                default: return new WebSocketUnexpectedException("Unknown error.", inner);
            }
        }
    }

    /// <summary>
    /// Generic WebSocket exception class
    /// </summary>
    public class WebSocketException : Exception
    {
	    public WebSocketException()
        { }

        public WebSocketException(string message)
            : base(message)
        { }

        public WebSocketException(string message, Exception inner)
            : base(message, inner)
        { }
    }

    /// <summary>
    /// Web socket exception raised when an error was not expected, probably due to corrupted internal state.
    /// </summary>
    public class WebSocketUnexpectedException : WebSocketException
    {
        public WebSocketUnexpectedException() { }
        public WebSocketUnexpectedException(string message) : base(message) { }
        public WebSocketUnexpectedException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Invalid argument exception raised when bad arguments are passed to a method.
    /// </summary>
    public class WebSocketInvalidArgumentException : WebSocketException
    {
        public WebSocketInvalidArgumentException() { }
        public WebSocketInvalidArgumentException(string message) : base(message) { }
        public WebSocketInvalidArgumentException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Invalid state exception raised when trying to invoke action which cannot be done due to different then required state.
    /// </summary>
    public class WebSocketInvalidStateException : WebSocketException
    {
        public WebSocketInvalidStateException() { }
        public WebSocketInvalidStateException(string message) : base(message) { }
        public WebSocketInvalidStateException(string message, Exception inner) : base(message, inner) { }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    /// <summary>
    /// WebSocket class bound to JSLIB.
    /// </summary>
    public class WebSocket : IWebSocket
    {
        /* WebSocket JSLIB functions */
        [DllImport("__Internal")]
        public static extern int ElympicsWebSocketConnect(int instanceId);

        [DllImport("__Internal")]
        public static extern int ElympicsWebSocketClose(int instanceId, int code, string reason);

        [DllImport("__Internal")]
        public static extern int ElympicsWebSocketSend(int instanceId, byte[] dataPtr, int dataLength);

        [DllImport("__Internal")]
        public static extern int ElympicsWebSocketGetState(int instanceId);

        /// <summary>
        /// The instance identifier.
        /// </summary>
        protected int instanceId;

        /// <summary>
        /// Occurs when the connection is opened.
        /// </summary>
        public event WebSocketOpenEventHandler OnOpen;

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        public event WebSocketMessageEventHandler OnMessage;

        /// <summary>
        /// Occurs when an error was reported from WebSocket.
        /// </summary>
        public event WebSocketErrorEventHandler OnError;

        /// <summary>
        /// Occurs when the socked was closed.
        /// </summary>
        public event WebSocketCloseEventHandler OnClose;

        /// <summary>
        /// Constructor - receive JSLIB instance id of allocated socket
        /// </summary>
        /// <param name="instanceId">Instance identifier.</param>
        internal WebSocket(int instanceId)
        {
            this.instanceId = instanceId;
        }

        /// <summary>
        /// Destructor - notifies WebSocketFactory about it to remove JSLIB references
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="T:HybridWebSocket.WebSocket"/> is reclaimed by garbage collection.
        /// </summary>
        ~WebSocket()
        {
            WebSocketFactory.HandleInstanceDestroy(this.instanceId);
        }

        /// <summary>
        /// Return JSLIB instance ID
        /// </summary>
        /// <returns>The instance identifier.</returns>
        public int GetInstanceId()
        {
            return this.instanceId;
        }

        /// <summary>
        /// Open WebSocket connection
        /// </summary>
        public void Connect()
        {
            int ret = ElympicsWebSocketConnect(this.instanceId);

            if (ret < 0)
                throw WebSocketHelpers.GetErrorMessageFromCode(ret, null);
        }

        /// <summary>
        /// Close WebSocket connection with optional status code and reason.
        /// </summary>
        /// <param name="code">Close status code.</param>
        /// <param name="reason">Reason string.</param>
        public void Close(WebSocketCloseCode code = WebSocketCloseCode.Normal, string reason = null)
        {
            int ret = ElympicsWebSocketClose(this.instanceId, (int)code, reason);

            if (ret < 0)
                throw WebSocketHelpers.GetErrorMessageFromCode(ret, null);
        }

        /// <summary>
        /// Send binary data over the socket.
        /// </summary>
        /// <param name="data">Payload data.</param>
        public void Send(byte[] data)
        {
            int ret = ElympicsWebSocketSend(this.instanceId, data, data.Length);

            if (ret < 0)
                throw WebSocketHelpers.GetErrorMessageFromCode(ret, null);
        }

        /// <summary>
        /// Return WebSocket connection state.
        /// </summary>
        /// <returns>The state.</returns>
        public WebSocketState GetState()
        {
            int state = ElympicsWebSocketGetState(this.instanceId);

            if (state < 0)
                throw WebSocketHelpers.GetErrorMessageFromCode(state, null);

            switch (state)
            {
                case 0:
                    return WebSocketState.Connecting;

                case 1:
                    return WebSocketState.Open;

                case 2:
                    return WebSocketState.Closing;

                case 3:
                    return WebSocketState.Closed;

                default:
                    return WebSocketState.Closed;
            }
        }

        /// <summary>
        /// Delegates onOpen event from JSLIB to native sharp event
        /// Is called by WebSocketFactory
        /// </summary>
        public void DelegateOnOpenEvent()
        {
            OnOpen?.Invoke();
        }

        /// <summary>
        /// Delegates onMessage event from JSLIB to native sharp event
        /// Is called by WebSocketFactory
        /// </summary>
        /// <param name="data">Binary data.</param>
        public void DelegateOnMessageEvent(byte[] data)
        {
            OnMessage?.Invoke(data);
        }

        /// <summary>
        /// Delegates onError event from JSLIB to native sharp event
        /// Is called by WebSocketFactory
        /// </summary>
        /// <param name="errorMsg">Error message.</param>
        public void DelegateOnErrorEvent(string errorMsg)
        {
            OnError?.Invoke(errorMsg);
        }

        /// <summary>
        /// Delegate onClose event from JSLIB to native sharp event
        /// Is called by WebSocketFactory
        /// </summary>
        /// <param name="closeCode">Close status code.</param>
        public void DelegateOnCloseEvent(int closeCode, string reason)
        {
            OnClose?.Invoke(WebSocketHelpers.ParseCloseCodeEnum(closeCode), reason);
        }
    }
#else
    public class WebSocket : IWebSocket
    {

        /// <summary>
        /// Occurs when the connection is opened.
        /// </summary>
        public event WebSocketOpenEventHandler OnOpen;

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        public event WebSocketMessageEventHandler OnMessage;

        /// <summary>
        /// Occurs when an error was reported from WebSocket.
        /// </summary>
        public event WebSocketErrorEventHandler OnError;

        /// <summary>
        /// Occurs when the socked was closed.
        /// </summary>
        public event WebSocketCloseEventHandler OnClose;

        /// <summary>
        /// The WebSocketSharp instance.
        /// </summary>
        private readonly WebSocketSharp.WebSocket _ws;

        /// <summary>
        /// WebSocket constructor.
        /// </summary>
        /// <param name="url">Valid WebSocket URL.</param>
        /// <param name="protocol">Requested WebSocket sub-protocol.</param>
        internal WebSocket(string url, string protocol = null)
        {
	        try
            {
                // Create WebSocket instance
                _ws = new WebSocketSharp.WebSocket(url, protocol == null ? Array.Empty<string>() : new []{ protocol });
                const SslProtocols tls13 = (SslProtocols)12288;
                if (_ws.IsSecure)
                    _ws.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12 | tls13;

                // Bind events
                _ws.OnOpen += (sender, ev) => OnOpen?.Invoke();
                _ws.OnMessage += (sender, ev) => OnMessage?.Invoke(ev.RawData);
                _ws.OnError += (sender, ev) => OnError?.Invoke(ev.Message);
                _ws.OnClose += (sender, ev) => OnClose?.Invoke(WebSocketHelpers.ParseCloseCodeEnum(ev.Code), ev.Reason);
            }
            catch (Exception e)
            {
	            throw new WebSocketUnexpectedException("Failed to create WebSocket Client.", e);
            }
        }

        /// <summary>
        /// Open WebSocket connection
        /// </summary>
        public void Connect()
        {
	        // Check state
            if (_ws.ReadyState == WebSocketSharp.WebSocketState.Open || _ws.ReadyState == WebSocketSharp.WebSocketState.Closing)
                throw new WebSocketInvalidStateException("WebSocket is already connected or is closing.");

            try
            {
                _ws.ConnectAsync();
            }
            catch (Exception e)
            {
                throw new WebSocketUnexpectedException("Failed to connect.", e);
            }
        }

        /// <summary>
        /// Close WebSocket connection with optional status code and reason.
        /// </summary>
        /// <param name="code">Close status code.</param>
        /// <param name="reason">Reason string.</param>
        public void Close(WebSocketCloseCode code = WebSocketCloseCode.Normal, string reason = null)
        {
	        // Check state
            if (_ws.ReadyState == WebSocketSharp.WebSocketState.Closing)
                throw new WebSocketInvalidStateException("WebSocket is already closing.");

            if (_ws.ReadyState == WebSocketSharp.WebSocketState.Closed)
                throw new WebSocketInvalidStateException("WebSocket is already closed.");

            try
            {
                _ws.CloseAsync((ushort)code, reason);
            }
            catch (Exception e)
            {
                throw new WebSocketUnexpectedException("Failed to close the connection.", e);
            }
        }

        /// <summary>
        /// Send binary data over the socket.
        /// </summary>
        /// <param name="data">Payload data.</param>
        public void Send(byte[] data)
        {
	        // Check state
            if (_ws.ReadyState != WebSocketSharp.WebSocketState.Open)
                throw new WebSocketInvalidStateException("WebSocket is not in open state.");

            try
            {
                _ws.Send(data);
            }
            catch (Exception e)
            {
                throw new WebSocketUnexpectedException("Failed to send message.", e);
            }
        }

        /// <summary>
        /// Return WebSocket connection state.
        /// </summary>
        /// <returns>The state.</returns>
        public WebSocketState GetState()
        {
	        switch (_ws.ReadyState)
            {
                case WebSocketSharp.WebSocketState.Connecting:
                    return WebSocketState.Connecting;

                case WebSocketSharp.WebSocketState.Open:
                    return WebSocketState.Open;

                case WebSocketSharp.WebSocketState.Closing:
                    return WebSocketState.Closing;

                case WebSocketSharp.WebSocketState.Closed:
                    return WebSocketState.Closed;

                default:
                    return WebSocketState.Closed;
            }
        }
    }
#endif

	/// <summary>
	/// Class providing static access methods to work with JSLIB WebSocket or WebSocketSharp interface
	/// </summary>
	public static class WebSocketFactory
	{
#if UNITY_WEBGL && !UNITY_EDITOR
        /* Map of websocket instances */
        private static readonly Dictionary<int, WebSocket> Instances = new Dictionary<int, WebSocket>();

        /* Delegates */
        public delegate void OnOpenCallback(int instanceId);
        public delegate void OnMessageCallback(int instanceId, IntPtr msgPtr, int msgSize);
        public delegate void OnErrorCallback(int instanceId, IntPtr errorPtr);
        public delegate void OnCloseCallback(int instanceId, int closeCode, IntPtr reasonPtr);

        /* WebSocket JSLIB callback setters and other functions */
        [DllImport("__Internal")]
        public static extern int ElympicsWebSocketAllocate(string url, string protocol);

        [DllImport("__Internal")]
        public static extern void ElympicsWebSocketFree(int instanceId);

        [DllImport("__Internal")]
        public static extern void ElympicsWebSocketSetOnOpen(OnOpenCallback callback);

        [DllImport("__Internal")]
        public static extern void ElympicsWebSocketSetOnMessage(OnMessageCallback callback);

        [DllImport("__Internal")]
        public static extern void ElympicsWebSocketSetOnError(OnErrorCallback callback);

        [DllImport("__Internal")]
        public static extern void ElympicsWebSocketSetOnClose(OnCloseCallback callback);

        /* If callbacks was initialized and set */
        private static bool isInitialized;

        /*
         * Initialize WebSocket callbacks to JSLIB
         */
        private static void Initialize()
        {
            ElympicsWebSocketSetOnOpen(DelegateOnOpenEvent);
            ElympicsWebSocketSetOnMessage(DelegateOnMessageEvent);
            ElympicsWebSocketSetOnError(DelegateOnErrorEvent);
            ElympicsWebSocketSetOnClose(DelegateOnCloseEvent);

            isInitialized = true;
        }

        /// <summary>
        /// Called when instance is destroyed (by destructor)
        /// Method removes instance from map and free it in JSLIB implementation
        /// </summary>
        /// <param name="instanceId">Instance identifier.</param>
        public static void HandleInstanceDestroy(int instanceId)
        {
            Instances.Remove(instanceId);
            ElympicsWebSocketFree(instanceId);
        }

        [MonoPInvokeCallback(typeof(OnOpenCallback))]
        public static void DelegateOnOpenEvent(int instanceId)
        {
            if (Instances.TryGetValue(instanceId, out var instanceRef))
            {
                instanceRef.DelegateOnOpenEvent();
            }
        }

        [MonoPInvokeCallback(typeof(OnMessageCallback))]
        public static void DelegateOnMessageEvent(int instanceId, IntPtr msgPtr, int msgSize)
        {
            if (Instances.TryGetValue(instanceId, out var instanceRef))
            {
                var msg = new byte[msgSize];
                Marshal.Copy(msgPtr, msg, 0, msgSize);

                instanceRef.DelegateOnMessageEvent(msg);
            }
        }

        [MonoPInvokeCallback(typeof(OnErrorCallback))]
        public static void DelegateOnErrorEvent(int instanceId, IntPtr errorPtr)
        {
            if (Instances.TryGetValue(instanceId, out var instanceRef))
            {
                var errorMsg = Marshal.PtrToStringAuto(errorPtr);
                instanceRef.DelegateOnErrorEvent(errorMsg);
            }
        }

        [MonoPInvokeCallback(typeof(OnCloseCallback))]
        public static void DelegateOnCloseEvent(int instanceId, int closeCode, IntPtr reasonPtr)
        {
            if (Instances.TryGetValue(instanceId, out var instanceRef))
            {
                var reason = Marshal.PtrToStringAuto(reasonPtr);
                instanceRef.DelegateOnCloseEvent(closeCode, reason);
            }
        }
#endif

		/// <summary>
		/// Create WebSocket client instance
		/// </summary>
		/// <returns>The WebSocket instance.</returns>
		/// <param name="url">WebSocket valid URL.</param>
		/// <param name="protocol">Requested WebSocket sub-protocol.</param>
		public static WebSocket CreateInstance(string url, string protocol = null)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
	        if (!isInitialized)
	            Initialize();

	        var instanceId = ElympicsWebSocketAllocate(url, protocol);
	        var wrapper = new WebSocket(instanceId);
	        Instances.Add(instanceId, wrapper);

	        return wrapper;
#else
			return new WebSocket(url, protocol);
#endif
		}
	}
}
