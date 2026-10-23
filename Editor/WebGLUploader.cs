using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Elympics.Core.Logger;
using UnityEngine;

namespace Elympics.Editor
{
    /// <summary>The files or arguments to upload are invalid, so nothing was sent.</summary>
    internal class ElympicsUploadInputException : ElympicsException
    {
        public ElympicsUploadInputException(string message)
            : base(message)
        { }
    }

    /// <summary>The client build directory is missing or doesn't hold exactly the expected build files.</summary>
    internal sealed class ElympicsClientBuildFilesException : ElympicsUploadInputException
    {
        public ElympicsClientBuildFilesException(string message)
            : base(message)
        { }
    }

    public static class WebGLUploader
    {
        private const string NamePattern = "^[0-9-a-zA-Z.]+$";
        private const string FixedPrefix = "Build";

        private const int MaxStreamingAssetsVersionLength = 63;
        private const string AddressablesDirectoryName = "aa";
        private const string AddressablesSettingsFileName = "settings.json";
        private const string CatalogFileNamePrefix = "catalog";
        private const string DefaultContentType = "application/octet-stream";

        /// <summary>Generated in memory and uploaded to bucket so client doesn't have to list it to discover variants.</summary>
        /// <remarks>Only used in <see cref="StreamingAssetsLayout.AddressableVariants" />.</remarks>
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

        /// <summary>Content-Type header values for GCS PUT requests.</summary>
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

        private static bool TryGetFullExtension(string filename, string[] knownCompoundExtensions, out string compoundExtension)
        {
            compoundExtension = string.Empty;
            foreach (var ext in knownCompoundExtensions)
            {
                var extIndex = filename.IndexOf(ext, StringComparison.OrdinalIgnoreCase);
                if (extIndex < 0)
                    continue;

                compoundExtension = filename[extIndex..];
                return true;
            }

            return false;
        }

        internal static bool DoesFileHaveGivenCompoundExtension(string filePath, string compoundExtension)
        {
            var filename = Path.GetFileName(filePath);
            if (TryGetFullExtension(filename, CompoundExtensions, out var fileCompoundExtension))
                return fileCompoundExtension == compoundExtension;
            return false;
        }

        /// <param name="extension">Extension with a dot. May be compound.</param>
        internal static string FetchEncoding(string extension)
        {
            if (extension.EndsWith(".br"))
                return "br";
            if (extension.EndsWith(".gz"))
                return "gzip";
            return string.Empty;
        }

        internal static string FetchContentType(string fileExtension)
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
                throw new ElympicsUploadInputException($"{label} '{version}' contains invalid characters. Only alphanumeric characters, \"-\" and \".\" are allowed.");
        }

        internal static List<(string name, string extension)> GetValidFiles(string[] filenames, string[] knownCompoundExtensions)
        {
            return filenames.Select(filePath =>
            {
                if (TryGetFullExtension(Path.GetFileName(filePath), knownCompoundExtensions, out var compoundExtension))
                {
                    var splitExtension = compoundExtension.Split('.');
                    var split = filePath.Split('.');
                    var name = string.Join(".", split.Take(split.Length - splitExtension.Length + 1));
                    return (name, compoundExtension);
                }

                return (string.Empty, string.Empty);
            }).Where(file => !string.IsNullOrEmpty(file.Item1)).ToList();
        }

        #endregion


        internal const string BuildFolderName = "Build";

        /// <param name="clientBuildPath">The WebGL build's <see cref="BuildFolderName"/> folder or the build root containing it.</param>
        /// <returns>The folder that holds the build files, and the files.</returns>
        internal static (string path, List<(string name, string extension)> validFiles) ResolveClientBuildFiles(string clientBuildPath, string clientGameVersion)
        {
            if (string.IsNullOrWhiteSpace(clientBuildPath) || !Directory.Exists(clientBuildPath))
                throw new ElympicsClientBuildFilesException($"Client build directory '{clientBuildPath}' does not exist.");
            ValidateVersionCharacters("Client game version", clientGameVersion);

            var fileNames = GetFileNames(clientBuildPath);
            var validFiles = GetValidFiles(fileNames, CompoundExtensions);

            var buildFolderPath = Path.Combine(clientBuildPath, BuildFolderName);
            if (validFiles.Count == 0 && Directory.Exists(buildFolderPath))
            {
                var buildFolderFileNames = GetFileNames(buildFolderPath);
                var buildFolderValidFiles = GetValidFiles(buildFolderFileNames, CompoundExtensions);
                if (buildFolderValidFiles.Count > 0)
                {
                    ElympicsLogger.LogInfo($"Using the client build files in '{buildFolderPath}'.");
                    clientBuildPath = buildFolderPath;
                    fileNames = buildFolderFileNames;
                    validFiles = buildFolderValidFiles;
                }
            }

            if (validFiles.Count != CompoundExtensions.Length)
                throw new ElympicsClientBuildFilesException((validFiles.Count < CompoundExtensions.Length ? "Some required files are missing" : "There are too many files")
                    + $" in client build directory '{clientBuildPath}'. Pass the WebGL build's root or its {BuildFolderName} folder.\n"
                    + $"Required extensions: [{string.Join(", ", CompoundExtensions)}]\n"
                    + $"Files in directory: [{string.Join(", ", fileNames)}]\n"
                    + $"Validated files: [{string.Join(", ", validFiles)}]");

            return (clientBuildPath, validFiles);

            static string[] GetFileNames(string directory) => Directory.GetFiles(directory).Select(Path.GetFileName).ToArray();
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

        #region StreamingAssets content upload

        /// <exception cref="ElympicsUploadInputException">The content cannot be uploaded; the message says why.</exception>
        internal static void PrepareStreamingAssetsFiles(
            string rootPath,
            string version,
            StreamingAssetsLayout layout,
            out List<string> relativePaths,
            out Dictionary<string, byte[]> generatedFiles)
        {
            generatedFiles = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                throw new ElympicsUploadInputException($"Content directory '{rootPath}' does not exist.");

            if (string.IsNullOrEmpty(version))
                throw new ElympicsUploadInputException("Content version cannot be empty.");

            ValidateVersionCharacters("Content version", version);

            if (version.Length > MaxStreamingAssetsVersionLength)
                throw new ElympicsUploadInputException($"Content version '{version}' is too long - {version.Length} characters, maximum is {MaxStreamingAssetsVersionLength}.");

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
                    throw new ElympicsUploadInputException($"Unknown content layout '{layout}'.");
            }

            if (files.Count == 0)
                throw new ElympicsUploadInputException($"No files found in '{rootPath}'.");

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

        /// <exception cref="ElympicsUploadInputException">
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
                throw new ElympicsUploadInputException(
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
                throw new ElympicsUploadInputException(string.Join(Environment.NewLine, errors));

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

        internal static string FetchStreamingAssetsContentType(string extension) =>
            StreamingAssetsContentTypes.GetValueOrDefault(extension ?? string.Empty, DefaultContentType);

        #endregion
    }
}
