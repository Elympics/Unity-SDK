using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    public class TestCreateZipFromDirectory
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "ElympicsZipTests-" + Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(Path.Combine(_root, "source", "Data", "Nested"));
            _ = Directory.CreateDirectory(Path.Combine(_root, "source", "Empty"));
            File.WriteAllText(Path.Combine(_root, "source", "Unity"), "binary");
            File.WriteAllText(Path.Combine(_root, "source", "Data", "Nested", "level0"), "scene");
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_root, true);

        [Test]
        public void ArchiveHasRelativeForwardSlashEntriesWithoutTheBaseDirectory()
        {
            var zip = Path.Combine(_root, "pack.zip");

            ElympicsWebIntegration.CreateZipFromDirectory(Path.Combine(_root, "source") + Path.DirectorySeparatorChar, zip);

            using var archive = new ZipArchive(File.OpenRead(zip), ZipArchiveMode.Read);
            Assert.That(archive.Entries.Select(e => e.FullName), Is.EquivalentTo(new[] { "Unity", "Data/Nested/level0", "Empty/" }));
            using var reader = new StreamReader(archive.GetEntry("Data/Nested/level0").Open());
            Assert.That(reader.ReadToEnd(), Is.EqualTo("scene"));
        }
    }
}
