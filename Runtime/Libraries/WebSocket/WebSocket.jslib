/*
 * unity-websocket-webgl
 * 
 * @author Jiri Hybek <jiri@hybek.cz>
 * @copyright 2018 Jiri Hybek <jiri@hybek.cz>
 * @license Apache 2.0 - See LICENSE file distributed with this source code.
 */

var LibraryWebSocket = {
	$webSocketState: {
		/*
		 * Map of instances
		 * 
		 * Instance structure:
		 * {
		 * 	url: string,
		 * 	ws: WebSocket
		 * }
		 */
		instances: {},

		/* Last instance ID */
		lastId: 0,

		/* Event listeners */
		onOpen: null,
		onMesssage: null,
		onError: null,
		onClose: null,

		/* Debug mode */
		debug: false
	},

	/**
	 * Set onOpen callback
	 * 
	 * @param callback Reference to C# static function
	 */
	WebSocketSetOnOpen: function(callback) {

		webSocketState.onOpen = callback;

	},

	/**
	 * Set onMessage callback
	 * 
	 * @param callback Reference to C# static function
	 */
	WebSocketSetOnMessage: function(callback) {

		webSocketState.onMessage = callback;

	},

	/**
	 * Set onError callback
	 * 
	 * @param callback Reference to C# static function
	 */
	WebSocketSetOnError: function(callback) {

		webSocketState.onError = callback;

	},

	/**
	 * Set onClose callback
	 * 
	 * @param callback Reference to C# static function
	 */
	WebSocketSetOnClose: function(callback) {

		webSocketState.onClose = callback;

	},

	/**
	 * Allocate new WebSocket instance struct
	 * 
	 * @param url Server URL
	 */
	WebSocketAllocate: function(url) {
		var urlStr;
		if (UTF8ToString !== undefined)
			urlStr = UTF8ToString(url);
		else
			urlStr = Pointer_stringify(url);
			
		var id = webSocketState.lastId++;

		webSocketState.instances[id] = {
			url: urlStr,
			ws: null
		};

		return id;

	},

	/**
	 * Remove reference to WebSocket instance
	 * 
	 * If socket is not closed function will close it but onClose event will not be emitted because
	 * this function should be invoked by C# WebSocket destructor.
	 * 
	 * @param instanceId Instance ID
	 */
	WebSocketFree: function(instanceId) {

		var instance = webSocketState.instances[instanceId];

		if (!instance) return 0;

		// Close if not closed
		if (instance.ws !== null && instance.ws.readyState < 2)
			instance.ws.close();

		// Remove reference
		delete webSocketState.instances[instanceId];

		return 0;

	},

	/**
	 * Connect WebSocket to the server
	 * 
	 * @param instanceId Instance ID
	 */
	WebSocketConnect: function(instanceId) {
		// Emscripten v1.37.27: 12/24/2017
		// --------------------
		//  - Breaking change: Remove the `Runtime` object, and move all the useful methods
		//    from it to simple top-level functions. Any usage of `Runtime.func` should be
		//    changed to `func`.
		if (typeof Runtime === "undefined") var Runtime = {dynCall: dynCall};
		
		var instance = webSocketState.instances[instanceId];
		if (!instance) return -1;

		if (instance.ws !== null)
			return -2;

		instance.ws = new WebSocket(instance.url);

		instance.ws.binaryType = 'arraybuffer';

		instance.ws.onopen = function() {

			if (webSocketState.debug)
				console.log("[JSLIB WebSocket] Connected.");

			if (webSocketState.onOpen)
				Runtime.dynCall('vi', webSocketState.onOpen, [ instanceId ]);

		};

		instance.ws.onmessage = function(ev) {

			if (webSocketState.debug)
				console.log("[JSLIB WebSocket] Received message:", ev.data);

			if (webSocketState.onMessage === null)
				return;

			if (ev.data instanceof ArrayBuffer) {

				var dataBuffer = new Uint8Array(ev.data);
				
				var buffer = _malloc(dataBuffer.length);
				HEAPU8.set(dataBuffer, buffer);

				try {
					Runtime.dynCall('viii', webSocketState.onMessage, [ instanceId, buffer, dataBuffer.length ]);
				} finally {
					_free(buffer);
				}

			}

		};

		instance.ws.onerror = function(ev) {
			
			if (webSocketState.debug)
				console.log("[JSLIB WebSocket] Error occured.");

			if (webSocketState.onError) {
				
				var msg = "WebSocket error.";
				var msgBytes = lengthBytesUTF8(msg) + 1;
				var msgBuffer = _malloc(msgBytes);
				stringToUTF8(msg, msgBuffer, msgBytes);

				try {
					Runtime.dynCall('vii', webSocketState.onError, [ instanceId, msgBuffer ]);
				} finally {
					_free(msgBuffer);
				}
			}

		};

		instance.ws.onclose = function(ev) {

			if (webSocketState.debug)
				console.log("[JSLIB WebSocket] Closed.");

			if (webSocketState.onClose)
				Runtime.dynCall('vii', webSocketState.onClose, [ instanceId, ev.code ]);

			delete instance.ws;

		};

		return 0;

	},

	/**
	 * Close WebSocket connection
	 * 
	 * @param instanceId Instance ID
	 * @param code Close status code
	 * @param reasonPtr Pointer to reason string
	 */
	WebSocketClose: function(instanceId, code, reasonPtr) {

		var instance = webSocketState.instances[instanceId];
		if (!instance) return -1;

		if (instance.ws === null)
			return -3;

		if (instance.ws.readyState === 2)
			return -4;

		if (instance.ws.readyState === 3)
			return -5;

		var reason = "";
		if (reasonPtr) {
			if (UTF8ToString !== undefined)
				reason = UTF8ToString(reasonPtr);
			else
				reason = Pointer_stringify(reasonPtr);
		}
		
		try {
			instance.ws.close(code, reason);
		} catch(err) {
			return -7;
		}

		return 0;

	},

	/**
	 * Send message over WebSocket
	 * 
	 * @param instanceId Instance ID
	 * @param bufferPtr Pointer to the message buffer
	 * @param length Length of the message in the buffer
	 */
	WebSocketSend: function(instanceId, bufferPtr, length) {
	
		var instance = webSocketState.instances[instanceId];
		if (!instance) return -1;
		
		if (instance.ws === null)
			return -3;

		if (instance.ws.readyState !== 1)
			return -6;

		instance.ws.send(HEAPU8.buffer.slice(bufferPtr, bufferPtr + length));

		return 0;

	},

	/**
	 * Return WebSocket readyState
	 * 
	 * @param instanceId Instance ID
	 */
	WebSocketGetState: function(instanceId) {

		var instance = webSocketState.instances[instanceId];
		if (!instance) return -1;

		if (instance.ws)
			return instance.ws.readyState;
		else
			return 3;

	}

};

autoAddDeps(LibraryWebSocket, '$webSocketState');
mergeInto(LibraryManager.library, LibraryWebSocket);