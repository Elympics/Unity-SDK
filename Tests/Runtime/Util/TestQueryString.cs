#nullable enable

using System.Collections.Generic;
using NUnit.Framework;

namespace Elympics.Tests
{
    [Category("Util")]
    public class TestQueryString
    {
        [Test]
        public void ParseDecodesPercentEncodingAndPlus()
        {
            var query = QueryString.Parse("?name=John+Doe&city=Krak%C3%B3w&sign=a%2Bb");

            Assert.That(query["name"], Is.EqualTo("John Doe"));
            Assert.That(query["city"], Is.EqualTo("Kraków"));
            Assert.That(query["sign"], Is.EqualTo("a+b"));
        }

        [Test]
        public void ParseJoinsRepeatedKeysLikeHttpUtility()
        {
            var query = QueryString.Parse("a=1&a=2");

            Assert.That(query["a"], Is.EqualTo("1,2"));
            Assert.That(query.GetValues("a"), Is.EqualTo(new[] { "1", "2" }));
        }

        [Test]
        public void ParseKeepsEmptyValuesAndStoresBareSegmentsUnderNullKey()
        {
            var query = QueryString.Parse("empty=&flag&&x=1");

            Assert.That(query["empty"], Is.EqualTo(string.Empty));
            Assert.That(query[null], Is.EqualTo("flag"));
            Assert.That(query["flag"], Is.Null);
            Assert.That(query["x"], Is.EqualTo("1"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("?")]
        public void ParseOfEmptyQueryIsEmpty(string? query) => Assert.That(QueryString.Parse(query), Is.Empty);

        [Test]
        public void AppendKeepsExistingQueryAndEscapesNewValues()
        {
            var url = QueryString.Append("https://example.com/path?existing=1", new Dictionary<string, string> { ["key"] = "a b&c" });

            Assert.That(url, Is.EqualTo("https://example.com/path?existing=1&key=a%20b%26c"));
            Assert.That(QueryString.Parse(new System.Uri(url).Query)["key"], Is.EqualTo("a b&c"));
        }

        [Test]
        public void AppendToUrlWithoutQuery()
        {
            var url = QueryString.Append("wss://example.com/ws", new[] { new KeyValuePair<string, string>("jwt_token", "abc.def") });

            Assert.That(url, Is.EqualTo("wss://example.com/ws?jwt_token=abc.def"));
        }

        [Test]
        public void AppendSinglePairToBuilder()
        {
            var url = QueryString.Append(new System.UriBuilder("wss://example.com/ws?existing=1"), "jwt_token", "a b");

            Assert.That(url, Is.EqualTo("wss://example.com/ws?existing=1&jwt_token=a%20b"));
        }
    }
}
