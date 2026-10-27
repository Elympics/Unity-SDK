using System;
using System.Linq;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    public class TestGetValidFiles
    {
        private const string DefaultPrefix = "Build";

        private static readonly string[] CompoundExtensions = WebGLUploader.CompoundExtensions;
        private static readonly string[] InvalidFileNames = { "readme.txt", "notes.md", "config.yaml" };

        private static string[] ValidFileNames =>
            CompoundExtensions.Select(ext => DefaultPrefix + ext).ToArray();

        [Test]
        public void ReturnsEmpty_WhenNoFilesMatch()
        {
            var result = WebGLUploader.GetValidFiles(InvalidFileNames, CompoundExtensions);

            Assert.IsEmpty(result);
        }

        [Test]
        public void ReturnsEmpty_WhenInputIsEmpty()
        {
            var result = WebGLUploader.GetValidFiles(Array.Empty<string>(), CompoundExtensions);

            Assert.IsEmpty(result);
        }

        [Test]
        public void ParsesEachKnownExtension([ValueSource(nameof(CompoundExtensions))] string extension)
        {
            var fileName = DefaultPrefix + extension;
            var fileNames = new[] { fileName };

            var result = WebGLUploader.GetValidFiles(fileNames, CompoundExtensions);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(DefaultPrefix, result[0].name);
            Assert.AreEqual(extension, result[0].extension);
        }

        [Test]
        public void FiltersOutInvalidFiles()
        {
            var mixed = ValidFileNames.Concat(InvalidFileNames).ToArray();

            var result = WebGLUploader.GetValidFiles(mixed, CompoundExtensions);

            Assert.AreEqual(CompoundExtensions.Length, result.Count);
            foreach (var file in result)
                Assert.AreEqual(DefaultPrefix, file.name);
        }

        [Test]
        public void ExtensionMatchingIsCaseInsensitive([ValueSource(nameof(CompoundExtensions))] string extension)
        {
            var fileName = DefaultPrefix + extension.ToUpperInvariant();
            var fileNames = new[] { fileName };

            var result = WebGLUploader.GetValidFiles(fileNames, CompoundExtensions);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void ParsesPrefix(
            [Values("MyGame", DefaultPrefix + "-1", DefaultPrefix + ".1")] string prefix,
            [ValueSource(nameof(CompoundExtensions))] string extension)
        {
            var result = WebGLUploader.GetValidFiles(new[] { prefix + extension }, CompoundExtensions);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(prefix, result[0].name);
            Assert.AreEqual(extension, result[0].extension);
        }

        [Test]
        public void ParseFilesWithBrotli([ValueSource(nameof(CompoundExtensions))] string extension)
        {
            const string packExtension = ".brotli";
            var fileName = DefaultPrefix + extension + packExtension;
            var fileNames = new[] { fileName };

            var result = WebGLUploader.GetValidFiles(fileNames, CompoundExtensions);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(DefaultPrefix, result[0].name);
            Assert.AreEqual(extension + packExtension, result[0].extension);
        }

        [Test]
        public void MatchesCompoundExtension_WhenADirectorySegmentCarriesAnotherKnownExtension()
        {
            const string filename = DefaultPrefix + ".data";
            const string fullPath = "release.wasm/" + filename;

            Assert.IsTrue(WebGLUploader.DoesFileHaveGivenCompoundExtension(filename, ".data"));
            Assert.IsTrue(WebGLUploader.DoesFileHaveGivenCompoundExtension(fullPath, ".data"));
        }
    }
}
