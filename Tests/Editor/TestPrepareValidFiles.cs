using System.IO;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    public class TestPrepareValidFiles
    {
        private const string ClientGameVersion = "v1";

        private string _buildRoot;
        private string _buildFolder;

        [SetUp]
        public void SetUp()
        {
            _buildRoot = Path.Combine(Path.GetTempPath(), "ElympicsTestPrepareValidFiles_" + Path.GetRandomFileName());
            _buildFolder = Path.Combine(_buildRoot, WebGLUploader.BuildFolderName);
            _ = Directory.CreateDirectory(_buildFolder);
            File.WriteAllText(Path.Combine(_buildRoot, "index.html"), string.Empty);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_buildRoot, true);

        private void CreateBuildFiles()
        {
            foreach (var extension in WebGLUploader.CompoundExtensions)
                File.WriteAllText(Path.Combine(_buildFolder, "Build" + extension), string.Empty);
        }

        [Test]
        public void KeepsPath_WhenItIsTheBuildFolder()
        {
            CreateBuildFiles();
            var path = _buildFolder;

            WebGLUploader.PrepareValidFiles(ref path, ClientGameVersion, out var validFiles);

            Assert.AreEqual(_buildFolder, path);
            Assert.AreEqual(WebGLUploader.CompoundExtensions.Length, validFiles.Count);
        }

        [Test]
        public void UsesBuildFolder_WhenPathIsTheBuildRoot()
        {
            CreateBuildFiles();
            var path = _buildRoot;

            WebGLUploader.PrepareValidFiles(ref path, ClientGameVersion, out var validFiles);

            Assert.AreEqual(_buildFolder, path);
            Assert.AreEqual(WebGLUploader.CompoundExtensions.Length, validFiles.Count);
        }

        [Test]
        public void Throws_WhenNeitherPathNorBuildFolderHasTheFiles()
        {
            var path = _buildRoot;

            var exception = Assert.Throws<ElympicsException>(() => WebGLUploader.PrepareValidFiles(ref path, ClientGameVersion, out _));

            StringAssert.Contains("missing", exception.Message);
            Assert.AreEqual(_buildRoot, path);
        }
    }
}
