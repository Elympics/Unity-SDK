// noinspection ES6ConvertVarToLetConst

/**
 * Emscripten / Unity WebGL runtime globals — declaration-only file for IDE analysis.
 * Not compiled or loaded at runtime; exists solely so Rider can resolve
 * these symbols in *.jslib files without reporting "unresolved variable" errors.
 */

/* eslint-disable @typescript-eslint/no-unused-vars */

/**
 * @typedef {{
 *   dynCall_vi: (
 *     func: (arg1: number | null) => void,
 *     arg1: number | null,
 *   ) => void,
 *   dynCall_vii: (
 *     func: (arg1: number | null, arg2: number | null) => void,
 *     arg1: number | null,
 *     arg2: number | null,
 *   ) => void,
 *   dynCall_viii: (
 *     func: (arg1: number | null, arg2: number | null, arg3: number | null) => void,
 *     arg1: number | null,
 *     arg2: number | null,
 *     arg3: number | null,
 *   ) => void,
 *   dynCall_viiii: (
 *     func: (arg1: number | null, arg2: number | null, arg3: number | null, arg4: number | null) => void,
 *     arg1: number | null,
 *     arg2: number | null,
 *     arg3: number | null,
 *     arg4: number | null,
 *   ) => void,
 *   preRun: (() => void)[],
 * }} EmscriptenModule
 */

/** @type {EmscriptenModule} */
var Module;

/** @type {Uint8Array} View for 8-bit unsigned memory. */
var HEAPU8;

/**
 * Allocates a buffer.
 * @param {number} size Buffer size in bytes.
 * @returns {number} Pointer to the allocated buffer.
 */
function _malloc(size) {}

/**
 * Frees an allocated buffer.
 * @param {number} ptr Pointer to the allocated buffer.
 */
function _free(ptr) {}

/**
 * Given a pointer `ptr` to a null-terminated UTF8-encoded string in the Emscripten HEAP, returns a copy of that string as a JavaScript `String` object.
 * @param {number} ptr A pointer to a null-terminated UTF8-encoded string in the Emscripten HEAP.
 * @returns {string} A JavaScript `String` object
 */
function UTF8ToString(ptr) {}

/**
 * Copies the given JavaScript `String` object `str` to the Emscripten HEAP at address `outPtr`, null-terminated and encoded in UTF8 form.
 *
 * The copy will require at most `str.length*4+1` bytes of space in the HEAP. You can use the function `lengthBytesUTF8()` to compute the exact amount of bytes (excluding the null terminator) needed to encode the string.
 * @param {string} str A JavaScript `String` object.
 * @param {number} outPtr Pointer to data copied from `str`, encoded in UTF8 format and null-terminated.
 * @param {number} maxBytesToWrite A limit on the number of bytes that this function can at most write out. If the string is longer than this, the output is truncated. The outputted string will always be null terminated, even if truncation occurred, as long as `maxBytesToWrite > 0`.
 */
function stringToUTF8(str, outPtr, maxBytesToWrite) {}

/**
 * Computes the exact amount of bytes (excluding the null terminator) needed to encode the string `str`.
 * @param {string} str The string to encode.
 * @returns {number} The number of bytes needed to encode the string.
 */
function lengthBytesUTF8(str) {}

/**
 * Converts a function pointer to a callable JS function.
 * @param {number} functionPtr Function pointer.
 * @returns {function} Callable JS function.
 */
function getWasmTableEntry(functionPtr) {}

/**
 * Registers the dependency identified by `dependencyName` within the function library.
 * @param {Object} library
 * @param {string} dependencyName
 */
function autoAddDeps(library, dependencyName) {}

/**
 * Merges the source function library into the target function library.
 * @param {Object} target Target function library.
 * @param {Object} source Source function library.
 */
function mergeInto(target, source) {}

/** @type {{ library: Object }} */
var LibraryManager;
