using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using Cysharp.Threading.Tasks;
using Elympics.Models.Authentication;
using Elympics.Tests.MockWebClient;
using NUnit.Framework;
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
        public IEnumerator NullEthAddressShouldResultInError() => UniTask.ToCoroutine(async () =>
        {
            var ethSignerMock = new EthSignerMock { Address = null! };
            var result = await _sut.AuthenticateWithEthAddress(ethSignerMock);
            Assert.IsTrue(result.IsFailure);
            Assert.IsTrue(result.Error.Contains("null"));

        });

        public static string[] InvalidAddresses = { "", "00", "0x12345", "0x0123456789abcdef0123456789abcdef0123456z", "0x0123456789abcdef0123456789abcdef012345678" };

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator InvalidEthAddressFormatShouldResultInError([ValueSource(nameof(InvalidAddresses))] string invalidAddress) => UniTask.ToCoroutine(async () =>
        {
            var ethSignerMock = new EthSignerMock { Address = invalidAddress };

            var result = await _sut.AuthenticateWithEthAddress(ethSignerMock);

            Assert.IsTrue(result.IsFailure);
            Assert.IsTrue(result.Error.Contains("format"));
        });

        public static string[] ValidAddresses = { "0123456789abcdef0123456789abcdef01234567", "0x0123456789abcdef0123456789abcdef01234567" };

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator ValidEthAddressShouldResultInNonceRequest([ValueSource(nameof(ValidAddresses))] string validAddress) => UniTask.ToCoroutine(async () =>
        {
            var ethSignerMock = new EthSignerMock { Address = validAddress };
            var webClientMock = new ElympicsMockWebClient();
            ElympicsWebClient.Instance = webClientMock;
            EthAddressNonceRequest? request = null;
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressNonce,
                reqParams =>
                {
                    request = reqParams.JsonBody as EthAddressNonceRequest?;
                    throw new Exception(nameof(ValidEthAddressShouldResultInNonceRequest));
                });
            var result = await _sut.AuthenticateWithEthAddress(ethSignerMock);

            Assert.IsTrue(result.IsFailure);
            Assert.IsTrue(result.Error.Contains(nameof(ValidEthAddressShouldResultInNonceRequest)));

            Assert.IsTrue(request.HasValue);
            var requestedAddress = request!.Value.address;
            Assert.AreEqual(42, requestedAddress.Length);
            Assert.IsTrue(requestedAddress.EndsWith(validAddress, true, CultureInfo.InvariantCulture));
            Assert.IsTrue(requestedAddress.StartsWith("0x", true, CultureInfo.InvariantCulture));
        });

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator NullEthSignatureShouldResultInError() => UniTask.ToCoroutine(async () =>
        {
            var ethSignerMock = new EthSignerMock { Address = ValidAddresses[0], TypedData = "", Signature = null };
            var webClientMock = new ElympicsMockWebClient();
            ElympicsWebClient.Instance = webClientMock;
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressNonce, _ => new EthAddressNonceResponse { nonce = Guid.Empty.ToString() });

            var result = await _sut.AuthenticateWithEthAddress(ethSignerMock);

            Assert.IsTrue(result.IsFailure);
            Assert.IsTrue(result.Error.Contains("null"));
        });

        public static string[] InvalidSignatures = { "", "xyz", "-1", new(Enumerable.Repeat('a', 131).ToArray()), "0x" + new string(Enumerable.Repeat('b', 131).ToArray()) };

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator InvalidEthSignatureFormatShouldResultInError([ValueSource(nameof(InvalidSignatures))] string invalidSignature) => UniTask.ToCoroutine(async () =>
        {
            var ethSignerMock = new EthSignerMock
            {
                Address = ValidAddresses[0],
                TypedData = "",
                Signature = invalidSignature,
            };
            var webClientMock = new ElympicsMockWebClient();
            ElympicsWebClient.Instance = webClientMock;
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressNonce, _ => new EthAddressNonceResponse { nonce = Guid.Empty.ToString() });

            var result = await _sut.AuthenticateWithEthAddress(ethSignerMock);

            Assert.IsTrue(result.IsFailure);
            Assert.IsTrue(result.Error.Contains("format"));
        });

        public static string[] ValidSignatures = { new(Enumerable.Repeat('a', 130).ToArray()), "0x" + new string(Enumerable.Repeat('b', 130).ToArray()) };

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator ValidEthSignatureFormatShouldResultInAuthRequest([ValueSource(nameof(ValidSignatures))] string validSignature) => UniTask.ToCoroutine(async () =>
        {
            var ethSignerMock = new EthSignerMock
            {
                Address = ValidAddresses[0],
                TypedData = "",
                Signature = validSignature,
            };
            var webClientMock = new ElympicsMockWebClient();
            ElympicsWebClient.Instance = webClientMock;
            EthAddressAuthRequest? request = null;
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressNonce, _ => new EthAddressNonceResponse { nonce = Guid.Empty.ToString() });
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressAuth,
                reqParams =>
                {
                    request = reqParams.JsonBody as EthAddressAuthRequest?;
                    throw new Exception(nameof(ValidEthSignatureFormatShouldResultInAuthRequest));
                });

            var result = await _sut.AuthenticateWithEthAddress(ethSignerMock);

            Assert.IsTrue(result.IsFailure);
            Assert.IsTrue(result.Error.Contains(nameof(ValidEthSignatureFormatShouldResultInAuthRequest)));

            Assert.IsTrue(request.HasValue);
            var requestSignature = request!.Value.signature;
            Assert.AreEqual(132, requestSignature.Length);
            Assert.IsTrue(requestSignature.EndsWith(ethSignerMock.Signature, true, CultureInfo.InvariantCulture));
            Assert.IsTrue(requestSignature.StartsWith("0x", true, CultureInfo.InvariantCulture));
        });

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator AuthRequestShouldContainMessageFromEthSigner() => UniTask.ToCoroutine(async () =>
        {
            var ethSignerMock = new EthSignerMock
            {
                Address = ValidAddresses[0],
                TypedData = "Hello world!",
                Signature = ValidSignatures[0],
            };
            var webClientMock = new ElympicsMockWebClient();
            ElympicsWebClient.Instance = webClientMock;
            EthAddressAuthRequest? request = null;
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressNonce, _ => new EthAddressNonceResponse { nonce = Guid.Empty.ToString() });
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressAuth,
                reqParams =>
                {
                    request = reqParams.JsonBody as EthAddressAuthRequest?;
                    throw new Exception(nameof(AuthRequestShouldContainMessageFromEthSigner));
                });

            var result = await _sut.AuthenticateWithEthAddress(ethSignerMock);

            Assert.IsTrue(result.IsFailure);
            Assert.IsTrue(result.Error.Contains(nameof(AuthRequestShouldContainMessageFromEthSigner)));

            Assert.IsTrue(request.HasValue);
            var requestTypedData = request!.Value.typedData;
            Assert.AreEqual(ethSignerMock.TypedData, requestTypedData);
        });

        [UnityTest, Timeout(TimeoutMsPerTest)]
        public IEnumerator ValidEthDataShouldResultInAuthDataBeingReturned() => UniTask.ToCoroutine(async () =>
        {
            var ethSignerMock = new EthSignerMock
            {
                Address = ValidAddresses[0],
                TypedData = "",
                Signature = ValidSignatures[0],
            };
            var webClientMock = new ElympicsMockWebClient();
            ElympicsWebClient.Instance = webClientMock;
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressNonce, _ => new EthAddressNonceResponse { nonce = Guid.Empty.ToString() });
            var expectedAuthData = new AuthData(Guid.Empty, "", "", AuthType.EthAddress);
            webClientMock.AddHandler("/" + AuthRoutes.EthAddressAuth,
                _ => new AuthenticationDataResponse
                {
                    jwtToken = expectedAuthData.JwtToken,
                    userId = expectedAuthData.UserId.ToString(),
                });

            var result = await _sut.AuthenticateWithEthAddress(ethSignerMock);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(expectedAuthData.JwtToken, result.Value.JwtToken);
            Assert.AreEqual(expectedAuthData.UserId, result.Value.UserId);
            Assert.AreEqual(expectedAuthData.AuthType, result.Value.AuthType);
        });
    }
}
