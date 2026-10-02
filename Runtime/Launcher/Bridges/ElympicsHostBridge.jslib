/**
 * JS host API of the Elympics match launcher, installed as `window.elympicsHost` once the build boots
 * (wait for its `ready` event). A page drives matches with its methods and receives every launcher event
 * through `onEvent` and as an `elympics` CustomEvent on `window`. Event types and payloads: ElympicsHostProtocol (C#).
 *
 * Every launch method takes the game ID, the mode's launch data and the launch options. Data and options are
 * objects or JSON strings; omitted or null means the defaults. A `Uint8Array` (e.g. `gameEngineData`) is sent as base64.
 *
 * @typedef {object | string | null | undefined} ElympicsLaunchArgument
 * @typedef {(gameId: string, data?: ElympicsLaunchArgument, options?: ElympicsLaunchArgument) => void} ElympicsLaunchMethod
 * @typedef {{
 *   launchOnline: ElympicsLaunchMethod,
 *   launchSinglePlayer: ElympicsLaunchMethod,
 *   launchHalfRemoteClient: ElympicsLaunchMethod,
 *   launchHalfRemoteServer: ElympicsLaunchMethod,
 *   launchSnapshotReplay: ElympicsLaunchMethod,
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
 * }} LibraryElympicsHost
 */

/**
 * @typedef {{
 *   toJson: (value: ElympicsLaunchArgument) => string,
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
        toJson: function (value) {
            if (typeof value === 'undefined' || value === null) {
                return '';
            }
            if (typeof value === 'string') {
                return value;
            }
            return JSON.stringify(value, function (_key, field) {
                if (field instanceof Uint8Array) {
                    let binary = '';
                    for (let i = 0; i < field.length; i++) {
                        binary += String.fromCharCode(field[i]);
                    }
                    return btoa(binary);
                }
                return field;
            });
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
        /** @param {number} kind @param {string} [argument] */
        const send = function (kind, argument) {
            elympicsHostState.send(commandCallback, kind, argument || '');
        };
        /** @param {number} kind @returns {ElympicsLaunchMethod} */
        const launch = function (kind) {
            return function (gameId, data, options) {
                elympicsHostState.send(commandCallback, kind, gameId || '', elympicsHostState.toJson(data), elympicsHostState.toJson(options));
            };
        };
        elympicsWindow.elympicsHost = {
            launchOnline: launch(6),
            launchSinglePlayer: launch(7),
            launchHalfRemoteClient: launch(8),
            launchHalfRemoteServer: launch(9),
            launchSnapshotReplay: launch(10),
            abortMatch: function (requestId) { send(2, requestId); },
            returnToBoot: function (requestId) { send(3, requestId); },
            quitApplication: function (reason) { send(4, reason); },
            ping: function (requestId) { send(5, requestId); },
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
};

autoAddDeps(elympicsHostLibrary, '$elympicsHostState');
autoAddDeps(elympicsHostLibrary, '$elympicsUtils');
mergeInto(LibraryManager.library, elympicsHostLibrary);
