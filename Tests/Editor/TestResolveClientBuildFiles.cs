using System.IO;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    public class TestResolveClientBuildFiles
    {
        private const string ClientGameVersion = "v1";

        private TempDirectory _temp;
        private string _buildRoot;
        private string _buildFolder;

        [SetUp]
        public void SetUp()
        {
            _temp = new TempDirectory("ElympicsTestPrepareValidFiles");
            _buildRoot = _temp.Path;
            _buildFolder = Path.Combine(_buildRoot, WebGLUploader.BuildFolderName);
            _ = Directory.CreateDirectory(_buildFolder);
            File.WriteAllText(Path.Combine(_buildRoot, "index.html"), string.Empty);
        }

        [TearDown]
        public void TearDown() => _temp.Dispose();

        private void CreateBuildFiles()
        {
            foreach (var extension in WebGLUploader.CompoundExtensions)
                File.WriteAllText(Path.Combine(_buildFolder, "Build" + extension), string.Empty);
        }

        [Test]
        public void KeepsPath_WhenItIsTheBuildFolder()
        {
            CreateBuildFiles();
            var (path, validFiles) = WebGLUploader.ResolveClientBuildFiles(_buildFolder, ClientGameVersion);

            Assert.AreEqual(_buildFolder, path);
            Assert.AreEqual(WebGLUploader.CompoundExtensions.Length, validFiles.Count);
        }

        [Test]
        public void UsesBuildFolder_WhenPathIsTheBuildRoot()
        {
            CreateBuildFiles();
            var (path, validFiles) = WebGLUploader.ResolveClientBuildFiles(_buildRoot, ClientGameVersion);

            Assert.AreEqual(_buildFolder, path);
            Assert.AreEqual(WebGLUploader.CompoundExtensions.Length, validFiles.Count);
        }

        [Test]
        public void Throws_WhenNeitherPathNorBuildFolderHasTheFiles()
        {
            var exception = Assert.Throws<ElympicsClientBuildFilesException>(() => WebGLUploader.ResolveClientBuildFiles(_buildRoot, ClientGameVersion));

            StringAssert.Contains("missing", exception.Message);
        }
    }
}
