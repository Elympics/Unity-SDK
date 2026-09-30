/**
 * JS host API of the Elympics match launcher, installed as `window.elympicsHost` once the build boots.
 * A page drives matches with its methods and receives every launcher event through `onEvent`
 * and as an `elympics` CustomEvent on `window`. Event types and payloads: ElympicsHostProtocol (C#).
 *
 * @typedef {{
 *   launchMatch: (request: object | string) => void,
 *   abortMatch: (requestId?: string) => void,
 *   returnToBoot: (requestId?: string) => void,
 *   quitApplication: (reason?: string) => void,
 *   ping: (requestId?: string) => void,
 *   onEvent: ((type: string, requestId: string, payload: any, payloadJson: string) => void) | null,
 * }} ElympicsHostApi
 */

/**
 * @typedef {{
 *   $elympicsHostState: ElympicsHostState,
 *   ElympicsHost_Initialize: (commandCallback: FunctionPointer) => void,
 *   ElympicsHost_DispatchEvent: (typePtr: number, requestIdPtr: number, payloadPtr: number) => void,
 *   ElympicsHost_TakeLaunchData: (defaultGameIdPtr: number) => number,
 * }} LibraryElympicsHost
 */

/**
 * @typedef {{
 *   toBase64: (value: unknown) => string,
 *   toList: (value: unknown) => unknown[],
 *   toText: (value: unknown) => string,
 *   send: (commandCallback: FunctionPointer, kind: number, first?: string, second?: string, third?: string) => void,
 * }} ElympicsHostState
 */

/* eslint-disable no-unassigned-vars */
// Unity mergeInto aliases $elympicsHostState as elympicsHostState at build time.
// noinspection ES6ConvertVarToLetConst
/** @type {ElympicsHostState} */ var elympicsHostState;
/* eslint-enable no-unassigned-vars */

/** @type {LibraryElympicsHost} */
const elympicsHostLibrary = {
    /** @alias elympicsHostState */
    /** @type {ElympicsHostState} */ $elympicsHostState: {
        toBase64: function (value) {
            if (typeof value === 'string') {
                return value;
            }
            if (value instanceof Uint8Array) {
                let binary = '';
                for (let i = 0; i < value.length; i++) {
                    binary += String.fromCharCode(value[i]);
                }
                return btoa(binary);
            }
            return '';
        },
        // Accepts an array, a JSON array string or a comma-separated string.
        toList: function (value) {
            if (Array.isArray(value)) {
                return value;
            }
            if (typeof value !== 'string' || value.length === 0) {
                return [];
            }
            if (value.charAt(0) === '[') {
                const parsed = JSON.parse(value);
                return Array.isArray(parsed) ? parsed : [];
            }
            return value.split(',').map(function (item) { return item.trim(); });
        },
        toText: function (value) {
            return typeof value === 'string' ? value : '';
        },
        // Calls the C# command callback synchronously; C# copies the strings before it returns.
        send: function (commandCallback, kind, first, second, third) {
            const buffers = [first, second, third].map(function (text) { return elympicsUtils.stringToBuffer(text || ''); });
            try {
                Module.dynCall_viiii(commandCallback, kind, buffers[0], buffers[1], buffers[2]);
            } finally {
                buffers.forEach(function (buffer) { _free(buffer); });
            }
        },
    },

    // Command kinds: ElympicsHostCommandKind (C#).
    ElympicsHost_Initialize: function (commandCallback) {
        const elympicsWindow = /** @type {ElympicsWindow} */ (window);
        const existing = elympicsWindow.elympicsHost;
        /** @param {number} kind @param {string} argument */
        const send = function (kind, argument) {
            elympicsHostState.send(commandCallback, kind, argument);
        };
        elympicsWindow.elympicsHost = {
            launchMatch: function (request) {
                send(1, typeof request === 'string' ? request : JSON.stringify(request));
            },
            abortMatch: function (requestId) { send(2, requestId || ''); },
            returnToBoot: function (requestId) { send(3, requestId || ''); },
            quitApplication: function (reason) { send(4, reason || ''); },
            ping: function (requestId) { send(5, requestId || ''); },
            // Keeps a handler the page registered before the build booted.
            onEvent: existing && typeof existing.onEvent === 'function' ? existing.onEvent : null,
        };
    },

    ElympicsHost_DispatchEvent: function (typePtr, requestIdPtr, payloadPtr) {
        const type = UTF8ToString(typePtr);
        const requestId = UTF8ToString(requestIdPtr);
        const payloadJson = UTF8ToString(payloadPtr);
        const payload = payloadJson ? JSON.parse(payloadJson) : null;
        const host = /** @type {ElympicsWindow} */ (window).elympicsHost;
        if (host && typeof host.onEvent === 'function') {
            try {
                host.onEvent(type, requestId, payload, payloadJson);
            } catch (error) {
                console.error(`elympicsHost.onEvent threw for event ${type}:`, error);
            }
        }
        window.dispatchEvent(new CustomEvent('elympics', {
            detail: { type: type, requestId: requestId, payload: payload, payloadJson: payloadJson },
        }));
    },

    // Converts window.elympicsLaunchData (set by the page before the build loads) into a launch request, once.
    ElympicsHost_TakeLaunchData: function (defaultGameIdPtr) {
        const elympicsWindow = /** @type {ElympicsWindow} */ (window);
        const data = elympicsWindow.elympicsLaunchData;
        if (typeof data === 'undefined' || data === null) {
            return elympicsUtils.stringToBuffer('');
        }
        delete elympicsWindow.elympicsLaunchData;
        try {
            /** @type {Record<string, unknown>} */
            const source = typeof data === 'string' ? JSON.parse(data) : data;
            const request = {
                requestId: elympicsHostState.toText(source.requestId) || 'elympicsLaunchData',
                gameId: elympicsHostState.toText(source.gameId) || UTF8ToString(defaultGameIdPtr),
                matchMode: elympicsHostState.toText(source.matchMode) || 'Online',
                userId: elympicsHostState.toText(source.userId),
                matchId: elympicsHostState.toText(source.matchId),
                userSecret: elympicsHostState.toText(source.userSecret),
                queueName: elympicsHostState.toText(source.queueName),
                regionName: elympicsHostState.toText(source.regionName),
                tcpUdpServerAddress: elympicsHostState.toText(source.tcpUdpServerAddress),
                webServerAddress: elympicsHostState.toText(source.webServerAddress),
                gameEngineData: elympicsHostState.toBase64(source.gameEngineData),
                matchmakerData: elympicsHostState.toList(source.matchmakerData).map(Number),
                matchedPlayers: elympicsHostState.toList(source.matchedPlayers).map(String),
            };
            return elympicsUtils.stringToBuffer(JSON.stringify(request));
        } catch (error) {
            console.error('window.elympicsLaunchData could not be read, no match will be launched from it:', error);
            return elympicsUtils.stringToBuffer('');
        }
    },
};

autoAddDeps(elympicsHostLibrary, '$elympicsHostState');
autoAddDeps(elympicsHostLibrary, '$elympicsUtils');
mergeInto(LibraryManager.library, elympicsHostLibrary);
