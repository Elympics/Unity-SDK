using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Networking;

namespace Elympics.Editor
{
    public static class WebGLUploader
    {
        private const string ContentEncodingKey = "Content-Encoding";
        private const string ContentTypeKey = "Content-Type";
        private const string NamePattern = "^[0-9-a-zA-Z.]+$";
        private const string FixedPrefix = "Build";

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

    }
}
