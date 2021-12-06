var LibraryWebRtc = {
    $webRtcState: {
        instances: {},
        lastId: 0,

        onReceived: null,
        onReceivingError: null,
        onReceivingEnded: null,
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

        // Check UnityAdapters for this code, it's tested there ~pprzestrzelski 04.11.2021
        function WebRtcClient(received, receivingError, receivingEnded, offerCallback) {
            this.pc = new RTCPeerConnection()
            this.dc = this.pc.createDataChannel('data')
            this.received = received;
            this.receivingEnded = receivingEnded;
            this.receivingError = receivingError;
            this.dc.onopen = function () {
                var selectedPair = this.pc.sctp.transport.iceTransport.getSelectedCandidatePair();
                console.log(selectedPair);
            }.bind(this);
            this.dc.onmessage = function (message) {
                this.received(new Uint8Array(message.data));
            }.bind(this);
            this.dc.onerror = function (error) {
                this.receivingError(error.error.toString());
            }.bind(this);
            this.dc.onclose = function () {
                this.receivingEnded();
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
            this.send = function (message) {
                if (this.dc.readyState !== "open")
                    return;
                this.dc.send(message);
            }.bind(this);
            this.close = function () {
                this.dc.close();
                this.pc.close();
            }.bind(this);
        }

        var WebRtcReceived = function (msg) {
            if (webRtcState.onReceived === null)
                return;

            var buffer = _malloc(msg.length);
            HEAPU8.set(msg, buffer);

            try {
                Runtime.dynCall('viii', webRtcState.onReceived, [id, buffer, msg.length]);
            } finally {
                _free(buffer);
            }
        };

        var WebRtcReceivingError = function (msg) {
            if (webRtcState.onReceivingError === null)
                return

            var msgBytes = lengthBytesUTF8(msg) + 1;
            var msgBuffer = _malloc(msgBytes);
            stringToUTF8(msg, msgBuffer, msgBytes);

            try {
                Runtime.dynCall('vii', webRtcState.onReceivingError, [id, msgBuffer]);
            } finally {
                _free(msgBuffer);
            }
        }

        var WebRtcReceivingEnded = function () {
            if (webRtcState.onReceivingEnded === null)
                return

            Runtime.dynCall('vi', webRtcState.onReceivingEnded, [id]);
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

        webRtcState.instances[id] = new WebRtcClient(WebRtcReceived, WebRtcReceivingError, WebRtcReceivingEnded, WebRtcOfferCallback);

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

    WebRtcSetOnReceived: function (callback) {
        webRtcState.onReceived = callback;
    },

    WebRtcSetOnReceivingError: function (callback) {
        webRtcState.onReceivingError = callback;
    },

    WebRtcSetOnReceivingEnded: function (callback) {
        webRtcState.onReceivingEnded = callback;
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

    WebRtcSend: function (id, bufferPtr, length) {
        var instance = webRtcState.instances[id];
        if (!instance)
            return;

        instance.send(HEAPU8.buffer.slice(bufferPtr, bufferPtr + length));
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