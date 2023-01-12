using NSign.Signatures;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Providers
{
    public sealed class ECDsaP382Sha384SignatureProviderTests
    {
        private readonly Random rng = new Random();

        [Theory]
        [InlineData("ecdsa-p192-nsign.test.local", "P192")]
        [InlineData("ecdsa-p256-nsign.test.local", "P256")]
        [InlineData("rsa-nsign.test.local", "The certificate does not use elliptic curve keys.")]
        public void CtorFailsForNonP384Curve(string cert, string expectedCurve)
        {
            ArgumentException ex;
            ex = Assert.Throws<ArgumentException>(() => Make(true, certName: cert));
            Assert.Equal("certificate", ex.ParamName);
            Assert.Contains(expectedCurve, ex.Message);

            ex = Assert.Throws<ArgumentException>(() => Make(false, certName: cert));
            Assert.Equal("certificate", ex.ParamName);
            Assert.Contains(expectedCurve, ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("my-key-id")]
        public async Task OwnSignatureCanBeVerified(string keyId)
        {
            ECDsaP382Sha384SignatureProvider signingProvider = Make(true, keyId);
            ECDsaP382Sha384SignatureProvider verifyingProvider = Make(false, keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();
            byte[] random = new byte[2048];

            rng.NextBytes(random);
            ReadOnlyMemory<byte> signature = await signingProvider.SignAsync(random, CancellationToken.None);

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
            ECDsaP382Sha384SignatureProvider signingProvider = Make(true, keyId);
            ECDsaP382Sha384SignatureProvider verifyingProvider = Make(false, keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent()
                .WithKeyId("VerificationFailsForDifferentKeyId");
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
            ECDsaP382Sha384SignatureProvider signingProvider = Make(true, keyId);
            ECDsaP382Sha384SignatureProvider verifyingProvider = Make(false, keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent().WithAlgorithm(SignatureAlgorithm.HmacSha256);
            byte[] random = new byte[2048];

            rng.NextBytes(random);
            ReadOnlyMemory<byte> signature = await signingProvider.SignAsync(random, CancellationToken.None);

            VerificationResult result = await verifyingProvider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.NoMatchingVerifierFound, result);
        }

        [Fact]
        public void UpdateSignatureParamsSetsTheAlgorithm()
        {
            ECDsaP382Sha384SignatureProvider signingProvider = Make(true);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            Assert.Null(signatureParams.Algorithm);
            signingProvider.UpdateSignatureParams(signatureParams);
            Assert.Equal("ecdsa-p384-sha384", signatureParams.Algorithm);
        }

        private static ECDsaP382Sha384SignatureProvider Make(
            bool forSigning = false,
            string? keyId = null,
            string certName = "ecdsa-p384-nsign.test.local")
        {
            X509Certificate2 cert;

            if (forSigning)
            {
                cert = Certificates.GetCertificateWithPrivateKey($"{certName}.pfx", null);
            }
            else
            {
                cert = Certificates.GetCertificate($"{certName}.cer");
            }

            return new ECDsaP382Sha384SignatureProvider(cert, keyId ?? cert.Thumbprint);
        }
    }
}
