using NSign.Signatures;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Providers
{
    public sealed class RsaPkcs15Sha256SignatureProviderTests
    {
        private readonly Random rng = new Random();

        [Theory]
        [InlineData(null)]
        [InlineData("my-key-id")]
        public async Task OwnSignatureCanBeVerified(string keyId)
        {
            RsaPkcs15Sha256SignatureProvider signingProvider = Make(true, keyId);
            RsaPkcs15Sha256SignatureProvider verifyingProvider = Make(false, keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();
            byte[] random = new byte[2048];

            rng.NextBytes(random);
            byte[] signature = await signingProvider.SignAsync(random, CancellationToken.None);

            VerificationResult result = await verifyingProvider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);

            // With the keyId set:
            signatureParams.WithKeyId(keyId);

            result = await verifyingProvider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Theory]
        [InlineData("my-key-id")]
        public async Task VerificationFailsForDifferentKeyId(string keyId)
        {
            RsaPkcs15Sha256SignatureProvider signingProvider = Make(true, keyId);
            RsaPkcs15Sha256SignatureProvider verifyingProvider = Make(false, keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent()
                .WithKeyId("VerificationFailsForDifferentKeyId");
            byte[] random = new byte[2048];

            rng.NextBytes(random);
            byte[] signature = await signingProvider.SignAsync(random, CancellationToken.None);

            VerificationResult result = await verifyingProvider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.NoMatchingVerifierFound, result);
        }

        [Theory]
        [InlineData("my-key-id")]
        public async Task VerificationFailsForDifferentAlgorithm(string keyId)
        {
            RsaPkcs15Sha256SignatureProvider signingProvider = Make(true, keyId);
            RsaPkcs15Sha256SignatureProvider verifyingProvider = Make(false, keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent().WithAlgorithm(SignatureAlgorithm.RsaPssSha512);
            byte[] random = new byte[2048];

            rng.NextBytes(random);
            byte[] signature = await signingProvider.SignAsync(random, CancellationToken.None);

            VerificationResult result = await verifyingProvider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.NoMatchingVerifierFound, result);
        }

        [Fact]
        public void UpdateSignatureParamsSetsTheAlgorithm()
        {
            RsaPkcs15Sha256SignatureProvider signingProvider = Make(true);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            Assert.Null(signatureParams.Algorithm);
            signingProvider.UpdateSignatureParams(signatureParams);
            Assert.Equal("rsa-v1_5-sha256", signatureParams.Algorithm);
        }

        private static RsaPkcs15Sha256SignatureProvider Make(bool forSigning = false, string keyId = null)
        {
            X509Certificate2 cert;

            if (forSigning)
            {
                cert = Certificates.GetCertificateWithPrivateKey("rsa-nsign.test.local.pfx", null);
            }
            else
            {
                cert = Certificates.GetCertificate("rsa-nsign.test.local.cer");
            }

            return new RsaPkcs15Sha256SignatureProvider(cert, keyId ?? cert.Thumbprint);
        }
    }
}
