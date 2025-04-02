using Microsoft.Extensions.Logging;
using Moq;
using NSign.Signatures;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.BouncyCastle.Providers
{
    public sealed class EdDsaEdwards25519SignatureProviderTests
    {
        private readonly Random rng = new Random();

        private readonly Ed25519PrivateKeyParameters privateKeyFromStandard = GetPrivateKeyFromStandard();
        private readonly Ed25519PublicKeyParameters publicKeyFromStandard = GetPublicKeyFromStandard();
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>(MockBehavior.Loose);
        private readonly TestMessageContext messageContext;

        public EdDsaEdwards25519SignatureProviderTests()
        {
            messageContext = new TestMessageContext(mockLogger.Object);
        }

        [Fact]
        public void CtorValidatesInput()
        {
            ArgumentNullException ex;

            ex = Assert.Throws<ArgumentNullException>(
                () => new EdDsaEdwards25519SignatureProvider(null!, null));
            Assert.Equal("publicKey", ex.ParamName);
        }

        [Fact]
        public async Task SignAsyncThrowsWhenPrivateKeyMissing()
        {
            (_, Ed25519PublicKeyParameters publicKey) =
                GetKeys("ed25519.nsign.test.local");
            EdDsaEdwards25519SignatureProvider provider = new EdDsaEdwards25519SignatureProvider(publicKey, "test-key-id");
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.SignAsync(new byte[] { }, default));

            Assert.Equal("Cannot sign without a private key. Please make sure the provider is created with a valid private key.", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("my-key-id")]
        public async Task OwnSignatureCanBeVerified(string? keyId)
        {
            (Ed25519PrivateKeyParameters? privateKey, Ed25519PublicKeyParameters publicKey) =
                GetKeys("ed25519.nsign.test.local");
            EdDsaEdwards25519SignatureProvider signingProvider = new EdDsaEdwards25519SignatureProvider(privateKey, publicKey, keyId);
            EdDsaEdwards25519SignatureProvider verifyingProvider = new EdDsaEdwards25519SignatureProvider(publicKey, keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();
            byte[] random = new byte[2048];

            rng.NextBytes(random);
            ReadOnlyMemory<byte> signature = await signingProvider.SignAsync(random, CancellationToken.None);

            VerificationResult result = await verifyingProvider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);

            // With the keyId set:
            signatureParams.WithKeyId(keyId!);

            result = await verifyingProvider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Theory]
        [InlineData("my-key-id")]
        public async Task VerificationFailsForDifferentKeyId(string keyId)
        {
            (Ed25519PrivateKeyParameters? privateKey, Ed25519PublicKeyParameters publicKey) =
                GetKeys("ed25519.nsign.test.local");
            EdDsaEdwards25519SignatureProvider signingProvider = new EdDsaEdwards25519SignatureProvider(privateKey, publicKey, keyId);
            EdDsaEdwards25519SignatureProvider verifyingProvider = new EdDsaEdwards25519SignatureProvider(publicKey, keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent().WithKeyId("unknown-key");
            byte[] random = new byte[2048];

            rng.NextBytes(random);
            ReadOnlyMemory<byte> signature = await signingProvider.SignAsync(random, CancellationToken.None);

            VerificationResult result = await verifyingProvider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.NoMatchingVerifierFound, result);
        }

        [Theory]
        [InlineData("my-key-id")]
        public async Task VerificationFailsForDifferentAlgorithm(string keyId)
        {
            (Ed25519PrivateKeyParameters? privateKey, Ed25519PublicKeyParameters publicKey) =
                GetKeys("ed25519.nsign.test.local");
            EdDsaEdwards25519SignatureProvider signingProvider = new EdDsaEdwards25519SignatureProvider(privateKey, publicKey, keyId);
            EdDsaEdwards25519SignatureProvider verifyingProvider = new EdDsaEdwards25519SignatureProvider(publicKey, keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent().WithAlgorithm(SignatureAlgorithm.HmacSha256);
            byte[] random = new byte[2048];

            rng.NextBytes(random);
            ReadOnlyMemory<byte> signature = await signingProvider.SignAsync(random, CancellationToken.None);

            VerificationResult result = await verifyingProvider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.NoMatchingVerifierFound, result);
        }

        [Fact]
        public async Task UpdateSignatureParamsSetsTheAlgorithm()
        {
            (_, Ed25519PublicKeyParameters publicKey) =
                GetKeys("ed25519.nsign.test.local");
            EdDsaEdwards25519SignatureProvider signingProvider = new EdDsaEdwards25519SignatureProvider(publicKey, "some-test-key");
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            Assert.Null(signatureParams.Algorithm);
            await signingProvider.UpdateSignatureParamsAsync(signatureParams, this.messageContext, CancellationToken.None);
            Assert.Equal("ed25519", signatureParams.Algorithm);
        }

        #region From Standard

        [Fact]
        public async Task Draft19_B_2_6_Sign()
        {
            EdDsaEdwards25519SignatureProvider provider = new EdDsaEdwards25519SignatureProvider(privateKeyFromStandard, publicKeyFromStandard, "test-key-ed25519");

            string rawSigParams = "(\"date\" \"@method\" \"@path\" \"@authority\" \"content-type\" \"content-length\")" +
               ";created=1618884473;keyid=\"test-key-ed25519\"";
            string input =
                "\"date\": Tue, 20 Apr 2021 02:07:55 GMT\n" +
                "\"@method\": POST\n" +
                "\"@path\": /foo\n" +
                "\"@authority\": example.com\n" +
                "\"content-type\": application/json\n" +
                "\"content-length\": 18\n" +
                "\"@signature-params\": " + rawSigParams;
            SignatureParamsComponent sigParams = new SignatureParamsComponent(rawSigParams);

            byte[] rawInput = Encoding.ASCII.GetBytes(input);
            ReadOnlyMemory<byte> rawSignature = await provider.SignAsync(rawInput, default);

            string signatureBase64 = Convert.ToBase64String(rawSignature.Span);
            Assert.Equal("wqcAqbmYJ2ji2glfAMaRy4gruYYnx2nEFN2HN6jrnDnQCK1u02Gb04v9EDgwUPiu4A0w6vuQv5lIp5WPpBKRCw==", signatureBase64);
        }

        [Fact]
        public async Task Draft19_B_2_6_Verify()
        {
            EdDsaEdwards25519SignatureProvider provider = new EdDsaEdwards25519SignatureProvider(null, publicKeyFromStandard, "test-key-ed25519");

            string rawSigParams = "(\"date\" \"@method\" \"@path\" \"@authority\" \"content-type\" \"content-length\")" +
                ";created=1618884473;keyid=\"test-key-ed25519\"";
            string input =
                "\"date\": Tue, 20 Apr 2021 02:07:55 GMT\n" +
                "\"@method\": POST\n" +
                "\"@path\": /foo\n" +
                "\"@authority\": example.com\n" +
                "\"content-type\": application/json\n" +
                "\"content-length\": 18\n" +
                "\"@signature-params\": " + rawSigParams;
            SignatureParamsComponent sigParams = new SignatureParamsComponent(rawSigParams);

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("wqcAqbmYJ2ji2glfAMaRy4gruYYnx2nEFN2HN6jrnDnQCK1u02Gb04v9EDgwUPiu4A0w6vuQv5lIp5WPpBKRCw=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Fact]
        public async Task Draft19_B_4_Sign()
        {
            EdDsaEdwards25519SignatureProvider provider = new EdDsaEdwards25519SignatureProvider(privateKeyFromStandard, publicKeyFromStandard, "test-key-ed25519");

            string rawSigParams = "(\"@method\" \"@path\" \"@authority\" \"accept\");created=1618884473;keyid=\"test-key-ed25519\"";
            string input =
                "\"@method\": GET\n" +
                "\"@path\": /demo\n" +
                "\"@authority\": example.org\n" +
                "\"accept\": application/json, */*\n" +
                "\"@signature-params\": " + rawSigParams;
            SignatureParamsComponent sigParams = new SignatureParamsComponent(rawSigParams);

            byte[] rawInput = Encoding.ASCII.GetBytes(input);
            ReadOnlyMemory<byte> rawSignature = await provider.SignAsync(rawInput, default);

            string signatureBase64 = Convert.ToBase64String(rawSignature.Span);
            Assert.Equal("ZT1kooQsEHpZ0I1IjCqtQppOmIqlJPeo7DHR3SoMn0s5JZ1eRGS0A+vyYP9t/LXlh5QMFFQ6cpLt2m0pmj3NDA==", signatureBase64);
        }

        [Fact]
        public async Task Draft19_B_4_Verify()
        {
            EdDsaEdwards25519SignatureProvider provider = new EdDsaEdwards25519SignatureProvider(null, publicKeyFromStandard, "test-key-ed25519");

            string rawSigParams = "(\"@method\" \"@path\" \"@authority\" \"accept\");created=1618884473;keyid=\"test-key-ed25519\"";
            string input =
                "\"@method\": GET\n" +
                "\"@path\": /demo\n" +
                "\"@authority\": example.org\n" +
                "\"accept\": application/json, */*\n" +
                "\"@signature-params\": " + rawSigParams;
            SignatureParamsComponent sigParams = new SignatureParamsComponent(rawSigParams);

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("ZT1kooQsEHpZ0I1IjCqtQppOmIqlJPeo7DHR3SoMn0s5JZ1eRGS0A+vyYP9t/LXlh5QMFFQ6cpLt2m0pmj3NDA=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        #endregion

        private static Ed25519PrivateKeyParameters GetPrivateKeyFromStandard()
        {
            using PemReader reader = new PemReader(new StringReader(@"
-----BEGIN PRIVATE KEY-----
MC4CAQAwBQYDK2VwBCIEIJ+DYvh6SEqVTm50DFtMDoQikTmiCqirVv9mWG9qfSnF
-----END PRIVATE KEY-----"));

            return (Ed25519PrivateKeyParameters)reader.ReadObject();
        }

        private static Ed25519PublicKeyParameters GetPublicKeyFromStandard()
        {
            using PemReader reader = new PemReader(new StringReader(@"
-----BEGIN PUBLIC KEY-----
MCowBQYDK2VwAyEAJrQLj5P/89iXES9+vFgrIy29clF9CC/oPPsw3c5D0bs=
-----END PUBLIC KEY-----"));

            return (Ed25519PublicKeyParameters)reader.ReadObject();
        }

        private (Ed25519PrivateKeyParameters? privateKey, Ed25519PublicKeyParameters publicKey) GetKeys(string pemBasePath)
        {
            Ed25519PrivateKeyParameters priv;
            Ed25519PublicKeyParameters pub;

            {
                using StreamReader streamReader = new StreamReader($"{pemBasePath}-priv.pem");
                using PemReader reader = new PemReader(streamReader);
                priv = (Ed25519PrivateKeyParameters)reader.ReadObject();
            }
            {
                using StreamReader streamReader = new StreamReader($"{pemBasePath}-pub.pem");
                using PemReader reader = new PemReader(streamReader);
                pub = (Ed25519PublicKeyParameters)reader.ReadObject();
            }

            return (priv, pub);
        }
    }
}
