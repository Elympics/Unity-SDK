#nullable enable

using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    public class TestStreamingAssetsContentType
    {
        [TestCase(".json", "application/json")]
        [TestCase(".xml", "application/xml")]
        [TestCase(".txt", "text/plain")]
        [TestCase(".TXT", "text/plain")]
        [TestCase(".bin", "application/octet-stream")]
        [TestCase(".hash", "application/octet-stream")]
        [TestCase("", "application/octet-stream")]
        [TestCase(null, "application/octet-stream")]
        public void ContentTypeMatchesTheOneTheBackendSigns(string? extension, string expected) =>
            Assert.That(WebGLUploader.FetchStreamingAssetsContentType(extension), Is.EqualTo(expected));
    }
}
