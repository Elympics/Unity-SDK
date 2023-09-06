using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Elympics.Models.Authentication;
using Elympics.Tests.MockWebClient;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AuthRoutes = Elympics.Models.Authentication.Routes;

namespace Elympics.Tests
{
    [TestFixture]
    public class RemoteAuthClientTests
    {
        private const int TimeoutMsPerTest = 10000;
        private readonly RemoteAuthClient _sut = new("test");

        [SetUp]
        public void ResetWebClient() => ElympicsWebClient.Instance = null;

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator NullEthAddressShouldResultInError()
        {
            var ethSignerMock = new EthSignerMock { Address = null };
            Result<AuthData, string> result = null;

            _sut.AuthenticateWithEthAddress(ethSignerMock, arg => result = arg);
            yield return new WaitUntil(() => result != null);

            Assert.IsTrue(result.IsFailure);
            Assert.IsTrue(result.Error.Contains("null"));
        }

        public static string[] InvalidAddresses = { "", "00", "0x12345", "0x0123456789abcdef0123456789abcdef0123456z", "0x0123456789abcdef0123456789abcdef012345678" };

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator InvalidEthAddressFormatShouldResultInError([ValueSource(nameof(InvalidAddresses))] string invalidAddress)
        {
            var ethSignerMock = new EthSignerMock { Address = invalidAddress };
            Result<AuthData, string> result = null;

            _sut.AuthenticateWithEthAddress(ethSignerMock, arg => result = arg);
            yield return new WaitUntil(() => result != null);

            Assert.IsTrue(result.IsFailure);
            Assert.IsTrue(result.Error.Contains("format"));
        }

        public static string[] ValidAddresses = { "0123456789abcdef0123456789abcdef01234567", "0x0123456789abcdef0123456789abcdef01234567" };

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator ValidEthAddressShouldResultInNonceRequest([ValueSource(nameof(ValidAddresses))] string validAddress)
        {
            var ethSignerMock = new EthSignerMock { Address = validAddress };
            var webClientMock = new ElympicsMockWebClient();
            ElympicsWebClient.Instance = webClientMock;
            EthAddressNonceRequest? request = null;
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressNonce, reqParams =>
            {
                request = reqParams.JsonBody as EthAddressNonceRequest?;
                throw new Exception(nameof(ValidEthAddressShouldResultInNonceRequest));
            });
            Result<AuthData, string> result = null;

            _sut.AuthenticateWithEthAddress(ethSignerMock, arg => result = arg);
            yield return new WaitUntil(() => result != null);

            Assert.IsTrue(result.IsFailure);
            Assert.IsTrue(result.Error.Contains(nameof(ValidEthAddressShouldResultInNonceRequest)));

            Assert.IsTrue(request.HasValue);
            var requestedAddress = request.Value.address;
            Assert.AreEqual(42, requestedAddress.Length);
            Assert.IsTrue(requestedAddress.EndsWith(validAddress, true, CultureInfo.InvariantCulture));
            Assert.IsTrue(requestedAddress.StartsWith("0x", true, CultureInfo.InvariantCulture));
        }

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator NullEthSignatureShouldResultInError()
        {
            var ethSignerMock = new EthSignerMock { Address = ValidAddresses[0], Message = "", Signature = null };
            var webClientMock = new ElympicsMockWebClient();
            ElympicsWebClient.Instance = webClientMock;
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressNonce, reqParams => Guid.Empty.ToString());
            Result<AuthData, string> result = null;

            _sut.AuthenticateWithEthAddress(ethSignerMock, arg => result = arg);
            yield return new WaitUntil(() => result != null);

            Assert.IsTrue(result.IsFailure);
            Assert.IsTrue(result.Error.Contains("null"));
        }

        public static string[] InvalidSignatures = { "", "xyz", "-1", new string(Enumerable.Repeat('a', 131).ToArray()), "0x" + new string(Enumerable.Repeat('b', 131).ToArray()) };

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator InvalidEthSignatureFormatShouldResultInError([ValueSource(nameof(InvalidSignatures))] string invalidSignature)
        {
            var ethSignerMock = new EthSignerMock
            {
                Address = ValidAddresses[0],
                Message = "",
                Signature = invalidSignature,
            };
            var webClientMock = new ElympicsMockWebClient();
            ElympicsWebClient.Instance = webClientMock;
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressNonce, reqParams => Guid.Empty.ToString());
            Result<AuthData, string> result = null;

            _sut.AuthenticateWithEthAddress(ethSignerMock, arg => result = arg);
            yield return new WaitUntil(() => result != null);

            Assert.IsTrue(result.IsFailure);
            Assert.IsTrue(result.Error.Contains("format"));
        }

        public static string[] ValidSignatures = { new string(Enumerable.Repeat('a', 130).ToArray()), "0x" + new string(Enumerable.Repeat('b', 130).ToArray()) };

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator ValidEthSignatureFormatShouldResultInAuthRequest([ValueSource(nameof(ValidSignatures))] string validSignature)
        {
            var ethSignerMock = new EthSignerMock
            {
                Address = ValidAddresses[0],
                Message = "",
                Signature = validSignature,
            };
            var webClientMock = new ElympicsMockWebClient();
            ElympicsWebClient.Instance = webClientMock;
            EthAddressAuthRequest? request = null;
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressNonce, reqParams => Guid.Empty.ToString());
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressAuth, reqParams =>
            {
                request = reqParams.JsonBody as EthAddressAuthRequest?;
                throw new Exception(nameof(ValidEthSignatureFormatShouldResultInAuthRequest));
            });
            Result<AuthData, string> result = null;

            _sut.AuthenticateWithEthAddress(ethSignerMock, arg => result = arg);
            yield return new WaitUntil(() => result != null);

            Assert.IsTrue(result.IsFailure);
            Assert.IsTrue(result.Error.Contains(nameof(ValidEthSignatureFormatShouldResultInAuthRequest)));

            Assert.IsTrue(request.HasValue);
            var requestSignature = request.Value.sig;
            Assert.AreEqual(132, requestSignature.Length);
            Assert.IsTrue(requestSignature.EndsWith(ethSignerMock.Signature, true, CultureInfo.InvariantCulture));
            Assert.IsTrue(requestSignature.StartsWith("0x", true, CultureInfo.InvariantCulture));
        }

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator AuthRequestShouldContainMessageFromEthSigner()
        {
            var ethSignerMock = new EthSignerMock
            {
                Address = ValidAddresses[0],
                Message = "Hello world!",
                Signature = ValidSignatures[0],
            };
            var webClientMock = new ElympicsMockWebClient();
            ElympicsWebClient.Instance = webClientMock;
            EthAddressAuthRequest? request = null;
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressNonce, reqParams => Guid.Empty.ToString());
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressAuth, reqParams =>
            {
                request = reqParams.JsonBody as EthAddressAuthRequest?;
                throw new Exception(nameof(AuthRequestShouldContainMessageFromEthSigner));
            });
            Result<AuthData, string> result = null;

            _sut.AuthenticateWithEthAddress(ethSignerMock, arg => result = arg);
            yield return new WaitUntil(() => result != null);

            Assert.IsTrue(result.IsFailure);
            Assert.IsTrue(result.Error.Contains(nameof(AuthRequestShouldContainMessageFromEthSigner)));

            Assert.IsTrue(request.HasValue);
            var requestMessage = request.Value.msg;
            Assert.AreEqual(ethSignerMock.Message, ConvertHexStringToUtf8String(requestMessage));
        }

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator ValidEthDataShouldResultInAuthDataBeingReturned()
        {
            var ethSignerMock = new EthSignerMock
            {
                Address = ValidAddresses[0],
                Message = "",
                Signature = ValidSignatures[0],
            };
            var webClientMock = new ElympicsMockWebClient();
            ElympicsWebClient.Instance = webClientMock;
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressNonce, reqParams => Guid.Empty.ToString());
            var expectedAuthData = new AuthData(Guid.Empty, "", AuthType.EthAddress);
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressAuth, reqParams => new AuthenticationDataResponse
            {
                jwtToken = expectedAuthData.JwtToken,
                userId = expectedAuthData.UserId.ToString(),
            });
            Result<AuthData, string> result = null;

            _sut.AuthenticateWithEthAddress(ethSignerMock, arg => result = arg);
            yield return new WaitUntil(() => result != null);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(expectedAuthData.JwtToken, result.Value.JwtToken);
            Assert.AreEqual(expectedAuthData.UserId, result.Value.UserId);
            Assert.AreEqual(expectedAuthData.AuthType, result.Value.AuthType);
        }

        private static string ConvertHexStringToUtf8String(string hexString)
        {
            hexString = hexString.ToLowerInvariant();
            var hasPrefix = hexString.StartsWith("0x");
            var bytes = new List<byte>();
            for (var i = hasPrefix ? 1 : 0; i < hexString.Length / 2; i++)
                bytes.Add((byte)((HexDigitToNumber(hexString[i * 2]) << 4) + HexDigitToNumber(hexString[i * 2 + 1])));
            return Encoding.UTF8.GetString(bytes.ToArray());

            static byte HexDigitToNumber(char c) => c >= 'a'
                ? (byte)(c - 'a' + 10)
                : (byte)(c - '0');
        }
    }
}
