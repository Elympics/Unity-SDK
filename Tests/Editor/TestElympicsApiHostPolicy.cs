using System;
using Elympics.Editor.Communication;
using Elympics.Editor.UnityCli;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    public class TestElympicsApiHostPolicy
    {
        private Func<string> _originalCustomHost;
        private string _customHost;

        [SetUp]
        public void SetUp()
        {
            _originalCustomHost = ElympicsApiHostPolicy.TrustedCustomHost;
            _customHost = string.Empty;
            ElympicsApiHostPolicy.TrustedCustomHost = () => _customHost;
        }

        [TearDown]
        public void TearDown() => ElympicsApiHostPolicy.TrustedCustomHost = _originalCustomHost;

        [TestCase("https://api.elympics.cc/v2")]
        [TestCase("https://api-stg.elympics.cc/v2/api")]
        [TestCase("https://elympics.ai")]
        [TestCase("https://API.Elympics.AI/x")]
        public void ElympicsHostsOverHttpsAreTrusted(string url) =>
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri(url)), Is.True);

        [TestCase("http://api.elympics.cc/v2")]
        [TestCase("https://example.com")]
        [TestCase("https://evil-elympics.cc")]
        [TestCase("https://elympics.cc.evil.com")]
        [TestCase("https://evilelympics.ai")]
        [TestCase("http://localhost:5000")]
        public void OtherHostsAreRejected(string url)
        {
            var uri = new Uri(url);
            Assert.That(ElympicsApiHostPolicy.IsTrusted(uri), Is.False);
            var exception = Assert.Throws<ElympicsEditorApiException>(() => ElympicsApiHostPolicy.Require(uri));
            Assert.That(exception.Kind, Is.EqualTo(EditorApiErrorKind.UntrustedHost));
            StringAssert.Contains(" Fix: ", exception.Message);
        }

        [Test]
        public void TheCustomHostTrustedOnThisMachineIsAllowedOnAnyScheme()
        {
            _customHost = "localhost";
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri("http://localhost:5000/api")), Is.True);
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri("https://LOCALHOST")), Is.True);
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri("http://localhost.evil.com")), Is.False);
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri("https://api.elympics.cc")), Is.True);
        }

        [Test]
        public void OnlyElympicsApiRequestsAreChecked()
        {
            _ = Assert.Throws<ElympicsEditorApiException>(() =>
                UnityWebRequestEditorApiTransport.Create(new EditorApiRequest { Url = "https://example.com/login" }).Dispose());
            UnityWebRequestEditorApiTransport.Create(new EditorApiRequest { Url = "https://storage.example.com/signed", ElympicsApi = false }).Dispose();
        }

        [Test]
        public void TheCliReportsAnUntrustedHostWithTheHostsFix()
        {
            var exception = Assert.Throws<ElympicsEditorApiException>(() => ElympicsApiHostPolicy.Require(new Uri("https://example.com")));
            var cliException = CliWeb.Map(exception);
            StringAssert.StartsWith(ElympicsCliErrors.UntrustedHost + ":", cliException.Message);
            StringAssert.Contains(ElympicsEditorMenuPaths.TrustCustomApiHost, cliException.Message);
        }
    }
}
