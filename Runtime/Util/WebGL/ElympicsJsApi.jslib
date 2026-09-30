/**
 * @typedef {{
 *   $elympicsUtils: ElympicsUtils,
 *   ElympicsJsApi_GetElympicsHref: () => number,
 *   ElympicsJsApi_GetHref: () => number,
 * }} ElympicsJsApi
 */

/**
 * @typedef {{
 *   stringToBuffer: (text: string) => number,
 * }} ElympicsUtils
 */

/**
 * @typedef {{
 *   href: string,
 * }} UnityGameOptions
 */

/**
 * @typedef {{
 *   matchId: string,
 *   userSecret: string,
 *   queueName?: string,
 *   regionName?: string,
 *   gameEngineData?: string | Uint8Array,
 *   matchmakerData?: string | number[],
 *   webServerAddress: string,
 *   matchedPlayers: string | string[],
 *   gameId?: string,
 *   matchMode?: string,
 *   userId?: string,
 *   requestId?: string,
 *   tcpUdpServerAddress?: string,
 * }} ElympicsLaunchData
 */

/**
 * Globals a page may set before the build loads, or that the build installs.
 * Accessed through a cast, as library files can only declare types, not globals.
 * @typedef {Window & {
 *   unityGameOptions?: UnityGameOptions,
 *   elympicsLaunchData?: ElympicsLaunchData | string | null,
 *   elympicsHost?: ElympicsHostApi,
 * }} ElympicsWindow
 */

/* eslint-disable no-unassigned-vars */
// noinspection ES6ConvertVarToLetConst
/** @type {ElympicsUtils} */ var elympicsUtils;
/* eslint-enable no-unassigned-vars */

/** @type {ElympicsJsApi} */
const elympicsJsApi = {
    /** @alias elympicsUtils */
    /** @type {ElympicsUtils} */ $elympicsUtils: {
        stringToBuffer: (text) => {
            const bufferSize = lengthBytesUTF8(text) + 1;
            const buffer = _malloc(bufferSize);
            stringToUTF8(text, buffer, bufferSize);
            return buffer;
        }
    },

    ElympicsJsApi_GetHref: function () {
        try {
            const href = (window.top || window).location.href;
            return elympicsUtils.stringToBuffer(href);
        } catch (error) {
            console.error(`ElympicsGetHref failed, empty string will be returned. Error:\n${error}`);
            return elympicsUtils.stringToBuffer('');
        }
    },

    ElympicsJsApi_GetElympicsHref: function () {
        try {
            const unityGameOptions = /** @type {ElympicsWindow} */ (window).unityGameOptions;
            if (typeof unityGameOptions === 'undefined') {
                return elympicsUtils.stringToBuffer('');
            }
            const href = unityGameOptions.href;
            if (typeof href !== 'string' || !href) {
                return elympicsUtils.stringToBuffer('');
            }
            return elympicsUtils.stringToBuffer(href);
        } catch (error) {
            console.error(`ElympicsGetHref failed, empty string will be returned. Error:\n${error}`);
            return elympicsUtils.stringToBuffer('');
        }
    },
};

autoAddDeps(elympicsJsApi, "$elympicsUtils");
mergeInto(LibraryManager.library, elympicsJsApi);
