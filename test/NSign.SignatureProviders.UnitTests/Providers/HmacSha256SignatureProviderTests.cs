using Microsoft.Extensions.Logging;
using Moq;
using NSign.Signatures;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Providers
{
    public sealed class HmacSha256SignatureProviderTests
    {
        private readonly Random rng = new Random();
        private static readonly byte[] defaultKey = Encoding.ASCII.GetBytes("mykey");
        private static readonly byte[] sharedKeyFromStandard =
            Convert.FromBase64String("uzvJfB4u3N0Jy4T7NZ75MDVcr8zSTInedJtkgcu46YW4XByzNJjxBdtjUkdJPBtbmHhIDi6pcl8jsasjlTMtDQ==");
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>(MockBehavior.Loose);
        private readonly TestMessageContext messageContext;

        public HmacSha256SignatureProviderTests()
        {
            messageContext = new TestMessageContext(mockLogger.Object);
        }

        #region From standard

        [Fact]
        public async Task Standard_B_2_5_Sign()
        {
            HmacSha256SignatureProvider provider = Make("test-shared-secret", sharedKeyFromStandard);

            string input =
                "\"date\": Tue, 20 Apr 2021 02:07:55 GMT\n" +
                "\"@authority\": example.com\n" +
                "\"content-type\": application/json\n" +
                "\"@signature-params\": (\"date\" \"@authority\" \"content-type\");created=1618884473;keyid=\"test-shared-secret\"";

            ReadOnlyMemory<byte> signature = await provider.SignAsync(Encoding.ASCII.GetBytes(input), default);
            string sigBase64 = Convert.ToBase64String(signature.Span);
            Assert.Equal("pxcQw6G3AjtMBQjwo8XzkZf/bws5LelbaMk5rGIGtE8=", sigBase64);
        }

        [Fact]
        public async Task Standard_B_2_5_Verify()
        {
            HmacSha256SignatureProvider provider = Make("test-shared-secret", sharedKeyFromStandard);

            string input =
                "\"date\": Tue, 20 Apr 2021 02:07:55 GMT\n" +
                "\"@authority\": example.com\n" +
                "\"content-type\": application/json\n" +
                "\"@signature-params\": (\"date\" \"@authority\" \"content-type\");created=1618884473;keyid=\"test-shared-secret\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"date\" \"@authority\" \"content-type\");created=1618884473;keyid=\"test-shared-secret\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("pxcQw6G3AjtMBQjwo8XzkZf/bws5LelbaMk5rGIGtE8="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        #endregion

        [Theory]
        [InlineData(null)]
        [InlineData("my-key-id")]
        public async Task OwnSignatureCanBeVerified(string? keyId)
        {
            HmacSha256SignatureProvider provider = Make(keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();
            byte[] random = new byte[2048];

            rng.NextBytes(random);
            ReadOnlyMemory<byte> signature = await provider.SignAsync(random, CancellationToken.None);

            VerificationResult result = await provider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);

            // With the keyId set:
            signatureParams.WithKeyId(keyId!);

            result = await provider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Theory]
        [InlineData("my-key-id")]
        public async Task VerificationFailsForDifferentKeyId(string keyId)
        {
            HmacSha256SignatureProvider provider = Make(keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent()
                .WithKeyId("VerificationFailsForDifferentKeyId");
            byte[] random = new byte[2048];

            rng.NextBytes(random);
            ReadOnlyMemory<byte> signature = await provider.SignAsync(random, CancellationToken.None);

            VerificationResult result = await provider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.NoMatchingVerifierFound, result);
        }

        [Theory]
        [InlineData("my-key-id")]
        public async Task VerificationFailsForDifferentAlgorithm(string keyId)
        {
            HmacSha256SignatureProvider provider = Make(keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent().WithAlgorithm(SignatureAlgorithm.RsaPssSha512);
            byte[] random = new byte[2048];

            rng.NextBytes(random);
            ReadOnlyMemory<byte> signature = await provider.SignAsync(random, CancellationToken.None);

            VerificationResult result = await provider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.NoMatchingVerifierFound, result);
        }

        [Fact]
        public async Task UpdateSignatureParamsSetsTheAlgorithm()
        {
            HmacSha256SignatureProvider provider = Make();
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            Assert.Null(signatureParams.Algorithm);
            await provider.UpdateSignatureParamsAsync(signatureParams, this.messageContext, CancellationToken.None);
            Assert.Equal("hmac-sha256", signatureParams.Algorithm);
        }

        private static HmacSha256SignatureProvider Make()
        {
            return new HmacSha256SignatureProvider(Encoding.ASCII.GetBytes("mykey"));
        }

        private static HmacSha256SignatureProvider Make(string? keyId = null, byte[]? keyBytes = null)
        {
            if (null == keyBytes)
            {
                keyBytes = defaultKey;
            }

            return new HmacSha256SignatureProvider(keyBytes, keyId);
        }
    }
}
