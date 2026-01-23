using System.Linq;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    public class TestGetValidFiles
    {
        private const string DefaultPrefix = "Build";

        private static readonly string[] CompoundExtensions = ElympicsWebIntegration.compoundExtensions;
        private static readonly string[] InvalidFileNames = { "readme.txt", "notes.md", "config.yaml" };

        private static string[] ValidFileNames =>
            CompoundExtensions.Select(ext => DefaultPrefix + ext).ToArray();

        [Test]
        public void ReturnsEmpty_WhenNoFilesMatch()
        {
            var result = ElympicsWebIntegration.GetValidFiles(InvalidFileNames, CompoundExtensions);

            Assert.IsEmpty(result);
        }

        [Test]
        public void ReturnsEmpty_WhenInputIsEmpty()
        {
            var result = ElympicsWebIntegration.GetValidFiles(new string[0], CompoundExtensions);

            Assert.IsEmpty(result);
        }

        [Test]
        public void ParsesEachKnownExtension([ValueSource(nameof(CompoundExtensions))] string extension)
        {
            var fileName = DefaultPrefix + extension;
            var fileNames = new[] { fileName };

            var result = ElympicsWebIntegration.GetValidFiles(fileNames, CompoundExtensions);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(DefaultPrefix, result[0].name);
            Assert.AreEqual(extension, result[0].extension);
        }

        [Test]
        public void ParsesAllValidFilesTogether()
        {
            var result = ElympicsWebIntegration.GetValidFiles(ValidFileNames, CompoundExtensions);

            Assert.AreEqual(CompoundExtensions.Length, result.Count);
        }

        [Test]
        public void FiltersOutInvalidFiles()
        {
            var mixed = ValidFileNames.Concat(InvalidFileNames).ToArray();

            var result = ElympicsWebIntegration.GetValidFiles(mixed, CompoundExtensions);

            Assert.AreEqual(CompoundExtensions.Length, result.Count);
            foreach (var file in result)
                Assert.AreEqual(DefaultPrefix, file.name);
        }

        [Test]
        public void ExtensionMatchingIsCaseInsensitive([ValueSource(nameof(CompoundExtensions))] string extension)
        {
            var fileName = DefaultPrefix + extension.ToUpperInvariant();
            var fileNames = new[] { fileName };

            var result = ElympicsWebIntegration.GetValidFiles(fileNames, CompoundExtensions);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void UsesFirstDotSegmentAsName([ValueSource(nameof(CompoundExtensions))] string extension)
        {
            const string customPrefix = "MyGame";
            var fileName = customPrefix + extension;
            var fileNames = new[] { fileName };

            var result = ElympicsWebIntegration.GetValidFiles(fileNames, CompoundExtensions);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(customPrefix, result[0].name);
            Assert.AreEqual(extension, result[0].extension);
        }

        [Test]
        public void ParsesPrefixWithHyphenAndNumber([ValueSource(nameof(CompoundExtensions))] string extension)
        {
            const string prefixWithHyphen = "Build-1";
            var fileName = prefixWithHyphen + extension;
            var fileNames = new[] { fileName };

            var result = ElympicsWebIntegration.GetValidFiles(fileNames, CompoundExtensions);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(prefixWithHyphen, result[0].name);
            Assert.AreEqual(extension, result[0].extension);
        }

        [Test]
        public void ParsesPrefixWithDot([ValueSource(nameof(CompoundExtensions))] string extension)
        {
            const string prefixWithDot = "Build.1";
            var fileName = prefixWithDot + extension;
            var fileNames = new[] { fileName };

            var result = ElympicsWebIntegration.GetValidFiles(fileNames, CompoundExtensions);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(prefixWithDot, result[0].name);
            Assert.AreEqual(extension, result[0].extension);
        }

        [Test]
        public void ParseFilesWithBrotli([ValueSource(nameof(CompoundExtensions))] string extension)
        {
            const string packExtension = ".brotli";
            var fileName = DefaultPrefix + extension + packExtension;
            var fileNames = new[] { fileName };

            var result = ElympicsWebIntegration.GetValidFiles(fileNames, CompoundExtensions);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(DefaultPrefix, result[0].name);
            Assert.AreEqual(extension + packExtension, result[0].extension);
        }
    }
}
