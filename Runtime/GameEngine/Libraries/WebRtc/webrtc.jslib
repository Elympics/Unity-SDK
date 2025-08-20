const logWebRtc = message => console.log(`[${new Date().toISOString()}] [WebRTC] ${message}`);
const logErrorWebRtc = error => console.error(error, `[${new Date().toISOString()}] [WebRTC]`);

const LibraryWebRtc = {
    $webRtcState: {
        instances: {},
        lastId: 0,

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
        logWebRtc(`Allocating client #${id}`);

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
            const config = {
                iceServers: [
                    {urls: 'stun:stun.l.google.com:19302'},
                ],
            };
            this.pc = new RTCPeerConnection(config);

            this.reliableDc = this.pc.createDataChannel("reliable");
            this.reliableReceived = reliableReceived;
            this.reliableError = reliableError;
            this.reliableEnded = reliableEnded;

            this.reliableDc.onopen = function () {
                const selectedPair = this.pc.sctp != null
                    ? `, selected candidate pair: ${this.pc.sctp.transport.iceTransport.getSelectedCandidatePair()}`
                    : "";
                logWebRtc("Reliable data channel opened" + selectedPair);
            }.bind(this);
            this.reliableDc.onmessage = function (message) {
                this.reliableReceived(new Uint8Array(message.data));
            }.bind(this);
            this.reliableDc.onerror = function (error) {
                this.reliableError(error.error.toString());
            }.bind(this);
            this.reliableDc.onclose = function () {
                const selectedPair = this.pc.sctp != null
                    ? `, selected candidate pair: ${this.pc.sctp.transport.iceTransport.getSelectedCandidatePair()}`
                    : "";
                logWebRtc("Reliable data channel closed" + selectedPair);
                this.reliableEnded();
            }.bind(this);

            this.unreliableDc = this.pc.createDataChannel("unreliable", {
                maxRetransmits: 0,
                ordered: false
            });
            this.unreliableReceived = unreliableReceived;
            this.unreliableError = unreliableError;
            this.unreliableEnded = unreliableEnded;

            this.unreliableDc.onopen = function () {
                const selectedPair = this.pc.sctp != null
                    ? `, selected candidate pair: ${this.pc.sctp.transport.iceTransport.getSelectedCandidatePair()}`
                    : "";
                logWebRtc("Unreliable data channel opened" + selectedPair);
            }.bind(this);
            this.unreliableDc.onmessage = function (message) {
                this.unreliableReceived(new Uint8Array(message.data));
            }.bind(this);
            this.unreliableDc.onerror = function (error) {
                this.unreliableError(error.error.toString());
            }.bind(this);
            this.unreliableDc.onclose = function () {
                const selectedPair = this.pc.sctp != null
                    ? `, selected candidate pair: ${this.pc.sctp.transport.iceTransport.getSelectedCandidatePair()}`
                    : "";
                logWebRtc("Unreliable data channel closed" + selectedPair);
                this.unreliableEnded();
            }.bind(this);

            this.createOffer = function (iceRestart) {
                this.pc
                    .createOffer({iceRestart: iceRestart})
                    .then(
                        function (offer) {
                            try {
                                return this.pc.setLocalDescription(offer).then(() => offer);
                            } catch (error) {
                                logErrorWebRtc(error);
                            }
                        }.bind(this)
                    )
                    .then((offer) => {
                        const offerJson = JSON.stringify(offer);
                        offerCallback(offerJson);
                    });
            }.bind(this);
            this.onAnswer = function (answerJson) {
                logWebRtc(`Answer received\n${answerJson}`);
                const answer = JSON.parse(answerJson);
                this.pc.setRemoteDescription(answer);
            }.bind(this);

            this.sendReliable = function (message) {
                if (this.reliableDc.readyState !== "open") return;
                this.reliableDc.send(message);
            }.bind(this);

            this.sendUnreliable = function (message) {
                if (this.unreliableDc.readyState !== "open") return;
                this.unreliableDc.send(message);
            }.bind(this);

            this.close = function () {
                this.reliableDc.close();
                this.unreliableDc.close();
                this.pc.close();
            }.bind(this);

            this.pc.onicecandidate = (ev) => {
                if (ev.candidate !== null) {
                    const candidateJson = ev.candidate.toJSON();
                    logWebRtc(`Candidate received\n${candidateJson}`);
                    iceCandidateCallback(candidateJson);
                } else {
                    logWebRtc('End of candidates');
                }
            };

            this.onIceConnectionStateChanged = iceConnectionStateChanged;
            this.onConnectionStateChanged = connectionStateChanged;

            this.pc.oniceconnectionstatechange = (ev) => {
                logWebRtc(`ICE connection state changed\n${this.pc.iceConnectionState}`);
                this.onIceConnectionStateChanged(this.pc.iceConnectionState);
            };

            this.pc.onconnectionstatechange = (ev) => {
                logWebRtc(`Connection state changed\n${this.pc.connectionState}`);
                this.onConnectionStateChanged(this.pc.connectionState);
            };

            this.pc.onicegatheringstatechange = (ev) => {
                const connection = ev.target;
                logWebRtc(`ICE gathering state changed\n${connection.iceGatheringState}`);
                if (connection.iceConnectionState === "failed") {
                    logWebRtc(`ICE connection failed, restart`);
                }
            };

            this.pc.onsignalingstatechange = (ev) => {
                logWebRtc(`Signaling state changed \n${this.pc.signalingState}`);
            };
        }

        const WebRtcReliableReceived = (msg) => {
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

        const WebRtcReliableError = (msg) => {
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

        const WebRtcUnreliableReceived = (msg) => {
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

        const WebRtcUnreliableError = (msg) => {
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

        const WebRtcOfferCallback = (msg) => {
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

        const WebRtcIceCandidateCallback = (msg) => {
            if (webRtcState.onIceCandidate === null) {
                logWebRtc("onIceCandidate callback is not set");
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

        logWebRtc("Receiving callbacks created");

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

        logWebRtc("Client allocated");

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
        logWebRtc("Creating offer" + (iceRestart ? "with restart" : "without restart"));

        const instance = webRtcState.instances[id];
        if (!instance) {
            logWebRtc(`Instance not found for ID: ${id}`);
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
