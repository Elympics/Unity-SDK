using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Elympics.Core.Logger;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics.Editor
{
    public static class WebGLUploader
    {
        private const string ContentEncodingKey = "Content-Encoding";
        private const string ContentTypeKey = "Content-Type";
        private const string NamePattern = "^[0-9-a-zA-Z.]+$";
        private const string FixedPrefix = "Build";

        private const int MaxStreamingAssetsVersionLength = 63;
        private const string AddressablesDirectoryName = "aa";
        private const string AddressablesSettingsFileName = "settings.json";
        private const string CatalogFileNamePrefix = "catalog";
        private const string DefaultContentType = "application/octet-stream";

        /// <summary>
        /// Generated in memory and uploaded at the root of an <see cref="StreamingAssetsLayout.AddressableVariants" />
        /// upload, so a consumer can discover which variants a content version contains without listing the bucket.
        /// </summary>
        internal const string VariantsManifestFileName = "variants.meta.json";

        internal static readonly string[] CompoundExtensions =
        {
            ".framework.js",
            ".wasm",
            ".elympicsmeta.json",
            ".loader.js",
            ".data",
        };

        private static readonly string[] ContentTypes =
        {
            "application/javascript",
            "application/wasm",
            "application/json",
            "application/javascript",
            "application/octet-stream",
        };

        // The signed URL covers Content-Type, so a value the backend did not sign fails the PUT with GCS
        // "SignatureDoesNotMatch". Only extensions verified against a real upload belong here; the rest must fall
        // through to DefaultContentType, which is what the backend uses for them.
        private static readonly Dictionary<string, string> StreamingAssetsContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".json"] = "application/json",
            [".xml"] = "application/xml",
        };

        #region Models

        [Serializable]
        internal class UploadInitRequest
        {
            public string gameId;
            public string clientGameVersion;
            public string serverGameVersion;
            public string streamingAssetsUrl;
            public string[] files;
        }

        [Serializable]
        public class UploadInitResponse
        {
            public string UploadId;
            public DateTime ExpiresAt;
            public FileUploadInfo[] Files;

            [Serializable]
            public struct FileUploadInfo
            {
                public string FilePath;
                public string SignedUrl;
                public string GcsPath;
            }
        }

        [Serializable]
        internal class UploadCompleteRequest
        {
            public string uploadId;
            public bool success;
        }

        [Serializable]
        internal class StreamingAssetsUploadInitRequest
        {
            public string gameId;
            public string version;
            public string[] files;
        }

        [Serializable]
        internal class VariantsManifest
        {
            public string[] variants;
        }

        [Serializable]
        internal class StreamingAssetsUploadInitResponse
        {
            // No UploadId: this endpoint has no matching complete call.
            public DateTime ExpiresAt;
            public UploadInitResponse.FileUploadInfo[] Files;
        }

        #endregion

        #region File Validation Helpers

        private static bool TryGetFullExtension(ReadOnlySpan<char> fileName, string[] knownCompoundExtensions, out string compoundExtension)
        {
            compoundExtension = string.Empty;
            foreach (var ext in knownCompoundExtensions)
            {
                var extIndex = fileName.IndexOf(ext.AsSpan(), StringComparison.OrdinalIgnoreCase);
                if (extIndex < 0)
                    continue;

                compoundExtension = fileName[extIndex..].ToString();
                return true;
            }

            return false;
        }

        private static bool DoesFileHaveGivenCompoundExtension(string fileName, string compoundExtension)
        {
            if (TryGetFullExtension(fileName.AsSpan(), CompoundExtensions, out var fileCompoundExtension))
                return fileCompoundExtension == compoundExtension;
            return false;
        }

        private static string FetchEncoding(string compoundExtension)
        {
            var fileName = compoundExtension.AsSpan();
            if (fileName.EndsWith(".br"))
                return "br";
            if (fileName.EndsWith(".gz"))
                return "gzip";
            return string.Empty;
        }

        private static string FetchContentType(string fileExtension)
        {
            for (var i = 0; i < CompoundExtensions.Length; i++)
            {
                var ext = CompoundExtensions[i];
                if (fileExtension.Contains(ext))
                    return ContentTypes[i];
            }

            throw new ElympicsException("Unknown content type: " + fileExtension);
        }

        internal static List<(string name, string extension)> GetValidFiles(string[] fileNames, string[] knownCompoundExtensions)
        {
            return fileNames.Select(fileName =>
            {
                if (TryGetFullExtension(fileName.AsSpan(), knownCompoundExtensions, out var compoundExtension))
                {
                    var splitExtension = compoundExtension.Split('.');
                    var split = fileName.Split('.');
                    var name = string.Join(".", split.Take(split.Length - splitExtension.Length + 1));
                    return (name, compoundExtension);
                }

                return (string.Empty, string.Empty);
            }).Where(file => !string.IsNullOrEmpty(file.Item1)).ToList();
        }

        #endregion


        internal static string PrepareValidFiles(
            string clientBuildPath,
            string clientGameVersion,
            out List<(string name, string extension)> validFiles)
        {
            validFiles = null;

            if (!Directory.Exists(clientBuildPath))
                return $"Client build directory '{clientBuildPath}' does not exist.";

            var isNameValid = Regex.IsMatch(clientGameVersion, NamePattern);
            if (!isNameValid)
                return $"Client game version '{clientGameVersion}' contains invalid characters. Only alphanumeric characters, \"-\" and \".\" are allowed.";

            var filePaths = Directory.GetFiles(clientBuildPath);
            var fileNames = filePaths.Select(Path.GetFileName).ToArray();
            validFiles = GetValidFiles(fileNames, CompoundExtensions);

            if (validFiles.Count != CompoundExtensions.Length)
                return
                    $"Not all required files will be uploaded to bucket{Environment.NewLine}Files in directory: {string.Join('|', fileNames)}{Environment.NewLine}Validated files: {string.Join('|', validFiles)}";

            if (!ElympicsConfig.IsLogin)
                return "You must be logged in Elympics to upload a client build.";

            return null;
        }

        internal static UploadInitRequest CreateInitRequest(
            string gameId,
            string clientGameVersion,
            string serverGameVersion,
            string streamingAssetsUrl,
            List<(string name, string extension)> validFiles)
        {
            return new UploadInitRequest
            {
                gameId = gameId,
                clientGameVersion = clientGameVersion,
                serverGameVersion = serverGameVersion,
                streamingAssetsUrl = streamingAssetsUrl,
                files = validFiles.Select(fileNameAndExtension => FixedPrefix + fileNameAndExtension.extension).ToArray(),
            };
        }

        internal static UnityWebRequestAsyncOperation SendInitRequest(string apiEndpoint, UploadInitRequest request, Action<UnityWebRequest> completed = null)
        {
            var uri = $"{apiEndpoint}/client-builds/init";
            return ElympicsEditorWebClient.SendJsonPostRequestApi(uri, request, completed);
        }

        internal static string UploadFilesToGcs(
            string clientBuildPath,
            UploadInitResponse initResponse,
            List<(string name, string extension)> validFiles,
            Action<string, float> progressCallback = null)
        {
            for (var index = 0; index < initResponse.Files.Length; index++)
            {
                var fileUploadInfo = initResponse.Files[index];
                var responseFile = Path.Combine(clientBuildPath, fileUploadInfo.FilePath);
                var expectedFile = validFiles[index];

                if (!DoesFileHaveGivenCompoundExtension(responseFile, expectedFile.extension))
                    return $"Uploaded file '{fileUploadInfo.FilePath}' does not match expected extension '{expectedFile.extension}'.";

                var localFile = expectedFile.name + expectedFile.extension;
                var progress = (float)(index + 1) / initResponse.Files.Length;
                progressCallback?.Invoke(localFile, progress);

                var filePath = Path.Combine(clientBuildPath, localFile);
                using var request = UnityWebRequest.Put(fileUploadInfo.SignedUrl, File.ReadAllBytes(filePath));
                var requestContentType = FetchContentType(expectedFile.extension);
                request.SetRequestHeader(ContentTypeKey, requestContentType);
                var encoding = FetchEncoding(expectedFile.extension);
                if (!string.IsNullOrEmpty(encoding))
                    request.SetRequestHeader(ContentEncodingKey, encoding);

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                { }

                if (operation.webRequest.IsConnectionError() || operation.webRequest.IsProtocolError())
                    return $"Failed to upload file '{localFile}': {operation.webRequest.error}{Environment.NewLine}{operation.webRequest.downloadHandler.text}";
            }

            return null;
        }

        internal static UnityWebRequestAsyncOperation SendCompleteRequest(string apiEndpoint, string uploadId, bool success)
        {
            var uri = $"{apiEndpoint}/client-builds/complete";
            return ElympicsEditorWebClient.SendJsonPostRequestApi(uri,
                new UploadCompleteRequest
                {
                    uploadId = uploadId,
                    success = success,
                });
        }

        #region StreamingAssets content upload

        /// <param name="relativePaths">Paths relative to <paramref name="rootPath" />, always separated with "/".</param>
        /// <param name="generatedFiles">
        /// Content for the subset of <paramref name="relativePaths" /> that is generated in memory rather than read
        /// from disk, keyed by relative path.
        /// </param>
        /// <returns>An error message, or null when <paramref name="relativePaths" /> can be uploaded.</returns>
        internal static string PrepareStreamingAssetsFiles(
            string rootPath,
            string version,
            StreamingAssetsLayout layout,
            out List<string> relativePaths,
            out Dictionary<string, byte[]> generatedFiles)
        {
            relativePaths = null;
            generatedFiles = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

            if (!ElympicsConfig.IsLogin)
                return "You must be logged in Elympics to upload StreamingAssets content.";

            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                return $"Content directory '{rootPath}' does not exist.";

            if (string.IsNullOrEmpty(version))
                return "Content version cannot be empty.";

            if (!Regex.IsMatch(version, NamePattern))
                return $"Content version '{version}' contains invalid characters. Only alphanumeric characters, \"-\" and \".\" are allowed.";

            if (version.Length > MaxStreamingAssetsVersionLength)
                return $"Content version '{version}' is too long - {version.Length} characters, maximum is {MaxStreamingAssetsVersionLength}.";

            var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            List<string> files;
            List<string> variantNames = null;
            switch (layout)
            {
                case StreamingAssetsLayout.AddressableVariants:
                    var layoutError = CollectAddressableVariants(root, out files, out variantNames);
                    if (layoutError != null)
                        return layoutError;
                    break;
                case StreamingAssetsLayout.UnstructuredAssets:
                    files = CollectFilesRecursively(root, root);
                    break;
                default:
                    return $"Unknown content layout '{layout}'.";
            }

            if (files.Count == 0)
                return $"No files found in '{rootPath}'.";

            files.Sort(StringComparer.OrdinalIgnoreCase);

            if (variantNames != null)
            {
                generatedFiles[VariantsManifestFileName] = CreateVariantsManifest(variantNames);
                files.Add(VariantsManifestFileName);
            }

            relativePaths = files;
            return null;
        }

        private static byte[] CreateVariantsManifest(List<string> variantNames)
        {
            var manifest = new VariantsManifest { variants = variantNames.OrderBy(name => name, StringComparer.Ordinal).ToArray() };
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(manifest));
        }

        private static string CollectAddressableVariants(string root, out List<string> relativePaths, out List<string> variantNames)
        {
            relativePaths = null;
            variantNames = null;

            var errors = new List<string>();

            var looseFiles = Directory.GetFiles(root).Select(Path.GetFileName).ToList();
            if (looseFiles.Count > 0)
                ElympicsLogger.LogWarning($"Uploading {looseFiles.Count} file(s) found directly in '{root}', outside any variant: {string.Join(", ", looseFiles)}.");

            var variantDirectories = Directory.GetDirectories(root);
            if (variantDirectories.Length == 0)
                return $"Content directory '{root}' contains no variant directories. The {nameof(StreamingAssetsLayout.AddressableVariants)} layout expects one directory per variant, "
                    + $"each containing its own \"{AddressablesDirectoryName}\" directory. Use {nameof(StreamingAssetsLayout.UnstructuredAssets)} to upload a path as-is.";

            var misplacedDirectories = Directory.GetDirectories(root, AddressablesDirectoryName, SearchOption.AllDirectories)
                .Select(directory => ToRelativePath(root, directory))
                .Where(directory => directory.Count(character => character == '/') != 1)
                .ToList();
            if (misplacedDirectories.Count > 0)
                errors.Add($"Unexpected \"{AddressablesDirectoryName}\" directories at the wrong depth: {string.Join(", ", misplacedDirectories)}. "
                    + $"The {nameof(StreamingAssetsLayout.AddressableVariants)} layout requires exactly one per variant, at \"{{variant}}/{AddressablesDirectoryName}\".");

            var files = new List<string>(looseFiles);
            var names = new List<string>();
            var variantsByCatalogFormat = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var variantDirectory in variantDirectories)
            {
                var variantName = Path.GetFileName(variantDirectory);
                names.Add(variantName);
                files.AddRange(CollectFilesRecursively(root, variantDirectory));

                var addressablesDirectory = Path.Combine(variantDirectory, AddressablesDirectoryName);
                if (!Directory.Exists(addressablesDirectory))
                {
                    errors.Add($"Variant directory '{variantName}' does not contain an \"{AddressablesDirectoryName}\" directory, so it is not an Addressables content build. "
                        + $"The {nameof(StreamingAssetsLayout.AddressableVariants)} layout requires '{variantName}/{AddressablesDirectoryName}/...'.");
                    continue;
                }

                var addressablesFileNames = Directory.GetFiles(addressablesDirectory).Select(Path.GetFileName).ToList();
                var catalogFormats = new[] { ".json", ".bin" }.Where(format => addressablesFileNames.Contains(CatalogFileNamePrefix + format, StringComparer.OrdinalIgnoreCase)).ToList();

                if (catalogFormats.Count == 0)
                    errors.Add($"Variant '{variantName}' has no catalog - '{variantName}/{AddressablesDirectoryName}' contains neither \"{CatalogFileNamePrefix}.json\" nor \"{CatalogFileNamePrefix}.bin\".");
                if (!addressablesFileNames.Contains(AddressablesSettingsFileName, StringComparer.OrdinalIgnoreCase))
                    errors.Add($"Variant '{variantName}' is missing '{variantName}/{AddressablesDirectoryName}/{AddressablesSettingsFileName}'.");

                foreach (var catalogFormat in catalogFormats)
                {
                    if (!variantsByCatalogFormat.TryGetValue(catalogFormat, out var formatVariants))
                        variantsByCatalogFormat[catalogFormat] = formatVariants = new List<string>();
                    formatVariants.Add(variantName);
                }
            }

            if (variantsByCatalogFormat.Count > 1)
                ElympicsLogger.LogWarning($"Variants use mixed catalog formats: {string.Join("; ", variantsByCatalogFormat.Select(entry => $"{CatalogFileNamePrefix}{entry.Key} - {string.Join(", ", entry.Value)}"))}. "
                    + "This is usually an ENABLE_JSON_CATALOG mismatch between content builds; the client can only load the format it was built for.");

            if (errors.Count > 0)
                return string.Join(Environment.NewLine, errors);

            relativePaths = files;
            variantNames = names;
            return null;
        }

        private static List<string> CollectFilesRecursively(string root, string directory) =>
            Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Select(filePath => ToRelativePath(root, filePath)).ToList();

        private static string ToRelativePath(string root, string fullPath) =>
            fullPath[root.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');

        internal static StreamingAssetsUploadInitRequest CreateStreamingAssetsInitRequest(string gameId, string version, List<string> relativePaths)
        {
            return new StreamingAssetsUploadInitRequest
            {
                gameId = gameId,
                version = version,
                files = relativePaths.ToArray(),
            };
        }

        internal static UnityWebRequestAsyncOperation SendStreamingAssetsInitRequest(string apiEndpoint, StreamingAssetsUploadInitRequest request, Action<UnityWebRequest> completed = null)
        {
            var uri = $"{apiEndpoint}/client-builds/streaming-assets";
            return ElympicsEditorWebClient.SendJsonPostRequestApi(uri, request, completed);
        }

        /// <summary>
        /// Sorts files by importance (bundle first, then per-variant metadata, then global metadata) and by name.
        /// </summary>
        internal static List<UploadInitResponse.FileUploadInfo> SortStreamingAssetsFilesForUpload(IEnumerable<UploadInitResponse.FileUploadInfo> files) =>
            files.OrderBy(file => UploadTier(file.FilePath)).ThenBy(file => file.FilePath, StringComparer.OrdinalIgnoreCase).ToList();

        private static int UploadTier(string filePath)
        {
            var fileName = Path.GetFileName(filePath ?? string.Empty);
            if (fileName.Equals(VariantsManifestFileName, StringComparison.OrdinalIgnoreCase))
                return 2;
            return fileName.StartsWith(CatalogFileNamePrefix, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }

        private static string FetchStreamingAssetsContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath ?? string.Empty);
            return StreamingAssetsContentTypes.GetValueOrDefault(extension, DefaultContentType);
        }

        internal static string UploadStreamingAssetsToGcs(
            string rootPath,
            StreamingAssetsUploadInitResponse initResponse,
            Dictionary<string, byte[]> generatedFiles,
            Action<string, float> progressCallback = null)
        {
            if (initResponse.Files == null || initResponse.Files.Length == 0)
                return "Elympics cloud returned no files to upload.";

            var files = SortStreamingAssetsFilesForUpload(initResponse.Files);
            var uploadedFiles = new List<string>();

            for (var index = 0; index < files.Count; index++)
            {
                var fileUploadInfo = files[index];
                byte[] payload;
                if (generatedFiles.TryGetValue(fileUploadInfo.FilePath, out var generatedPayload))
                    payload = generatedPayload;
                else
                {
                    var filePath = Path.Combine(rootPath, fileUploadInfo.FilePath);
                    if (!File.Exists(filePath))
                        return $"File '{fileUploadInfo.FilePath}' requested by Elympics cloud was not found at '{filePath}'.{DescribeUploadedFiles(uploadedFiles)}";
                    payload = File.ReadAllBytes(filePath);
                }

                progressCallback?.Invoke(fileUploadInfo.FilePath, (float)index / files.Count);

                using var request = UnityWebRequest.Put(fileUploadInfo.SignedUrl, payload);
                request.SetRequestHeader(ContentTypeKey, FetchStreamingAssetsContentType(fileUploadInfo.FilePath));
                var encoding = FetchEncoding(fileUploadInfo.FilePath);
                if (!string.IsNullOrEmpty(encoding))
                    request.SetRequestHeader(ContentEncodingKey, encoding);

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                { }

                if (operation.webRequest.IsConnectionError() || operation.webRequest.IsProtocolError())
                    return $"Failed to upload file '{fileUploadInfo.FilePath}': {operation.webRequest.error}{Environment.NewLine}{operation.webRequest.downloadHandler.text}{DescribeUploadedFiles(uploadedFiles)}";

                uploadedFiles.Add(fileUploadInfo.FilePath);
            }

            return null;
        }

        private static string DescribeUploadedFiles(List<string> uploadedFiles) =>
            uploadedFiles.Count == 0
                ? $"{Environment.NewLine}No files had been uploaded before the failure."
                : $"{Environment.NewLine}Files already uploaded before the failure ({uploadedFiles.Count}):{Environment.NewLine}"
                + $"{string.Join(Environment.NewLine, uploadedFiles)}{Environment.NewLine}"
                + "This version is now partially uploaded and cannot be re-uploaded - Elympics cloud rejects a version that already exists. "
                + "Fix the cause and upload the content again under a new version.";

        #endregion

    }
}
