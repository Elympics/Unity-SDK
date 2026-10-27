#nullable enable

using System;
using Elympics.Editor.Communication;
using Elympics.Editor.UnityCli;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    public class TestElympicsApiHostPolicy
    {
        private Func<string> _originalCustomHost = null!;
        private string _customHost = null!;

        [SetUp]
        public void SetUp()
        {
            _originalCustomHost = ElympicsApiHostPolicy.GetTrustedCustomHost;
            _customHost = string.Empty;
            ElympicsApiHostPolicy.GetTrustedCustomHost = () => _customHost;
        }

        [TearDown]
        public void TearDown() => ElympicsApiHostPolicy.GetTrustedCustomHost = _originalCustomHost;

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
        [TestCase("https://x@evil.com/api")]
        [TestCase("https://api.elympics.cc@evil.com/")]
        [TestCase("https://api.elympics.cc:pw@evil.com/")]
        [TestCase("https://api.elympics.cc./")]
        public void OtherHostsAreRejected(string url)
        {
            var uri = new Uri(url);
            Assert.That(ElympicsApiHostPolicy.IsTrusted(uri), Is.False);
            var exception = Assert.Throws<ElympicsEditorApiException>(() => ElympicsApiHostPolicy.Require(uri));
            Assert.That(exception.Kind, Is.EqualTo(EditorApiErrorKind.UntrustedHost));
            StringAssert.Contains(" Fix: ", exception.Message);
        }

        [Test]
        public void TheCustomHostTrustedOnThisMachineIsAllowedOnHttpsAndLocalhostHttp()
        {
            _customHost = "localhost";
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri("http://localhost:5000/api")), Is.True);
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri("https://LOCALHOST")), Is.True);
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri("http://localhost.evil.com")), Is.False);
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri("https://api.elympics.cc")), Is.True);
        }

        [TestCase("127.0.0.1", "http://127.0.0.1:5000", true)]
        [TestCase("127.0.0.2", "http://127.0.0.2:5000", true)]
        [TestCase("[::1]", "http://[::1]:5000", true)]
        [TestCase("[::1]", "https://[::1]:5000", true)]
        [TestCase("example.com", "https://example.com", true)]
        [TestCase("example.com", "http://example.com", false)]
        [TestCase("192.168.0.5", "http://192.168.0.5:5000", false)]
        public void AHttpCustomHostIsTrustedOnlyOnLoopback(string customHost, string url, bool expected)
        {
            _customHost = customHost;
            var uri = new Uri(url);
            Assert.That(ElympicsApiHostPolicy.IsTrusted(uri), Is.EqualTo(expected));
            if (!expected)
                _ = Assert.Throws<ElympicsEditorApiException>(() => ElympicsApiHostPolicy.Require(uri));
        }

        [Test]
        public void AnIdnHostIsComparedInItsPunycodeForm()
        {
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri("https://münchen.elympics.cc")), Is.True);
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri("https://еlympics.cc")), Is.False);
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri("https://еlympics.ai/api")), Is.False);

            _customHost = new Uri("https://münchen.example").Host;
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri("https://münchen.example")), Is.True);
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri("http://münchen.example")), Is.False);
        }

        [TestCase("elympics.ai", "https://elympics.ai", true)]
        [TestCase("elympics.cc", "https://elympics.cc/v2", true)]
        [TestCase("elympics.ai", "http://elympics.ai", false)]
        [TestCase("elympics.cc", "http://elympics.cc", false)]
        [TestCase("elympics.cc", "https://api.elympics.cc", true)]
        public void ACustomHostEqualToATrustedDomainDoesNotChangeThePolicy(string customHost, string url, bool expected)
        {
            _customHost = customHost;
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri(url)), Is.EqualTo(expected));
        }

        [Test]
        public void ATrustedHostIsTrustedOnAnyPort() =>
            Assert.That(ElympicsApiHostPolicy.IsTrusted(new Uri("https://api.elympics.cc:8443/v2")), Is.True);

        [TestCase(true, true, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        public void TheLoginIsSentOnlyToElympicsApiRequestsThatNeedAuth(bool elympicsApi, bool auth, bool expectedHeader)
        {
            using var _ = new EditorApiScope(new FakeLoginStore { AuthToken = "secret" });
            using var webRequest = UnityWebRequestEditorApiTransport.Create(new EditorApiRequest
            {
                Url = elympicsApi ? "https://api.elympics.cc/v2" : "https://storage.example.com/signed",
                ElympicsApi = elympicsApi,
                Auth = auth,
            });
            Assert.That(webRequest.GetRequestHeader("Authorization"), expectedHeader ? Is.EqualTo("Bearer secret") : Is.Null.Or.Empty);
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
