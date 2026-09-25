/**
 * @typedef {{
 *   ElympicsWebGL_GetElympicsHref: () => number,
 *   ElympicsWebGL_GetHref: () => number,
 * }} ElympicsWebGL
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
 * }} ElympicsLaunchData
 */

/* eslint-disable no-unassigned-vars */
// noinspection ES6ConvertVarToLetConst
/** @type {ElympicsUtils} */ var elympicsUtils;
/* eslint-enable no-unassigned-vars */

// noinspection SillyAssignmentJS
/** @type {UnityGameOptions | undefined}
 * @deprecated "Use typed ElympicsLaunchData"
 */ window.unityGameOptions = window.unityGameOptions;

// noinspection SillyAssignmentJS
/** @type {ElympicsLaunchData | string | undefined} */ window.elympicsLaunchData = window.elympicsLaunchData;

/** @type {ElympicsWebGL} */
const elympicsWebGL = {
    /** @alias elympicsUtils */
    /** @type {ElympicsUtils} */ $elympicsUtils: {
        stringToBuffer: (text) => {
            const bufferSize = lengthBytesUTF8(text) + 1;
            const buffer = _malloc(bufferSize);
            stringToUTF8(text, buffer, bufferSize);
            return buffer;
        }
    },

    ElympicsWebGL_GetHref: function () {
        try {
            const href = window.top.location.href;
            return elympicsUtils.stringToBuffer(href);
        } catch (error) {
            console.error(`ElympicsGetHref failed, empty string will be returned. Error:\n${error}`);
            return elympicsUtils.stringToBuffer('');
        }
    },

    ElympicsWebGL_GetElympicsHref: function () {
        try {
            const unityGameOptions = window.unityGameOptions;
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

autoAddDeps(elympicsWebGL, "$elympicsUtils");
mergeInto(LibraryManager.library, elympicsWebGL);
