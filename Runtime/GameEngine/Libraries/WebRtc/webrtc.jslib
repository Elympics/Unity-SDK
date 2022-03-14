var LibraryWebRtc = {
    $webRtcState: {
        instances: {},
        lastId: 0,

        onReliableReceived: null,
        onReliableError: null,
        onReliableEnded: null,
        onUnreliableReceived: null,
        onUnreliableError: null,
        onUnreliableEnded: null,
        onOffer: null
    },

    WebRtcAllocate: function () {
        // Emscripten v1.37.27: 12/24/2017
        // --------------------
        //  - Breaking change: Remove the `Runtime` object, and move all the useful methods
        //    from it to simple top-level functions. Any usage of `Runtime.func` should be
        //    changed to `func`.
        if (typeof Runtime === "undefined") var Runtime = {dynCall: dynCall};

        var id = webRtcState.lastId++;

        console.log("[WebRTC] Allocating client");

        // Test in UnityConnectors.WebRtcTestServer ~pprzestrzelski 07.02.2021
        function WebRtcClient(reliableReceived, reliableError, reliableEnded, unreliableReceived, unreliableError, unreliableEnded, offerCallback) {
            this.pc = new RTCPeerConnection();

            this.reliableDc = this.pc.createDataChannel('reliable');
            this.reliableReceived = reliableReceived;
            this.reliableError = reliableError;
            this.reliableEnded = reliableEnded;

            this.reliableDc.onopen = function () {
                console.log("Reliable data channel opened");
                if (this.pc.sctp != null) {
                    var selectedPair = this.pc.sctp.transport.iceTransport.getSelectedCandidatePair();
                    console.log(selectedPair);
                }
            }.bind(this);
            this.reliableDc.onmessage = function (message) {
                this.reliableReceived(new Uint8Array(message.data));
            }.bind(this);
            this.reliableDc.onerror = function (error) {
                this.reliableError(error.error.toString());
            }.bind(this);
            this.reliableDc.onclose = function () {
                this.reliableEnded();
            }.bind(this);

            this.unreliableDc = this.pc.createDataChannel('unreliable', {
                maxRetransmits: 0,
                ordered: false
            });
            this.unreliableReceived = unreliableReceived;
            this.unreliableError = unreliableError;
            this.unreliableEnded = unreliableEnded;

            this.unreliableDc.onopen = function () {
                console.log("Unreliable data channel opened");
                if (this.pc.sctp != null) {
                    var selectedPair = this.pc.sctp.transport.iceTransport.getSelectedCandidatePair();
                    console.log(selectedPair);
                }
            }.bind(this);
            this.unreliableDc.onmessage = function (message) {
                this.unreliableReceived(new Uint8Array(message.data));
            }.bind(this);
            this.unreliableDc.onerror = function (error) {
                this.unreliableError(error.error.toString());
            }.bind(this);
            this.unreliableDc.onclose = function () {
                this.unreliableEnded();
            }.bind(this);

            this.createOffer = function () {
                this.pc.createOffer()
                    .then(function (offer) {
                        return this.pc.setLocalDescription(offer).then(function () {
                            return offer;
                        });
                    }.bind(this))
                    .then(function (offer) {
                        var offerJson = JSON.stringify(offer);
                        console.log("[WebRTC] Offer created\n" + offerJson);
                        offerCallback(offerJson);
                    });
            }.bind(this);
            this.onAnswer = function (answerJson) {
                console.log("[WebRTC] Answer received\n" + answerJson);
                var answer = JSON.parse(answerJson);
                this.pc.setRemoteDescription(answer);
            }.bind(this);

            this.sendReliable = function (message) {
                if (this.reliableDc.readyState !== "open")
                    return;
                this.reliableDc.send(message);
            }.bind(this);

            this.sendUnreliable = function (message) {
                if (this.reliableDc.readyState !== "open")
                    return;
                this.unreliableDc.send(message);
            }.bind(this);

            this.close = function () {
                this.reliableDc.close();
                this.pc.close();
            }.bind(this);
        }

        var WebRtcReliableReceived = function (msg) {
            if (webRtcState.onReliableReceived === null)
                return;

            var buffer = _malloc(msg.length);
            HEAPU8.set(msg, buffer);

            try {
                Runtime.dynCall('viii', webRtcState.onReliableReceived, [id, buffer, msg.length]);
            } finally {
                _free(buffer);
            }
        };

        var WebRtcReliableError = function (msg) {
            if (webRtcState.onReliableError === null)
                return

            var msgBytes = lengthBytesUTF8(msg) + 1;
            var msgBuffer = _malloc(msgBytes);
            stringToUTF8(msg, msgBuffer, msgBytes);

            try {
                Runtime.dynCall('vii', webRtcState.onReliableError, [id, msgBuffer]);
            } finally {
                _free(msgBuffer);
            }
        }

        var WebRtcReliableEnded = function () {
            if (webRtcState.onReliableEnded === null)
                return

            Runtime.dynCall('vi', webRtcState.onReliableEnded, [id]);
        }

        var WebRtcUnreliableReceived = function (msg) {
            if (webRtcState.onUnreliableReceived === null)
                return;

            var buffer = _malloc(msg.length);
            HEAPU8.set(msg, buffer);

            try {
                Runtime.dynCall('viii', webRtcState.onUnreliableReceived, [id, buffer, msg.length]);
            } finally {
                _free(buffer);
            }
        };

        var WebRtcUnreliableError = function (msg) {
            if (webRtcState.onUnreliableError === null)
                return

            var msgBytes = lengthBytesUTF8(msg) + 1;
            var msgBuffer = _malloc(msgBytes);
            stringToUTF8(msg, msgBuffer, msgBytes);

            try {
                Runtime.dynCall('vii', webRtcState.onUnreliableError, [id, msgBuffer]);
            } finally {
                _free(msgBuffer);
            }
        }

        var WebRtcUnreliableEnded = function () {
            if (webRtcState.onUnreliableEnded === null)
                return

            Runtime.dynCall('vi', webRtcState.onUnreliableEnded, [id]);
        }

        var WebRtcOfferCallback = function (msg) {
            if (webRtcState.onOffer === null)
                return

            var msgBytes = lengthBytesUTF8(msg) + 1;
            var msgBuffer = _malloc(msgBytes);
            stringToUTF8(msg, msgBuffer, msgBytes);

            try {
                Runtime.dynCall('vii', webRtcState.onOffer, [id, msgBuffer]);
            } finally {
                _free(msgBuffer);
            }
        }

        console.log("[WebRTC] Receiving callbacks created");

        webRtcState.instances[id] = new WebRtcClient(WebRtcReliableReceived, WebRtcReliableError, WebRtcReliableEnded, WebRtcUnreliableReceived, WebRtcUnreliableError, WebRtcUnreliableEnded, WebRtcOfferCallback);

        console.log("[WebRTC] Client allocated");

        return id;
    },

    WebRtcFree: function (id) {

        var instance = webRtcState.instances[id];
        if (!instance)
            return;

        delete webRtcState.instances[id];
        instance.close();
    },

    WebRtcSetOnReliableReceived: function (callback) {
        webRtcState.onReliableReceived = callback;
    },

    WebRtcSetOnReliableError: function (callback) {
        webRtcState.onReliableError = callback;
    },

    WebRtcSetOnReliableEnded: function (callback) {
        webRtcState.onReliableEnded = callback;
    },

    WebRtcSetOnUnreliableReceived: function (callback) {
        webRtcState.onUnreliableReceived = callback;
    },

    WebRtcSetOnUnreliableError: function (callback) {
        webRtcState.onUnreliableError = callback;
    },

    WebRtcSetOnUnreliableEnded: function (callback) {
        webRtcState.onUnreliableEnded = callback;
    },

    WebRtcSetOnOffer: function (callback) {
        webRtcState.onOffer = callback;
    },

    WebRtcCreateOffer: function (id) {

        console.log("[WebRTC] Creating offer");

        var instance = webRtcState.instances[id];
        if (!instance)
            return;

        instance.createOffer();
    },

    WebRtcOnAnswer: function (id, answer) {
        var instance = webRtcState.instances[id];
        if (!instance)
            return;

        var answerStr;
        if (UTF8ToString !== undefined)
            answerStr = UTF8ToString(answer);
        else
            answerStr = Pointer_stringify(answer);
        instance.onAnswer(answerStr);
    },

    WebRtcSendReliable: function (id, bufferPtr, length) {
        var instance = webRtcState.instances[id];
        if (!instance)
            return;

        instance.sendReliable(HEAPU8.buffer.slice(bufferPtr, bufferPtr + length));
    },

    WebRtcSendUnreliable: function (id, bufferPtr, length) {
        var instance = webRtcState.instances[id];
        if (!instance)
            return;

        instance.sendUnreliable(HEAPU8.buffer.slice(bufferPtr, bufferPtr + length));
    },

    WebRtcClose: function (id) {
        var instance = webRtcState.instances[id];
        if (!instance)
            return;

        instance.close();
    }

}

autoAddDeps(LibraryWebRtc, '$webRtcState');
mergeInto(LibraryManager.library, LibraryWebRtc);