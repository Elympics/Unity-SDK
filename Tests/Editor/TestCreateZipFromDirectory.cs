using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    public class TestCreateZipFromDirectory
    {
        private TempDirectory _temp;

        [SetUp]
        public void SetUp()
        {
            _temp = new TempDirectory("ElympicsZipTests");
            _ = Directory.CreateDirectory(Path.Combine(_temp.Path, "source", "Data", "Nested"));
            _ = Directory.CreateDirectory(Path.Combine(_temp.Path, "source", "Empty"));
            File.WriteAllText(Path.Combine(_temp.Path, "source", "Unity"), "binary");
            File.WriteAllText(Path.Combine(_temp.Path, "source", "Data", "Nested", "level0"), "scene");
        }

        [TearDown]
        public void TearDown() => _temp.Dispose();

        [Test]
        public void ArchiveHasRelativeForwardSlashEntriesWithoutTheBaseDirectory()
        {
            var zip = Path.Combine(_temp.Path, "pack.zip");

            ElympicsWebIntegration.CreateZipFromDirectory(Path.Combine(_temp.Path, "source") + Path.DirectorySeparatorChar, zip);

            using var archive = new ZipArchive(File.OpenRead(zip), ZipArchiveMode.Read);
            Assert.That(archive.Entries.Select(e => e.FullName), Is.EquivalentTo(new[] { "Unity", "Data/Nested/level0", "Empty/" }));
            using var reader = new StreamReader(archive.GetEntry("Data/Nested/level0").Open());
            Assert.That(reader.ReadToEnd(), Is.EqualTo("scene"));
        }

        [Test]
        public void TimestampBeforeTheZipRangeIsClamped()
        {
            var zip = Path.Combine(_temp.Path, "pack.zip");
            File.SetLastWriteTime(Path.Combine(_temp.Path, "source", "Unity"), new DateTime(1970, 1, 2));

            ElympicsWebIntegration.CreateZipFromDirectory(Path.Combine(_temp.Path, "source"), zip);

            using var archive = new ZipArchive(File.OpenRead(zip), ZipArchiveMode.Read);
            Assert.That(archive.GetEntry("Unity").LastWriteTime.DateTime, Is.EqualTo(new DateTime(1980, 1, 1)));
        }
    }
}
