using NSign.Signatures;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Providers
{
    public sealed class ECDsaP256Sha256SignatureProviderTests
    {
        private readonly Random rng = new Random();

        [Theory]
        [InlineData(
            "ecdsa-p192-nsign.test.local",
            "A certificate with elliptic curve P-256 (oid: 1.2.840.10045.3.1.7) is expected, but curve 'nistP192' (oid: ) was provided.")]
        [InlineData(
            "ecdsa-p384-nsign.test.local",
            "A certificate with elliptic curve P-256 (oid: 1.2.840.10045.3.1.7) is expected, but curve 'nistP384' (oid: 1.3.132.0.34) was provided.")]
        [InlineData("rsa-nsign.test.local", "The certificate does not use elliptic curve keys.")]
        public void CtorFailsForNonP256Curve(string cert, string expectedMessage)
        {
            ArgumentException ex;
            ex = Assert.Throws<ArgumentException>(() => Make(true, certName: cert));
            Assert.Equal("certificate", ex.ParamName);
            Assert.Equal($"{expectedMessage} (Parameter 'certificate')", ex.Message);

            ex = Assert.Throws<ArgumentException>(() => Make(false, certName: cert));
            Assert.Equal("certificate", ex.ParamName);
            Assert.Equal($"{expectedMessage} (Parameter 'certificate')", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("my-key-id")]
        public async Task OwnSignatureCanBeVerified(string keyId)
        {
            ECDsaP256Sha256SignatureProvider signingProvider = Make(true, keyId);
            ECDsaP256Sha256SignatureProvider verifyingProvider = Make(false, keyId);
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
            ECDsaP256Sha256SignatureProvider signingProvider = Make(true, keyId);
            ECDsaP256Sha256SignatureProvider verifyingProvider = Make(false, keyId);
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
            ECDsaP256Sha256SignatureProvider signingProvider = Make(true, keyId);
            ECDsaP256Sha256SignatureProvider verifyingProvider = Make(false, keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent().WithAlgorithm(SignatureAlgorithm.HmacSha256);
            byte[] random = new byte[2048];

            rng.NextBytes(random);
            byte[] signature = await signingProvider.SignAsync(random, CancellationToken.None);

            VerificationResult result = await verifyingProvider.VerifyAsync(signatureParams, random, signature, CancellationToken.None);
            Assert.Equal(VerificationResult.NoMatchingVerifierFound, result);
        }

        [Fact]
        public void UpdateSignatureParamsSetsTheAlgorithm()
        {
            ECDsaP256Sha256SignatureProvider signingProvider = Make(true);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            Assert.Null(signatureParams.Algorithm);
            signingProvider.UpdateSignatureParams(signatureParams);
            Assert.Equal("ecdsa-p256-sha256", signatureParams.Algorithm);
        }

        private static ECDsaP256Sha256SignatureProvider Make(bool forSigning = false, string keyId = null, string certName = "ecdsa-p256-nsign.test.local")
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

            return new ECDsaP256Sha256SignatureProvider(cert, keyId ?? cert.Thumbprint);
        }
    }
}
