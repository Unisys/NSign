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

        [Theory]
        [InlineData(null)]
        [InlineData("my-key-id")]
        public async Task OwnSignatureCanBeVerified(string keyId)
        {
            HmacSha256SignatureProvider provider = Make(keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();
            byte[] random = new byte[2048];

            rng.NextBytes(random);
            byte[] signature = await provider.SignAsync(random, CancellationToken.None);

            VerificationResult result = await provider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);

            // With the keyId set:
            signatureParams.WithKeyId(keyId);

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
            byte[] signature = await provider.SignAsync(random, CancellationToken.None);

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
            byte[] signature = await provider.SignAsync(random, CancellationToken.None);

            VerificationResult result = await provider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.NoMatchingVerifierFound, result);
        }

        [Fact]
        public void UpdateSignatureParamsSetsTheAlgorithm()
        {
            HmacSha256SignatureProvider provider = Make();
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            Assert.Null(signatureParams.Algorithm);
            provider.UpdateSignatureParams(signatureParams);
            Assert.Equal("hmac-sha256", signatureParams.Algorithm);
        }

        private static HmacSha256SignatureProvider Make()
        {
            return new HmacSha256SignatureProvider(Encoding.ASCII.GetBytes("mykey"));
        }

        private static HmacSha256SignatureProvider Make(string keyId = null)
        {
            return new HmacSha256SignatureProvider(Encoding.ASCII.GetBytes("mykey"), keyId);
        }
    }
}
