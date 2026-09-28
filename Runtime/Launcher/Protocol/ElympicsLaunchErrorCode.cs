using JetBrains.Annotations;

namespace Elympics
{
    /// <summary>
    /// Reason for a failed launch or abort. Serialized on the wire by name, never by value.
    /// </summary>
    [PublicAPI]
    public enum ElympicsLaunchErrorCode
    {
        /// <summary>Malformed JSON, a missing required field, an unparseable GUID or a match field sent to a mode that does not read it.</summary>
        InvalidPayload,
        /// <summary>The major version of the request differs from <see cref="ElympicsHostProtocol.Version"/>.</summary>
        ProtocolMismatch,
        /// <summary>The game ID is not in <c>ElympicsConfig.availableGames</c>, or that game has no gameplay scene.</summary>
        UnknownGame,
        /// <summary>The match mode is not parseable, is not available over the wire, or is not supported on this platform.</summary>
        UnsupportedMode,
        /// <summary>The gameplay scene could not be loaded or initialized.</summary>
        SceneLoadFailed,
        /// <summary>Connecting to, authenticating with or joining the game server failed.</summary>
        ConnectionFailed,
        /// <summary>The launch did not succeed within its launch timeout.</summary>
        Timeout,
        /// <summary>A match is already loading, running or ended. Abort it first and wait for idle.</summary>
        Busy,
        /// <summary>The launch was cancelled by an abort while it was still loading.</summary>
        Aborted,
        /// <summary>An unexpected exception.</summary>
        InternalError,
    }
}
