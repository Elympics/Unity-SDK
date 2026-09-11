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
        private const string ClientBuildsRoute = "client-builds";

        private const int MaxStreamingAssetsVersionLength = 63;
        private const string AddressablesDirectoryName = "aa";
        private const string AddressablesSettingsFileName = "settings.json";
        private const string CatalogFileNamePrefix = "catalog";
        private const string DefaultContentType = "application/octet-stream";

        /// <summary>
        /// Generated in memory and uploaded at the root of an <see cref="StreamingAssetsLayout.AddressableVariants" />
        /// upload, so a consumer can discover which variants a content version contains without listing the bucket.
        /// </summary>
        private const string VariantsManifestFileName = "variants.meta.json";

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
            public string[] files;
        }

        [Serializable]
        public class UploadInitResponse
        {
            // ReSharper disable InconsistentNaming
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
            // ReSharper restore InconsistentNaming
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
            // ReSharper disable InconsistentNaming
            public DateTime ExpiresAt;
            public UploadInitResponse.FileUploadInfo[] Files;
            // ReSharper restore InconsistentNaming
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

        /// <param name="extension">Extension with a dot. May be compound.</param>
        private static string FetchEncoding(string extension)
        {
            if (extension.EndsWith(".br"))
                return "br";
            if (extension.EndsWith(".gz"))
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

        private static void ValidateVersionCharacters(string label, string version)
        {
            if (!Regex.IsMatch(version, NamePattern))
                throw new ElympicsException($"{label} '{version}' contains invalid characters. Only alphanumeric characters, \"-\" and \".\" are allowed.");
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


        internal static void PrepareValidFiles(
            string clientBuildPath,
            string clientGameVersion,
            out List<(string name, string extension)> validFiles)
        {
            if (string.IsNullOrWhiteSpace(clientBuildPath) || !Directory.Exists(clientBuildPath))
                throw new ElympicsException($"Client build directory '{clientBuildPath}' does not exist.");
            ValidateVersionCharacters("Client game version", clientGameVersion);

            var filePaths = Directory.GetFiles(clientBuildPath);
            var fileNames = filePaths.Select(Path.GetFileName).ToArray();
            validFiles = GetValidFiles(fileNames, CompoundExtensions);

            if (validFiles.Count != CompoundExtensions.Length)
                // TODO: make this exception more informative ~dsygocki 2026-09-11
                throw new ElympicsException((validFiles.Count < CompoundExtensions.Length ? "Some required files are missing\n" : "There are too many files\n")
                    + $"Required extensions: [{string.Join(", ", CompoundExtensions)}]\n"
                    + $"Files in directory: [{string.Join(", ", fileNames)}]\n"
                    + $"Validated files: [{string.Join(", ", validFiles)}]");
        }

        internal static UploadInitRequest CreateInitRequest(
            string gameId,
            string clientGameVersion,
            string serverGameVersion,
            List<(string name, string extension)> validFiles)
        {
            return new UploadInitRequest
            {
                gameId = gameId,
                clientGameVersion = clientGameVersion,
                serverGameVersion = serverGameVersion,
                files = validFiles.Select(fileNameAndExtension => FixedPrefix + fileNameAndExtension.extension).ToArray(),
            };
        }

        internal static UnityWebRequestAsyncOperation SendInitRequest(string apiEndpoint, UploadInitRequest request, Action<UnityWebRequest> completed = null)
        {
            var uri = $"{apiEndpoint}/{ClientBuildsRoute}/init";
            return ElympicsEditorWebClient.SendJsonPostRequestApi(uri, request, completed);
        }

        /// <remarks>Blocking (busy loop).</remarks>
        private static void PutFileToGcs(string filename, string signedUrl, byte[] payload, string contentType, string contentEncoding)
        {
            using var request = UnityWebRequest.Put(signedUrl, payload);
            request.SetRequestHeader(ContentTypeKey, contentType);
            if (!string.IsNullOrEmpty(contentEncoding))
                request.SetRequestHeader(ContentEncodingKey, contentEncoding);

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            { }

            if (operation.webRequest.IsConnectionError() || operation.webRequest.IsProtocolError())
                throw new ElympicsException($"Failed to upload file '{filename}': {operation.webRequest.error}\n{operation.webRequest.downloadHandler.text}");
        }

        internal static void UploadFilesToGcs(
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
                    throw new ElympicsException($"Uploaded file '{fileUploadInfo.FilePath}' does not match expected extension '{expectedFile.extension}'.");

                var localFile = expectedFile.name + expectedFile.extension;
                var progress = (float)(index + 1) / initResponse.Files.Length;
                progressCallback?.Invoke(localFile, progress);

                var filePath = Path.Combine(clientBuildPath, localFile);
                PutFileToGcs(localFile, fileUploadInfo.SignedUrl, File.ReadAllBytes(filePath), FetchContentType(expectedFile.extension), FetchEncoding(expectedFile.extension));
            }
        }

        internal static UnityWebRequestAsyncOperation SendCompleteRequest(string apiEndpoint, string uploadId, bool success)
        {
            var uri = $"{apiEndpoint}/{ClientBuildsRoute}/complete";
            return ElympicsEditorWebClient.SendJsonPostRequestApi(uri,
                new UploadCompleteRequest
                {
                    uploadId = uploadId,
                    success = success,
                });
        }

        #region StreamingAssets content upload

        /// <exception cref="ElympicsException">The content cannot be uploaded; the message says why.</exception>
        internal static void PrepareStreamingAssetsFiles(
            string rootPath,
            string version,
            StreamingAssetsLayout layout,
            out List<string> relativePaths,
            out Dictionary<string, byte[]> generatedFiles)
        {
            generatedFiles = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                throw new ElympicsException($"Content directory '{rootPath}' does not exist.");

            if (string.IsNullOrEmpty(version))
                throw new ElympicsException("Content version cannot be empty.");

            ValidateVersionCharacters("Content version", version);

            if (version.Length > MaxStreamingAssetsVersionLength)
                throw new ElympicsException($"Content version '{version}' is too long - {version.Length} characters, maximum is {MaxStreamingAssetsVersionLength}.");

            var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            List<string> files;
            List<string> variantNames = null;
            switch (layout)
            {
                case StreamingAssetsLayout.AddressableVariants:
                    CollectAddressableVariants(root, out files, out variantNames);
                    break;
                case StreamingAssetsLayout.UnstructuredAssets:
                    files = CollectFilesRecursively(root, root);
                    break;
                default:
                    throw new ElympicsException($"Unknown content layout '{layout}'.");
            }

            if (files.Count == 0)
                throw new ElympicsException($"No files found in '{rootPath}'.");

            files.Sort(StringComparer.OrdinalIgnoreCase);

            if (variantNames != null)
            {
                generatedFiles[VariantsManifestFileName] = CreateVariantsManifest(variantNames);
                files.Add(VariantsManifestFileName);
            }

            relativePaths = files;
        }

        private static byte[] CreateVariantsManifest(List<string> variantNames)
        {
            var manifest = new VariantsManifest { variants = variantNames.OrderBy(name => name, StringComparer.Ordinal).ToArray() };
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(manifest));
        }

        /// <exception cref="ElympicsException">
        /// The directory is not a set of Addressables variant builds. The message lists every problem found, not just the first.
        /// </exception>
        private static void CollectAddressableVariants(string root, out List<string> relativePaths, out List<string> variantNames)
        {
            var errors = new List<string>();

            var looseFiles = Directory.GetFiles(root).Select(Path.GetFileName).ToList();
            if (looseFiles.Count > 0)
                ElympicsLogger.LogWarning($"Uploading {looseFiles.Count} file(s) found directly in '{root}', outside any variant: {string.Join(", ", looseFiles)}.");

            var variantDirectories = Directory.GetDirectories(root);
            if (variantDirectories.Length == 0)
                throw new ElympicsException(
                    $"Content directory '{root}' contains no variant directories. The {nameof(StreamingAssetsLayout.AddressableVariants)} layout expects one directory per variant, "
                    + $"each containing its own \"{AddressablesDirectoryName}\" directory. Use {nameof(StreamingAssetsLayout.UnstructuredAssets)} to upload a path as-is.");

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
                throw new ElympicsException(string.Join(Environment.NewLine, errors));

            relativePaths = files;
            variantNames = names;
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
            var uri = $"{apiEndpoint}/{ClientBuildsRoute}/streaming-assets";
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

        private static string FetchStreamingAssetsContentType(string extension) =>
            StreamingAssetsContentTypes.GetValueOrDefault(extension ?? string.Empty, DefaultContentType);

        internal static void UploadStreamingAssetsToGcs(
            string rootPath,
            StreamingAssetsUploadInitResponse initResponse,
            Dictionary<string, byte[]> generatedFiles,
            Action<string, float> progressCallback = null)
        {
            if (initResponse.Files == null || initResponse.Files.Length == 0)
                throw new ElympicsException("Elympics cloud returned no files to upload.");

            var files = SortStreamingAssetsFilesForUpload(initResponse.Files);

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
                        throw new ElympicsException($"File '{fileUploadInfo.FilePath}' requested by Elympics cloud was not found at '{filePath}'.");
                    payload = File.ReadAllBytes(filePath);
                }

                progressCallback?.Invoke(fileUploadInfo.FilePath, (float)index / files.Count);

                var extension = Path.GetExtension(fileUploadInfo.FilePath);
                PutFileToGcs(fileUploadInfo.FilePath, fileUploadInfo.SignedUrl, payload, FetchStreamingAssetsContentType(extension), FetchEncoding(extension));
            }
        }

        #endregion

    }
}
