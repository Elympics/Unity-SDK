/**
 * @typedef {{
 *   $webRtcState: WebRtcState,
 *   WebRtcAllocate: () => number,
 *   WebRtcFree: (id: number) => void,
 *   WebRtcSetIceServers: (id: number, iceServersJsonPtr: Pointer | null) => void,
 *   WebRtcSetOfferAnnouncingDelay: (delayMs: number) => void,
 *   WebRtcCreateDataChannel: (id: number, labelPtr: Pointer, reliable: boolean) => number,
 *   WebRtcSendOnChannel: (id: number, channelId: number, bufferPtr: Pointer, length: number) => void,
 *   WebRtcCloseChannel: (id: number, channelId: number) => void,
 *   WebRtcSetOnChannelOpened: (callback: FunctionPointer) => void,
 *   WebRtcSetOnChannelReceived: (callback: FunctionPointer) => void,
 *   WebRtcSetOnChannelError: (callback: FunctionPointer) => void,
 *   WebRtcSetOnChannelEnded: (callback: FunctionPointer) => void,
 *   WebRtcSetOnOffer: (callback: FunctionPointer) => void,
 *   WebRtcSetOnIceCandidate: (callback: FunctionPointer) => void,
 *   WebRtcSetOnIceConnectionStateChanged: (callback: FunctionPointer) => void,
 *   WebRtcSetOnConnectionStateChanged: (callback: FunctionPointer) => void,
 *   WebRtcSetOnCandidatePairChosen: (callback: FunctionPointer) => void,
 *   WebRtcSetOnLog: (callback: FunctionPointer) => void,
 *   WebRtcSetOnLogWarning: (callback: FunctionPointer) => void,
 *   WebRtcSetOnLogError: (callback: FunctionPointer) => void,
 *   WebRtcCreateOffer: (id: number, iceRestart: boolean) => void,
 *   WebRtcOnAnswer: (id: number, answer: Pointer) => void,
 *   WebRtcClose: (id: number) => void,
 * }} LibraryWebRtc
 */

/**
 * @typedef {{
 *   dc: RTCDataChannel,
 *   label: string,
 * }} WebRtcDataChannelEntry
 */

/**
 * @typedef {{
 *   rtcConfig: RTCConfiguration,
 *   pc: RTCPeerConnection,
 *   channels: {[channelId: number]: WebRtcDataChannelEntry},
 *   nextChannelId: number,
 *   createDataChannel: (label: string, reliable: boolean) => number,
 *   sendOnChannel: (channelId: number, message: ArrayBuffer) => void,
 *   closeChannel: (channelId: number) => void,
 *   pendingOfferResolvers: (() => void)[],
 *   createOffer: (iceRestart: boolean) => Promise<void>,
 *   candidatePairCt: boolean[],
 *   onAnswer: (answerJson: string) => Promise<void>,
 *   waitForCandidatePair: (ct: boolean[]) => Promise<void>,
 *   setIceServers: (iceServers: RTCIceServer[]) => void,
 *   close: () => void,
 *   onIceConnectionStateChanged: (state: string) => void,
 *   onConnectionStateChanged: (state: string) => void,
 * }} WebRtcClient
 */

/**
 * @typedef {{
 *   instances: {[key: number]: WebRtcClient},
 *   lastId: number,
 *   logToConsole: (message: string) => void,
 *   offerAnnouncingDelay: number,
 *   onChannelOpened: FunctionPointer | null,
 *   onChannelReceived: FunctionPointer | null,
 *   onChannelError: FunctionPointer | null,
 *   onChannelEnded: FunctionPointer | null,
 *   onOffer: FunctionPointer | null,
 *   onIceCandidate: FunctionPointer | null,
 *   onCandidatePairChosen: FunctionPointer | null,
 *   onIceConnectionStateChanged: FunctionPointer | null,
 *   onConnectionStateChanged: FunctionPointer | null,
 *   onLog: FunctionPointer | null,
 *   onLogWarning: FunctionPointer | null,
 *   onLogError: FunctionPointer | null,
 * }} WebRtcState
 */

// Unity mergeInto aliases $webRtcState as webRtcState at build time.
/* eslint-disable no-unassigned-vars */
// noinspection ES6ConvertVarToLetConst
/** @type {WebRtcState} */ var webRtcState;
/* eslint-enable no-unassigned-vars */

/** @type {LibraryWebRtc} */
const LibraryWebRtc = {
    /** @alias webRtcState */
    /** @type {WebRtcState} */ $webRtcState: {
        /** @type {{[key: number]: WebRtcClient}} */ instances: {},
        lastId: 0,

        logToConsole: (/** @type {string} */ message) => console.log(`[${new Date().toISOString()}] [WebRTC] ${message}`),

        offerAnnouncingDelay: 1000,
        /** @type {FunctionPointer | null} */ onChannelOpened: null,
        /** @type {FunctionPointer | null} */ onChannelReceived: null,
        /** @type {FunctionPointer | null} */ onChannelError: null,
        /** @type {FunctionPointer | null} */ onChannelEnded: null,
        /** @type {FunctionPointer | null} */ onOffer: null,
        /** @type {FunctionPointer | null} */ onIceCandidate: null,
        /** @type {FunctionPointer | null} */ onCandidatePairChosen: null,
        /** @type {FunctionPointer | null} */ onIceConnectionStateChanged: null,
        /** @type {FunctionPointer | null} */ onConnectionStateChanged: null,
        /** @type {FunctionPointer | null} */ onLog: null,
        /** @type {FunctionPointer | null} */ onLogWarning: null,
        /** @type {FunctionPointer | null} */ onLogError: null
    },

    WebRtcAllocate: function () {
        const id = webRtcState.lastId++;
        webRtcState.logToConsole(`Allocating client #${id}`);

        /**
         * @constructor
         * @param {(channelId: number) => void} channelOpened
         * @param {(channelId: number, msg: Uint8Array) => void} channelReceived
         * @param {(channelId: number, msg: string) => void} channelError
         * @param {(channelId: number) => void} channelEnded
         * @param {(state: string) => void} iceConnectionStateChanged
         * @param {(state: string) => void} connectionStateChanged
         * @param {(candidateJson: string | null) => void} iceCandidateCallback
         * @param {(offerJson: string) => void} offerCallback
         * @param {(localCandidate: string, remoteCandidate: string) => void} candidatePairChosenCallback
         * @param {(methodName: string, message: string) => void} logCallback
         * @param {(methodName: string, message: string) => void} logWarningCallback
         * @param {(methodName: string, message: string) => void} logErrorCallback
         */
        function WebRtcClient(
            channelOpened,
            channelReceived,
            channelError,
            channelEnded,
            iceConnectionStateChanged,
            connectionStateChanged,
            iceCandidateCallback,
            offerCallback,
            candidatePairChosenCallback,
            logCallback,
            logWarningCallback,
            logErrorCallback
        ) {
            this.rtcConfig = /** @type {RTCConfiguration} */ ({});
            this.pc = new RTCPeerConnection(this.rtcConfig);

            const onChannel = (/** @type {string} */ label, /** @type {string} */ eventType) => {
                const selectedPair = (this.pc.sctp && this.pc.sctp.transport && this.pc.sctp.transport.iceTransport && typeof this.pc.sctp.transport.iceTransport.getSelectedCandidatePair === 'function')
                    ? this.pc.sctp.transport.iceTransport.getSelectedCandidatePair()
                    : null;
                const selectedPairJson = selectedPair
                    ? `, selected candidate pair: ${JSON.stringify(selectedPair)}`
                    : "";
                const message = `Data channel '${label}' ${eventType}`;
                logCallback('onChannel', message + selectedPairJson);
            };

            const buildErrorMessage = (/** @type {any} */ err, /** @type {string | undefined} */ details, /** @type {string} */ baseMessage) => {
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
                            if (typeof err.sctpCauseCodes !== 'undefined' && err.sctpCauseCode < err.sctpCauseCodes.length) {
                                message += ` | SCTP failure: ${err.sctpCauseCodes[err.sctpCauseCode]}`;
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

            /** @type {{[channelId: number]: WebRtcDataChannelEntry}} */
            this.channels = {};
            this.nextChannelId = 0;

            this.createDataChannel = (/** @type {string} */ label, /** @type {boolean} */ reliable) => {
                const channelId = this.nextChannelId++;
                const dc = reliable
                    ? this.pc.createDataChannel(label)
                    : this.pc.createDataChannel(label, { maxRetransmits: 0, ordered: false });
                this.channels[channelId] = { dc, label };

                dc.onopen = _ => {
                    // Notify first: onChannel's candidate-pair lookup can throw if pc.sctp is already
                    // gone by the time this fires (e.g. late event during teardown), which must not
                    // prevent the C# side from learning about the state change.
                    channelOpened(channelId);
                    onChannel(label, "opened");
                };
                dc.onmessage = message => channelReceived(channelId, new Uint8Array(message.data));
                dc.addEventListener("error", (ev) => {
                    const err = ev.error || ev;
                    const errorMessage = err.message || err.toString() || 'Unknown error';
                    logErrorCallback('dc.onerror', `Data channel '${label}' error: \n${errorMessage}`);
                    const message = buildErrorMessage(err, err.errorDetail, errorMessage);
                    channelError(channelId, message);
                });
                dc.onclose = _ => {
                    channelEnded(channelId);
                    onChannel(label, "closed");
                };

                return channelId;
            };

            this.sendOnChannel = (/** @type {number} */ channelId, /** @type {ArrayBuffer} */ message) => {
                const channel = this.channels[channelId];
                if (!channel || channel.dc.readyState !== "open") return;
                channel.dc.send(message);
            };

            this.closeChannel = (/** @type {number} */ channelId) => {
                const channel = this.channels[channelId];
                if (!channel) return;
                channel.dc.close();
            };

            /** @type {Array<() => void>} */
            this.pendingOfferResolvers = [];

            this.createOffer = async (/** @type {boolean} */ iceRestart) => {
                const offer = await this.pc.createOffer({iceRestart});
                logCallback('createOffer', `Created offer\n${JSON.stringify(offer)}`);
                await this.pc.setLocalDescription(offer);

                /** @type {() => void} */ let resolver = () => {};
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
                const sctp = /** @type {RTCSctpTransport} */ (this.pc.sctp);
                /** @type {RTCIceTransport & { getLocalCandidates?: () => RTCIceCandidate[] }} */
                const iceTransport = /** @type {any} */ (sctp.transport.iceTransport);
                if (iceTransport && typeof iceTransport.getLocalCandidates === 'function') {
                    logCallback('createOffer', `Local candidates\n${JSON.stringify(iceTransport.getLocalCandidates())}`);
                }
                offerCallback(JSON.stringify(updatedOffer));
            };

            this.candidatePairCt = [false];

            this.onAnswer = async (/** @type {string} */ answerJson) => {
                logCallback('onAnswer', `Answer received\n${answerJson}`);
                const answer = JSON.parse(answerJson);
                await this.pc.setRemoteDescription(answer);
                this.candidatePairCt[0] = true;
                this.candidatePairCt = [false];
                await this.waitForCandidatePair(this.candidatePairCt);
            };

            this.waitForCandidatePair = async (/** @type {boolean[]} */ ct) => {
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

            this.setIceServers = (/** @type {RTCIceServer[]} */ iceServers) => {
                this.rtcConfig.iceServers = iceServers;
                logCallback('setIceServers', "Updating rtcConfig: " + JSON.stringify(this.rtcConfig));
                this.pc.setConfiguration(this.rtcConfig);
            };

            this.close = () => {
                this.candidatePairCt[0] = true;
                Object.values(this.channels).forEach(channel => channel.dc.close());
                this.pc.close();
            };

            this.pc.onnegotiationneeded = _ => {
                logCallback('pc.onnegotiationneeded', 'Negotiation needed');
            };

            this.pc.onicecandidate = ({candidate}) => {
                if (candidate !== null) {
                    const candidateJson = JSON.stringify(candidate.toJSON());
                    logCallback('pc.onicecandidate', `Candidate received\n${candidateJson}`);
                    iceCandidateCallback(candidateJson);
                } else {
                    logCallback('pc.onicecandidate', "End of candidates");
                    let resolver;
                    while ((resolver = this.pendingOfferResolvers.pop()) !== undefined) {
                        resolver();
                    }
                    iceCandidateCallback(null);
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

            this.pc.onicegatheringstatechange = ({target}) => {
                const connection = /** @type {RTCPeerConnection} */ (target);
                logCallback('pc.onicegatheringstatechange', `ICE gathering state changed\n${connection.iceGatheringState}`);
                if (connection.iceConnectionState === "failed") {
                    logErrorCallback('pc.oniceconnectionstatechange', `ICE connection failed, restart`);
                }
            };

            this.pc.onsignalingstatechange = _ => {
                logCallback('pc.onsignalingstatechange', `Signaling state changed \n${this.pc.signalingState}`);
            };
        }

        const WebRtcChannelOpened = (/** @type {number} */ channelId) => {
            if (webRtcState.onChannelOpened === null) {
                return;
            }

            try {
                Module.dynCall_vii(webRtcState.onChannelOpened, id, channelId);
            } catch (e) {
                WebRtcLogErrorCallback('WebRtcChannelOpened', `${e}`);
            }
        };

        const WebRtcChannelReceived = (/** @type {number} */ channelId, /** @type {Uint8Array} */ msg) => {
            if (webRtcState.onChannelReceived === null) {
                return;
            }

            const buffer = _malloc(msg.length);
            HEAPU8.set(msg, buffer);

            try {
                Module.dynCall_viiii(
                    webRtcState.onChannelReceived,
                    id,
                    channelId,
                    buffer,
                    msg.length
                );
            } catch (e) {
                WebRtcLogErrorCallback('WebRtcChannelReceived', `${e}`);
            } finally {
                _free(buffer);
            }
        };

        const WebRtcChannelError = (/** @type {number} */ channelId, /** @type {string} */ msg) => {
            const callback = webRtcState.onChannelError;
            if (callback === null) {
                return;
            }

            const msgBytes = lengthBytesUTF8(msg) + 1;
            const msgBuffer = _malloc(msgBytes);
            stringToUTF8(msg, msgBuffer, msgBytes);

            try {
                Module.dynCall_viii(callback, id, channelId, msgBuffer);
            } catch (e) {
                WebRtcLogErrorCallback('WebRtcChannelError', `${e}`);
            } finally {
                _free(msgBuffer);
            }
        };

        const WebRtcChannelEnded = (/** @type {number} */ channelId) => {
            if (webRtcState.onChannelEnded === null) {
                return;
            }

            try {
                Module.dynCall_vii(webRtcState.onChannelEnded, id, channelId);
            } catch (e) {
                WebRtcLogErrorCallback('WebRtcChannelEnded', `${e}`);
            }
        };

        const WebRtcOfferCallback = (/** @type {string} */ msg) => {
            const callback = webRtcState.onOffer;
            if (callback === null) {
                return;
            }

            const msgBytes = lengthBytesUTF8(msg) + 1;
            const msgBuffer = _malloc(msgBytes);
            stringToUTF8(msg, msgBuffer, msgBytes);

            try {
                Module.dynCall_vii(callback, id, msgBuffer);
            } catch (e) {
                WebRtcLogErrorCallback('WebRtcOfferCallback', `${e}`);
            } finally {
                _free(msgBuffer);
            }
        };

        const WebRtcIceCandidateCallback = (/** @type {string | null} */ msg) => {
            const callback = webRtcState.onIceCandidate;
            if (callback === null) {
                WebRtcLogInfoCallback('WebRtcIceCandidateCallback', "onIceCandidate callback is not set");
                return;
            }
            if (!msg) {
                try {
                    Module.dynCall_vii(callback, id, null);
                } catch (e) {
                    WebRtcLogErrorCallback('WebRtcIceCandidateCallback', `${e}`);
                }
                return;
            }

            const msgBytes = lengthBytesUTF8(msg) + 1;
            const msgBuffer = _malloc(msgBytes);
            stringToUTF8(msg, msgBuffer, msgBytes);

            try {
                Module.dynCall_vii(callback, id, msgBuffer);
            } catch (e) {
                WebRtcLogErrorCallback('WebRtcIceCandidateCallback', `${e}`);
            } finally {
                _free(msgBuffer);
            }
        };

        const WebRtcCandidatePairChosenCallback = (/** @type {string} */ localCandidate, /** @type {string} */ remoteCandidate) => {
            const callback = webRtcState.onCandidatePairChosen;
            if (callback === null) {
                return;
            }
            const localCandidateBytes = lengthBytesUTF8(localCandidate) + 1;
            const localCandidateBuffer = _malloc(localCandidateBytes);
            stringToUTF8(localCandidate, localCandidateBuffer, localCandidateBytes);
            const remoteCandidateBytes = lengthBytesUTF8(remoteCandidate) + 1;
            const remoteCandidateBuffer = _malloc(remoteCandidateBytes);
            stringToUTF8(remoteCandidate, remoteCandidateBuffer, remoteCandidateBytes);

            try {
                Module.dynCall_viii(callback, id, localCandidateBuffer, remoteCandidateBuffer);
            } catch (e) {
                WebRtcLogErrorCallback('WebRtcCandidatePairChosenCallback', `${e}`);
            } finally {
                _free(localCandidateBuffer);
                _free(remoteCandidateBuffer);
            }
        };

        const IceConnectionStateChanged = (/** @type {string} */ state) => {
            const callback = webRtcState.onIceConnectionStateChanged;
            if (callback === null) {
                return;
            }
            const msgBytes = lengthBytesUTF8(state) + 1;
            const msgBuffer = _malloc(msgBytes);
            stringToUTF8(state, msgBuffer, msgBytes);
            try {
                Module.dynCall_vii(callback, id, msgBuffer);
            } catch (e) {
                WebRtcLogErrorCallback('IceConnectionStateChanged', `${e}`);
            } finally {
                _free(msgBuffer);
            }
        };

        const ConnectionStateChanged = (/** @type {string} */ state) => {
            const callback = webRtcState.onConnectionStateChanged;
            if (callback === null) {
                return;
            }
            const msgBytes = lengthBytesUTF8(state) + 1;
            const msgBuffer = _malloc(msgBytes);
            stringToUTF8(state, msgBuffer, msgBytes);
            try {
                Module.dynCall_vii(callback, id, msgBuffer);
            } catch (e) {
                WebRtcLogErrorCallback('ConnectionStateChanged', `${e}`);
            } finally {
                _free(msgBuffer);
            }
        };

        const WebRtcLogCallback = (/** @type {FunctionPointer | null} */ callback, /** @type {string} */ methodName, /** @type {string} */ logMessage) => {
            if (callback === null) {
                return;
            }
            const methodNameBytes = lengthBytesUTF8(methodName) + 1;
            const methodNameBuffer = _malloc(methodNameBytes);
            stringToUTF8(methodName, methodNameBuffer, methodNameBytes);
            const logMessageBytes = lengthBytesUTF8(logMessage) + 1;
            const logMessageBuffer = _malloc(logMessageBytes);
            stringToUTF8(logMessage, logMessageBuffer, logMessageBytes);

            try {
                Module.dynCall_viii(callback, id, methodNameBuffer, logMessageBuffer);
            } catch (e) {
                webRtcState.logToConsole(`WebRtcLogCallback failed to log from ${methodName}: ${logMessage}`);
            } finally {
                _free(methodNameBuffer);
                _free(logMessageBuffer);
            }
        };

        const WebRtcLogInfoCallback = (/** @type {string} */ methodName, /** @type {string} */ logMessage) => WebRtcLogCallback(webRtcState.onLog, methodName, logMessage);
        const WebRtcLogWarningCallback = (/** @type {string} */ methodName, /** @type {string} */ logMessage) => WebRtcLogCallback(webRtcState.onLogWarning, methodName, logMessage);
        const WebRtcLogErrorCallback = (/** @type {string} */ methodName, /** @type {string} */ logMessage) => WebRtcLogCallback(webRtcState.onLogError, methodName, logMessage);

        webRtcState.logToConsole("Receiving callbacks created");

        webRtcState.instances[id] = new WebRtcClient(
            WebRtcChannelOpened,
            WebRtcChannelReceived,
            WebRtcChannelError,
            WebRtcChannelEnded,
            IceConnectionStateChanged,
            ConnectionStateChanged,
            WebRtcIceCandidateCallback,
            WebRtcOfferCallback,
            WebRtcCandidatePairChosenCallback,
            WebRtcLogInfoCallback,
            WebRtcLogWarningCallback,
            WebRtcLogErrorCallback
        );

        webRtcState.logToConsole("Client allocated");

        return id;
    },

    WebRtcFree: function (id) {
        const instance = webRtcState.instances[id];
        if (!instance) {
            return;
        }

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

    WebRtcCreateDataChannel: function (id, labelPtr, reliable) {
        const instance = webRtcState.instances[id];
        if (!instance) {
            webRtcState.logToConsole(`Instance not found for ID: ${id}`);
            return -1;
        }

        const label = UTF8ToString(labelPtr);
        return instance.createDataChannel(label, !!reliable);
    },

    WebRtcSendOnChannel: function (id, channelId, bufferPtr, length) {
        const instance = webRtcState.instances[id];
        if (!instance) {
            return;
        }

        instance.sendOnChannel(channelId, /** @type {ArrayBuffer} */ (HEAPU8.buffer.slice(bufferPtr, bufferPtr + length)));
    },

    WebRtcCloseChannel: function (id, channelId) {
        const instance = webRtcState.instances[id];
        if (!instance) {
            return;
        }

        instance.closeChannel(channelId);
    },

    WebRtcSetOnChannelOpened: function (callback) {
        webRtcState.onChannelOpened = callback;
    },

    WebRtcSetOnChannelReceived: function (callback) {
        webRtcState.onChannelReceived = callback;
    },

    WebRtcSetOnChannelError: function (callback) {
        webRtcState.onChannelError = callback;
    },

    WebRtcSetOnChannelEnded: function (callback) {
        webRtcState.onChannelEnded = callback;
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

        instance.createOffer(iceRestart).catch((/** @type {any} */ e) => webRtcState.logToConsole(e.toString()));
    },

    WebRtcOnAnswer: function (id, answer) {
        const instance = webRtcState.instances[id];
        if (!instance) {
            return;
        }

        const answerStr = UTF8ToString(answer);
        instance.onAnswer(answerStr).catch((/** @type {any} */ e) => webRtcState.logToConsole(e.toString()));
    },

    WebRtcClose: function (id) {
        const instance = webRtcState.instances[id];
        if (!instance) {
            return;
        }

        instance.close();
    }
};

autoAddDeps(LibraryWebRtc, "$webRtcState");
mergeInto(LibraryManager.library, LibraryWebRtc);
