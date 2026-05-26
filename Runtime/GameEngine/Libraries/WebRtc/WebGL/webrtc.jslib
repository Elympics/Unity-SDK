const LibraryWebRtc = {
    $webRtcState: {
        instances: {},
        lastId: 0,

        logToConsole: message => console.log(`[${new Date().toISOString()}] [WebRTC] ${message}`),

        offerAnnouncingDelay: 1000,
        onReliableOpened: null,
        onReliableReceived: null,
        onReliableError: null,
        onReliableEnded: null,
        onUnreliableOpened: null,
        onUnreliableReceived: null,
        onUnreliableError: null,
        onUnreliableEnded: null,
        onOffer: null,
        onIceCandidate: null,
        onCandidatePairChosen: null,
        onIceConnectionStateChanged: null,
        onConnectionStateChanged: null,
        onLog: null,
        onLogWarning: null,
        onLogError: null
    },

    WebRtcAllocate: function () {
        const id = webRtcState.lastId++;
        webRtcState.logToConsole(`Allocating client #${id}`);

        function WebRtcClient(
            reliableOpened,
            reliableReceived,
            reliableError,
            reliableEnded,
            unreliableOpened,
            unreliableReceived,
            unreliableError,
            unreliableEnded,
            iceConnectionStateChanged,
            connectionStateChanged,
            iceCandidateCallback,
            offerCallback,
            candidatePairChosenCallback,
            logCallback,
            logWarningCallback,
            logErrorCallback
        ) {
            this.rtcConfig = {};
            this.pc = new RTCPeerConnection(this.rtcConfig);

            this.reliableDc = this.pc.createDataChannel("reliable");
            this.reliableOpened = reliableOpened;
            this.reliableReceived = reliableReceived;
            this.reliableError = reliableError;
            this.reliableEnded = reliableEnded;

            const onChannel = (name, eventType) => {
                const selectedPair = (this.pc.sctp && this.pc.sctp.transport && this.pc.sctp.transport.iceTransport && typeof this.pc.sctp.transport.iceTransport.getSelectedCandidatePair === 'function')
                    ? this.pc.sctp.transport.iceTransport.getSelectedCandidatePair()
                    : null;
                const selectedPairJson = selectedPair
                    ? `, selected candidate pair: ${JSON.stringify(selectedPair)}`
                    : "";
                const message = (name[0].toUpperCase() + name.slice(1)) + " data channel " + eventType;
                logCallback('onChannel', message + selectedPairJson);
            };

            const buildErrorMessage = (err, details, baseMessage) => {
                let message = baseMessage;

                // Add details if available
                if (details !== undefined) {
                    message += ` | Error detail: ${details}`;
                }

                switch (details) {
                    case "sdp-syntax-error":
                        if (err.sdpLineNumber !== undefined) {
                            message += ` | SDP syntax error in line ${err.sdpLineNumber}`;
                        } else {
                            message += ` | SDP syntax error (line number not supported)`;
                        }
                        break;
                    case "idp-load-failure":
                        if (err.httpRequestStatusCode !== undefined) {
                            message += ` | Identity provider load failure: HTTP error ${err.httpRequestStatusCode}`;
                        } else {
                            message += ` | Identity provider load failure (HTTP status code not supported)`;
                        }
                        break;
                    case "sctp-failure":
                        if (err.sctpCauseCode !== undefined) {
                            if (typeof sctpCauseCodes !== 'undefined' && err.sctpCauseCode < sctpCauseCodes.length) {
                                message += ` | SCTP failure: ${sctpCauseCodes[err.sctpCauseCode]}`;
                            } else {
                                message += ` | SCTP failure: cause code ${err.sctpCauseCode}`;
                            }
                        } else {
                            message += ` | SCTP failure (cause code not supported)`;
                        }
                        break;
                    case "dtls-failure":
                        if (err.receivedAlert !== undefined) {
                            message += ` | Received DTLS failure alert: ${err.receivedAlert}`;
                        } else {
                            message += ` | Received DTLS failure alert (not supported)`;
                        }
                        if (err.sentAlert !== undefined) {
                            message += ` | Sent DTLS failure alert: ${err.sentAlert}`;
                        } else {
                            message += ` | Sent DTLS failure alert (not supported)`;
                        }
                        break;
                    case "data-channel-failure":
                        message += ' | The connection RTCDataChannel has failed.';
                        break;
                }
                return message;
            };

            this.reliableDc.onopen = _ => {
                onChannel("reliable", "opened");
                this.reliableOpened();
            };
            this.reliableDc.onmessage = message => this.reliableReceived(new Uint8Array(message.data));
            this.reliableDc.addEventListener("error", (ev) => {
                const err = ev.error || ev;
                const errorMessage = err.message || err.toString() || 'Unknown error';
                logErrorCallback('reliableDc.onerror', `Reliable Error: \n${errorMessage}`);
                const message = buildErrorMessage(err, err.errorDetail, errorMessage);
                this.reliableError(message);
            });
            this.reliableDc.onclose = _ => {
                onChannel("reliable", "closed");
                this.reliableEnded();
            };

            this.unreliableDc = this.pc.createDataChannel("unreliable", {
                maxRetransmits: 0,
                ordered: false
            });
            this.unreliableOpened = unreliableOpened;
            this.unreliableReceived = unreliableReceived;
            this.unreliableError = unreliableError;
            this.unreliableEnded = unreliableEnded;

            this.unreliableDc.onopen = _ => {
                onChannel("unreliable", "opened");
                this.unreliableOpened();
            };
            this.unreliableDc.onmessage = message => this.unreliableReceived(new Uint8Array(message.data));
            this.unreliableDc.addEventListener("error", (ev) => {
                const err = ev.error || ev;
                const errorMessage = err.message || err.toString() || 'Unknown error';
                logErrorCallback('unreliableDc.onerror', `Unreliable Error: \n${errorMessage}`);
                const message = buildErrorMessage(err, err.errorDetail, errorMessage);
                this.unreliableError(message);
            });

            this.unreliableDc.onclose = _ => {
                onChannel("unreliable", "closed");
                this.unreliableEnded();
            };

            this.pendingOfferResolvers = [];

            this.createOffer = async iceRestart => {
                const offer = await this.pc.createOffer({iceRestart});
                logCallback('createOffer', `Created offer\n${JSON.stringify(offer)}`);
                await this.pc.setLocalDescription(offer);

                let resolver;
                logCallback('createOffer', `Gathering ICE candidates...`);
                const reason = await Promise.race([
                    new Promise(r => setTimeout(() => r('timeout'), webRtcState.offerAnnouncingDelay)),
                    new Promise(r => {
                        resolver = () => r('candidates gathered');
                        this.pendingOfferResolvers.push(resolver);
                    })
                ]);
                logCallback('createOffer', `ICE candidates gathering ended due to: ${reason}.`);
                const index = this.pendingOfferResolvers.indexOf(resolver);
                if (index > -1) {
                    this.pendingOfferResolvers.splice(index, 1);
                }

                const updatedOffer = this.pc.localDescription;
                if (this.pc.sctp && this.pc.sctp.transport && this.pc.sctp.transport.iceTransport && typeof this.pc.sctp.transport.iceTransport.getLocalCandidates === 'function') {
                    logCallback('createOffer', `Local candidates\n${JSON.stringify(this.pc.sctp.transport.iceTransport.getLocalCandidates())}`);
                }
                offerCallback(JSON.stringify(updatedOffer));
            };

            this.candidatePairCt = [false];

            this.onAnswer = async answerJson => {
                logCallback('onAnswer', `Answer received\n${answerJson}`);
                const answer = JSON.parse(answerJson);
                await this.pc.setRemoteDescription(answer);
                this.candidatePairCt[0] = true;
                this.candidatePairCt = [false];
                await this.waitForCandidatePair(this.candidatePairCt);
            };

            this.waitForCandidatePair = async ct => {
                while (!ct[0]) {
                    const stats = await this.pc.getStats();
                    const nominatedPair = Array.from(stats.values()).find(s => s.type === "candidate-pair" && s.nominated);
                    if (nominatedPair) {
                        const localCandidate = stats.get(nominatedPair.localCandidateId);
                        const remoteCandidate = stats.get(nominatedPair.remoteCandidateId);
                        logCallback('waitForCandidatePair', "Chosen candidate pair: " + JSON.stringify([localCandidate, remoteCandidate]));
                        candidatePairChosenCallback(JSON.stringify(localCandidate), JSON.stringify(remoteCandidate));
                        return;
                    }
                    await new Promise(r => setTimeout(r, 200));
                }
            };

            this.sendReliable = message => {
                if (this.reliableDc.readyState !== "open") return;
                this.reliableDc.send(message);
            };

            this.sendUnreliable = message => {
                if (this.unreliableDc.readyState !== "open") return;
                this.unreliableDc.send(message);
            };

            this.setIceServers = iceServers => {
                this.rtcConfig.iceServers = iceServers;
                logCallback('setIceServers', "Updating rtcConfig: " + JSON.stringify(this.rtcConfig));
                this.pc.setConfiguration(this.rtcConfig);
            };

            this.close = () => {
                this.candidatePairCt[0] = true;
                this.reliableDc.close();
                this.unreliableDc.close();
                this.pc.close();
            };

            this.pc.onicecandidate = ({candidate}) => {
                if (candidate !== null) {
                    const candidateJson = JSON.stringify(candidate.toJSON());
                    logCallback('pc.onicecandidate', `Candidate received\n${candidateJson}`);
                    iceCandidateCallback(candidateJson);
                } else {
                    logCallback('pc.onicecandidate', "End of candidates");
                    while (this.pendingOfferResolvers.length > 0) {
                        const resolver = this.pendingOfferResolvers.pop();
                        resolver();
                    }
                    iceCandidateCallback(candidate);
                }
            };

            this.onIceConnectionStateChanged = iceConnectionStateChanged;
            this.onConnectionStateChanged = connectionStateChanged;

            this.pc.oniceconnectionstatechange = _ => {
                logCallback('pc.oniceconnectionstatechange', `ICE connection state changed\n${this.pc.iceConnectionState}`);
                this.onIceConnectionStateChanged(this.pc.iceConnectionState);
            };

            this.pc.onconnectionstatechange = _ => {
                logCallback('pc.onconnectionstatechange', `Connection state changed\n${this.pc.connectionState}`);
                this.onConnectionStateChanged(this.pc.connectionState);
            };

            this.pc.onicegatheringstatechange = ({target: connection}) => {
                logCallback('pc.onicegatheringstatechange', `ICE gathering state changed\n${connection.iceGatheringState}`);
                if (connection.iceConnectionState === "failed") {
                    logErrorCallback('pc.oniceconnectionstatechange', `ICE connection failed, restart`);
                }
            };

            this.pc.onsignalingstatechange = _ => {
                logCallback('pc.onsignalingstatechange', `Signaling state changed \n${this.pc.signalingState}`);
            };
        }

        const WebRtcReliableOpened = () => {
            if (webRtcState.onReliableOpened === null) return;

            Module.dynCall_vi(webRtcState.onReliableOpened, id);
        };

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

            Module.dynCall_vi(webRtcState.onReliableEnded, id);
        };

        const WebRtcUnreliableOpened = () => {
            if (webRtcState.onUnreliableOpened === null) return;

            Module.dynCall_vi(webRtcState.onUnreliableOpened, id);
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

            Module.dynCall_vi(webRtcState.onUnreliableEnded, id);
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
                WebRtcLogCallback('WebRtcIceCandidateCallback', "onIceCandidate callback is not set");
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

        const WebRtcCandidatePairChosenCallback = (localCandidate, remoteCandidate) => {
            const localCandidateBytes = lengthBytesUTF8(localCandidate) + 1;
            const localCandidateBuffer = _malloc(localCandidateBytes);
            stringToUTF8(localCandidate, localCandidateBuffer, localCandidateBytes);
            const remoteCandidateBytes = lengthBytesUTF8(remoteCandidate) + 1;
            const remoteCandidateBuffer = _malloc(remoteCandidateBytes);
            stringToUTF8(remoteCandidate, remoteCandidateBuffer, remoteCandidateBytes);

            try {
                Module.dynCall_viii(webRtcState.onCandidatePairChosen, id, localCandidateBuffer, remoteCandidateBuffer);
            } finally {
                _free(localCandidateBuffer);
                _free(remoteCandidateBuffer);
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

        const WebRtcLogCallback = (methodName, logMessage) => {
            const methodNameBytes = lengthBytesUTF8(methodName) + 1;
            const methodNameBuffer = _malloc(methodNameBytes);
            stringToUTF8(methodName, methodNameBuffer, methodNameBytes);
            const logMessageBytes = lengthBytesUTF8(logMessage) + 1;
            const logMessageBuffer = _malloc(logMessageBytes);
            stringToUTF8(logMessage, logMessageBuffer, logMessageBytes);

            try {
                Module.dynCall_viii(webRtcState.onLog, id, methodNameBuffer, logMessageBuffer);
            } finally {
                _free(methodNameBuffer);
                _free(logMessageBuffer);
            }
        };

        const WebRtcLogWarningCallback = (methodName, logMessage) => {
            const methodNameBytes = lengthBytesUTF8(methodName) + 1;
            const methodNameBuffer = _malloc(methodNameBytes);
            stringToUTF8(methodName, methodNameBuffer, methodNameBytes);
            const logMessageBytes = lengthBytesUTF8(logMessage) + 1;
            const logMessageBuffer = _malloc(logMessageBytes);
            stringToUTF8(logMessage, logMessageBuffer, logMessageBytes);

            try {
                Module.dynCall_viii(webRtcState.onLogWarning, id, methodNameBuffer, logMessageBuffer);
            } finally {
                _free(methodNameBuffer);
                _free(logMessageBuffer);
            }
        };

        const WebRtcLogErrorCallback = (methodName, logMessage) => {
            const methodNameBytes = lengthBytesUTF8(methodName) + 1;
            const methodNameBuffer = _malloc(methodNameBytes);
            stringToUTF8(methodName, methodNameBuffer, methodNameBytes);
            const logMessageBytes = lengthBytesUTF8(logMessage) + 1;
            const logMessageBuffer = _malloc(logMessageBytes);
            stringToUTF8(logMessage, logMessageBuffer, logMessageBytes);

            try {
                Module.dynCall_viii(webRtcState.onLogError, id, methodNameBuffer, logMessageBuffer);
            } finally {
                _free(methodNameBuffer);
                _free(logMessageBuffer);
            }
        };

        webRtcState.logToConsole("Receiving callbacks created");

        webRtcState.instances[id] = new WebRtcClient(
            WebRtcReliableOpened,
            WebRtcReliableReceived,
            WebRtcReliableError,
            WebRtcReliableEnded,
            WebRtcUnreliableOpened,
            WebRtcUnreliableReceived,
            WebRtcUnreliableError,
            WebRtcUnreliableEnded,
            IceConnectionStateChanged,
            ConnectionStateChanged,
            WebRtcIceCandidateCallback,
            WebRtcOfferCallback,
            WebRtcCandidatePairChosenCallback,
            WebRtcLogCallback,
            WebRtcLogWarningCallback,
            WebRtcLogErrorCallback
        );

        webRtcState.logToConsole("Client allocated");

        return id;
    },

    WebRtcFree: function (id) {
        const instance = webRtcState.instances[id];
        if (!instance) return;

        delete webRtcState.instances[id];
        instance.close();
    },

    WebRtcSetIceServers: function (id, iceServersJsonPtr) {
        const instance = webRtcState.instances[id];
        if (!instance) {
            webRtcState.logToConsole(`Instance not found for ID: ${id}`);
            return;
        }

        let iceServers = null;
        if (iceServersJsonPtr) {
            const json = UTF8ToString(iceServersJsonPtr);
            if (json && json.length > 0) {
                const parsed = JSON.parse(json);
                iceServers = Array.isArray(parsed) ? parsed : (parsed.iceServers || [parsed]);
            }
        }

        if (iceServers) {
            instance.setIceServers(iceServers);
        }
    },

    WebRtcSetOfferAnnouncingDelay: function (delayMs) {
        webRtcState.offerAnnouncingDelay = delayMs;
    },

    WebRtcSetOnReliableOpened: function (callback) {
        webRtcState.onReliableOpened = callback;
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

    WebRtcSetOnUnreliableOpened: function (callback) {
        webRtcState.onUnreliableOpened = callback;
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

    WebRtcSetOnIceCandidate: function (callback) {
        webRtcState.onIceCandidate = callback;
    },

    WebRtcSetOnIceConnectionStateChanged: function (callback) {
        webRtcState.onIceConnectionStateChanged = callback;
    },

    WebRtcSetOnConnectionStateChanged: function (callback) {
        webRtcState.onConnectionStateChanged = callback;
    },

    WebRtcSetOnCandidatePairChosen: function (callback) {
        webRtcState.onCandidatePairChosen = callback;
    },

    WebRtcSetOnLog: function (callback) {
        webRtcState.onLog = callback;
    },
    WebRtcSetOnLogWarning: function (callback) {
        webRtcState.onLogWarning = callback;
    },
    WebRtcSetOnLogError: function (callback) {
        webRtcState.onLogError = callback;
    },

    WebRtcCreateOffer: function (id, iceRestart) {
        webRtcState.logToConsole("Creating offer " + (iceRestart ? "with restart" : "without restart"));

        const instance = webRtcState.instances[id];
        if (!instance) {
            webRtcState.logToConsole(`Instance not found for ID: ${id}`);
            return;
        }

        instance.createOffer(iceRestart);
    },

    WebRtcOnAnswer: function (id, answer) {
        const instance = webRtcState.instances[id];
        if (!instance) return;

        let answerStr;
        if (UTF8ToString !== undefined) answerStr = UTF8ToString(answer);
        else answerStr = Pointer_stringify(answer);
        instance.onAnswer(answerStr);
    },

    WebRtcSendReliable: function (id, bufferPtr, length) {
        const instance = webRtcState.instances[id];
        if (!instance) return;

        instance.sendReliable(HEAPU8.buffer.slice(bufferPtr, bufferPtr + length));
    },

    WebRtcSendUnreliable: function (id, bufferPtr, length) {
        const instance = webRtcState.instances[id];
        if (!instance) return;

        instance.sendUnreliable(HEAPU8.buffer.slice(bufferPtr, bufferPtr + length));
    },

    WebRtcClose: function (id) {
        const instance = webRtcState.instances[id];
        if (!instance) return;

        instance.close();
    }
};

autoAddDeps(LibraryWebRtc, "$webRtcState");
mergeInto(LibraryManager.library, LibraryWebRtc);
