const LibraryWebRtc = {
    $webRtcState: {
        instances: {},
        lastId: 0,

        log: message => console.log(`[${new Date().toISOString()}] [WebRTC] ${message}`),

        offerAnnouncingDelay: 1000,
        onReliableReceived: null,
        onReliableError: null,
        onReliableEnded: null,
        onUnreliableReceived: null,
        onUnreliableError: null,
        onUnreliableEnded: null,
        onOffer: null,
        onIceCandidate: null,
        onIceConnectionStateChanged: null,
        onConnectionStateChanged: null
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcAllocate: function () {
        const id = webRtcState.lastId++;
        webRtcState.log(`Allocating client #${id}`);

        // Test in UnityConnectors.WebRtcTestServer ~pprzestrzelski 07.02.2021
        function WebRtcClient(
            reliableReceived,
            reliableError,
            reliableEnded,
            unreliableReceived,
            unreliableError,
            unreliableEnded,
            iceConnectionStateChanged,
            connectionStateChanged,
            iceCandidateCallback,
            offerCallback
        ) {
            this.pc = new RTCPeerConnection();

            this.reliableDc = this.pc.createDataChannel("reliable");
            this.reliableReceived = reliableReceived;
            this.reliableError = reliableError;
            this.reliableEnded = reliableEnded;

            const onChannel = (name, eventType) => {
                const selectedPair = this.pc.sctp ? this.pc.sctp.transport.iceTransport.getSelectedCandidatePair() : null;
                const selectedPairJson = selectedPair
                    ? `, selected candidate pair: ${JSON.stringify(selectedPair)}`
                    : "";
                const message = (name[0].toUpperCase() + name.slice(1)) + " data channel " + eventType;
                webRtcState.log(message + selectedPairJson);
            };

            this.reliableDc.onopen = _ => onChannel("reliable", "opened");
            this.reliableDc.onmessage = message => this.reliableReceived(new Uint8Array(message.data));
            this.reliableDc.onerror = error => this.reliableError(error.error.toString());
            this.reliableDc.onclose = _ => {
                onChannel("reliable", "closed");
                this.reliableEnded();
            };

            this.unreliableDc = this.pc.createDataChannel("unreliable", {
                maxRetransmits: 0,
                ordered: false
            });
            this.unreliableReceived = unreliableReceived;
            this.unreliableError = unreliableError;
            this.unreliableEnded = unreliableEnded;

            this.unreliableDc.onopen = _ => onChannel("unreliable", "opened");
            this.unreliableDc.onmessage = message => this.unreliableReceived(new Uint8Array(message.data));
            this.unreliableDc.onerror = error => this.unreliableError(error.error.toString());
            this.unreliableDc.onclose = _ => {
                onChannel("unreliable", "closed");
                this.unreliableEnded();
            };

            this.createOffer = async iceRestart => {
                const offer = await this.pc.createOffer({ iceRestart });
                webRtcState.log(`Created offer\n${JSON.stringify(offer)}`);
                await this.pc.setLocalDescription(offer);
                await new Promise(r => setTimeout(r, webRtcState.offerAnnouncingDelay));
                const updatedOffer = this.pc.localDescription;
                webRtcState.log(`Updated offer after ${webRtcState.offerAnnouncingDelay} ms\n${JSON.stringify(updatedOffer)}`);
                if (this.pc.sctp && this.pc.sctp.transport.iceTransport.getLocalCandidates) {
                    webRtcState.log(`Local candidates\n${JSON.stringify(this.pc.sctp.transport.iceTransport.getLocalCandidates())}`);
                }
                offerCallback(JSON.stringify(updatedOffer));
            };
            this.onAnswer = function(answerJson) {
                webRtcState.log(`Answer received\n${answerJson}`);
                const answer = JSON.parse(answerJson);
                this.pc.setRemoteDescription(answer);
            };

            this.sendReliable = function(message) {
                if (this.reliableDc.readyState !== "open") return;
                this.reliableDc.send(message);
            };

            this.sendUnreliable = function(message) {
                if (this.unreliableDc.readyState !== "open") return;
                this.unreliableDc.send(message);
            };

            this.close = function() {
                this.reliableDc.close();
                this.unreliableDc.close();
                this.pc.close();
            };

            this.pc.onicecandidate = ({ candidate }) => {
                if (candidate !== null) {
                    const candidateJson = JSON.stringify(candidate.toJSON());
                    webRtcState.log(`Candidate received\n${candidateJson}`);
                    iceCandidateCallback(candidateJson);
                } else {
                    webRtcState.log("End of candidates");
                    iceCandidateCallback(candidate);
                }
            };

            this.onIceConnectionStateChanged = iceConnectionStateChanged;
            this.onConnectionStateChanged = connectionStateChanged;

            this.pc.oniceconnectionstatechange = _ => {
                webRtcState.log(`ICE connection state changed\n${this.pc.iceConnectionState}`);
                this.onIceConnectionStateChanged(this.pc.iceConnectionState);
            };

            this.pc.onconnectionstatechange = _ => {
                webRtcState.log(`Connection state changed\n${this.pc.connectionState}`);
                this.onConnectionStateChanged(this.pc.connectionState);
            };

            this.pc.onicegatheringstatechange = ({ target: connection }) => {
                webRtcState.log(`ICE gathering state changed\n${connection.iceGatheringState}`);
                if (connection.iceConnectionState === "failed") {
                    webRtcState.log(`ICE connection failed, restart`);
                }
            };

            this.pc.onsignalingstatechange = _ => {
                webRtcState.log(`Signaling state changed \n${this.pc.signalingState}`);
            };
        }

        const WebRtcReliableReceived = msg => {
            if (webRtcState.onReliableReceived === null) return;

            const buffer = _malloc(msg.length);
            HEAPU8.set(msg, buffer);

            try {
                Module.dynCall_viii(
                    webRtcState.onReliableReceived,
                    id,
                    buffer,
                    msg.length
                );
            } finally {
                _free(buffer);
            }
        };

        const WebRtcReliableError = msg => {
            if (webRtcState.onReliableError === null) return;

            const msgBytes = lengthBytesUTF8(msg) + 1;
            const msgBuffer = _malloc(msgBytes);
            stringToUTF8(msg, msgBuffer, msgBytes);

            try {
                Module.dynCall_vii(webRtcState.onReliableError, id, msgBuffer);
            } finally {
                _free(msgBuffer);
            }
        };

        const WebRtcReliableEnded = () => {
            if (webRtcState.onReliableEnded === null) return;

            Module.dynCall_vi(webRtcState.onReliableEnded, [id]);
        };

        const WebRtcUnreliableReceived = msg => {
            if (webRtcState.onUnreliableReceived === null) return;

            const buffer = _malloc(msg.length);
            HEAPU8.set(msg, buffer);

            try {
                Module.dynCall_viii(
                    webRtcState.onUnreliableReceived,
                    id,
                    buffer,
                    msg.length
                );
            } finally {
                _free(buffer);
            }
        };

        const WebRtcUnreliableError = msg => {
            if (webRtcState.onUnreliableError === null) return;

            const msgBytes = lengthBytesUTF8(msg) + 1;
            const msgBuffer = _malloc(msgBytes);
            stringToUTF8(msg, msgBuffer, msgBytes);

            try {
                Module.dynCall_vii(webRtcState.onUnreliableError, id, msgBuffer);
            } finally {
                _free(msgBuffer);
            }
        };

        const WebRtcUnreliableEnded = () => {
            if (webRtcState.onUnreliableEnded === null) return;

            Module.dynCall_vi(webRtcState.onUnreliableEnded, [id]);
        };

        const WebRtcOfferCallback = msg => {
            if (webRtcState.onOffer === null) return;

            const msgBytes = lengthBytesUTF8(msg) + 1;
            const msgBuffer = _malloc(msgBytes);
            stringToUTF8(msg, msgBuffer, msgBytes);

            try {
                Module.dynCall_vii(webRtcState.onOffer, id, msgBuffer);
            } finally {
                _free(msgBuffer);
            }
        };

        const WebRtcIceCandidateCallback = msg => {
            if (webRtcState.onIceCandidate === null) {
                webRtcState.log("onIceCandidate callback is not set");
                return;
            }
            if (!msg) {
                Module.dynCall_vii(webRtcState.onIceCandidate, id, null);
                return;
            }

            const msgBytes = lengthBytesUTF8(msg) + 1;
            const msgBuffer = _malloc(msgBytes);
            stringToUTF8(msg, msgBuffer, msgBytes);

            try {
                Module.dynCall_vii(webRtcState.onIceCandidate, id, msgBuffer);
            } finally {
                _free(msgBuffer);
            }
        };

        const IceConnectionStateChanged = (state) => {
            const msgBytes = lengthBytesUTF8(state) + 1;
            const msgBuffer = _malloc(msgBytes);
            stringToUTF8(state, msgBuffer, msgBytes);
            try {
                Module.dynCall_vii(
                    webRtcState.onIceConnectionStateChanged,
                    id,
                    msgBuffer
                );
            } finally {
                _free(msgBuffer);
            }
        };

        const ConnectionStateChanged = (state) => {
            const msgBytes = lengthBytesUTF8(state) + 1;
            const msgBuffer = _malloc(msgBytes);
            stringToUTF8(state, msgBuffer, msgBytes);
            try {
                Module.dynCall_vii(webRtcState.onConnectionStateChanged, id, msgBuffer);
            } finally {
                _free(msgBuffer);
            }
        };

        webRtcState.log("Receiving callbacks created");

        webRtcState.instances[id] = new WebRtcClient(
            WebRtcReliableReceived,
            WebRtcReliableError,
            WebRtcReliableEnded,
            WebRtcUnreliableReceived,
            WebRtcUnreliableError,
            WebRtcUnreliableEnded,
            IceConnectionStateChanged,
            ConnectionStateChanged,
            WebRtcIceCandidateCallback,
            WebRtcOfferCallback
        );

        webRtcState.log("Client allocated");

        return id;
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcFree: function (id) {
        const instance = webRtcState.instances[id];
        if (!instance) return;

        delete webRtcState.instances[id];
        instance.close();
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcSetOfferAnnouncingDelay: function (delayMs) {
        webRtcState.offerAnnouncingDelay = delayMs;
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcSetOnReliableReceived: function (callback) {
        webRtcState.onReliableReceived = callback;
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcSetOnReliableError: function (callback) {
        webRtcState.onReliableError = callback;
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcSetOnReliableEnded: function (callback) {
        webRtcState.onReliableEnded = callback;
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcSetOnUnreliableReceived: function (callback) {
        webRtcState.onUnreliableReceived = callback;
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcSetOnUnreliableError: function (callback) {
        webRtcState.onUnreliableError = callback;
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcSetOnUnreliableEnded: function (callback) {
        webRtcState.onUnreliableEnded = callback;
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcSetOnOffer: function (callback) {
        webRtcState.onOffer = callback;
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcSetOnIceCandidate: function (callback) {
        webRtcState.onIceCandidate = callback;
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcSetOnIceConnectionStateChanged: function (callback) {
        webRtcState.onIceConnectionStateChanged = callback;
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcSetOnConnectionStateChanged: function (callback) {
        webRtcState.onConnectionStateChanged = callback;
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcCreateOffer: function (id, iceRestart) {
        webRtcState.log("Creating offer" + (iceRestart ? "with restart" : "without restart"));

        const instance = webRtcState.instances[id];
        if (!instance) {
            webRtcState.log(`Instance not found for ID: ${id}`);
            return;
        }

        instance.createOffer(iceRestart);
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcOnAnswer: function (id, answer) {
        const instance = webRtcState.instances[id];
        if (!instance) return;

        let answerStr;
        if (UTF8ToString !== undefined) answerStr = UTF8ToString(answer);
        else answerStr = Pointer_stringify(answer);
        instance.onAnswer(answerStr);
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcSendReliable: function (id, bufferPtr, length) {
        const instance = webRtcState.instances[id];
        if (!instance) return;

        instance.sendReliable(HEAPU8.buffer.slice(bufferPtr, bufferPtr + length));
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcSendUnreliable: function (id, bufferPtr, length) {
        const instance = webRtcState.instances[id];
        if (!instance) return;

        instance.sendUnreliable(HEAPU8.buffer.slice(bufferPtr, bufferPtr + length));
    },

    // biome-ignore lint/complexity/useArrowFunction: <explanation>
    WebRtcClose: function (id) {
        const instance = webRtcState.instances[id];
        if (!instance) return;

        instance.close();
    }
};

autoAddDeps(LibraryWebRtc, "$webRtcState");
mergeInto(LibraryManager.library, LibraryWebRtc);
