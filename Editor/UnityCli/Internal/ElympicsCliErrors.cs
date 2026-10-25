using System;

namespace Elympics.Editor.UnityCli
{
    internal static class ElympicsCliErrors
    {
        public const string Prefix = "ELYMPICS_";
        public const string FixMarker = " Fix: ";

        public const string Internal = "ELYMPICS_INTERNAL";
        public const string InvalidArgument = "ELYMPICS_INVALID_ARGUMENT";
        public const string ConfirmRequired = "ELYMPICS_CONFIRM_REQUIRED";
        public const string CredentialsArgsRemoved = "ELYMPICS_CREDENTIALS_ARGS_REMOVED";

        public const string NoConfig = "ELYMPICS_NO_CONFIG";
        public const string NoActiveGame = "ELYMPICS_NO_ACTIVE_GAME";
        public const string GameNotFound = "ELYMPICS_GAME_NOT_FOUND";

        public const string NotLoggedIn = "ELYMPICS_NOT_LOGGED_IN";
        public const string NetworkTimeout = "ELYMPICS_NETWORK_TIMEOUT";
        public const string RequestFailed = "ELYMPICS_REQUEST_FAILED";
        public const string UntrustedHost = "ELYMPICS_UNTRUSTED_HOST";

        public const string EditorPlaying = "ELYMPICS_EDITOR_PLAYING";
        public const string Compiling = "ELYMPICS_COMPILING";
        public const string CompileErrors = "ELYMPICS_COMPILE_ERRORS";
        public const string NoActiveScene = "ELYMPICS_NO_ACTIVE_SCENE";
        public const string SceneNotOpen = "ELYMPICS_SCENE_NOT_OPEN";
        public const string UnsavedScenes = "ELYMPICS_UNSAVED_SCENES";
        public const string SystemAlreadyPresent = "ELYMPICS_SYSTEM_ALREADY_PRESENT";

        public const string SampleNotFound = "ELYMPICS_SAMPLE_NOT_FOUND";
        public const string SampleAlreadyImported = "ELYMPICS_SAMPLE_ALREADY_IMPORTED";
        public const string SampleImportFailed = "ELYMPICS_SAMPLE_IMPORT_FAILED";

        public const string NoElympicsPackages = "ELYMPICS_NO_ELYMPICS_PACKAGES";
        public const string UpdateInProgress = "ELYMPICS_UPDATE_IN_PROGRESS";
        public const string OperationNotFound = "ELYMPICS_OPERATION_NOT_FOUND";
        public const string PackageManagerFailed = "ELYMPICS_PACKAGE_MANAGER_FAILED";

        public const string UnsupportedMode = "ELYMPICS_UNSUPPORTED_MODE";
        public const string LauncherBusy = "ELYMPICS_LAUNCHER_BUSY";
        public const string LaunchRefused = "ELYMPICS_LAUNCH_REFUSED";
        public const string LaunchTimeout = "ELYMPICS_LAUNCH_TIMEOUT";
        public const string LaunchFailed = "ELYMPICS_LAUNCH_FAILED";
        public const string MppmUnavailable = "ELYMPICS_MPPM_UNAVAILABLE";
        public const string MppmFailed = "ELYMPICS_MPPM_FAILED";

        public const string BuildFailed = "ELYMPICS_BUILD_FAILED";
        public const string MissingBuildModule = "ELYMPICS_MISSING_BUILD_MODULE";
        public const string PackFailed = "ELYMPICS_PACK_FAILED";
        public const string BuildDirNotFound = "ELYMPICS_BUILD_DIR_NOT_FOUND";
        public const string UploadFailed = "ELYMPICS_UPLOAD_FAILED";

        public static string Format(string code, string message, string fix) =>
            string.IsNullOrWhiteSpace(fix) ? $"{code}: {message}" : $"{code}: {message}{FixMarker}{fix}";

        public static ArgumentException InvalidArgumentException(string message, string fix) => new(Format(InvalidArgument, message, fix));
    }
}
