using NSign.Signatures;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Providers
{
    public sealed class RsaPkcs15Sha256SignatureProviderTests
    {
        private readonly Random rng = new Random();

        #region From standard

        [Fact]
        public async Task Standard_4_3_Sign()
        {
            RsaPkcs15Sha256SignatureProvider provider = Make(true, "test-key-rsa", "test-key-rsa");

            string input =
                "\"signature\";key=\"sig1\": :LAH8BjcfcOcLojiuOBFWn0P5keD3xAOuJRGziCLuD8r5MW9S0RoXXLzLSRfGY/3SF8kVIkHjE13SEFdTo4Af/fJ/Pu9wheqoLVdwXyY/UkBIS1M8Brc8IODsn5DFIrG0IrburbLi0uCc+E2ZIIb6HbUJ+o+jP58JelMTe0QE3IpWINTEzpxjqDf5/Df+InHCAkQCTuKsamjWXUpyOT1Wkxi7YPVNOjW4MfNuTZ9HdbD2Tr65+BXeTG9ZS/9SWuXAc+BZ8WyPz0QRz//ec3uWXd7bYYODSjRAxHqX+S1ag3LZElYyUKaAIjZ8MGOt4gXEwCSLDv/zqxZeWLj/PDkn6w==:\n" +
                "\"forwarded\": for=192.0.2.123\n" +
                "\"@signature-params\": (\"signature\";key=\"sig1\" \"forwarded\");created=1618884480;expires=1618884540;keyid=\"test-key-rsa\";alg=\"rsa-v1_5-sha256\"";

            byte[] signature = await provider.SignAsync(Encoding.ASCII.GetBytes(input), default);
            string sigBase64 = Convert.ToBase64String(signature);
            Assert.Equal(
                "G1WLTL4/9PGSKEQbSAMypZNk+I2dpLJ6qvl2JISahlP31OO/QEUd8/HdO2O7vYLi5k3JIiAK3UPK4U+kvJZyIUidsiXlzRI+Y2se3SGo0D8dLfhG95bKr6ukYXl60QHpsGRTfSiwdtvYKXGpKNrMlISJYd+oGrGRyI9gbCy0aFhc6I/okIMLeK4g9PgzpC3YTwhUQ98KIBNLWHgREfBgJxjPbxFlsgJ9ykPviLj8GKJ81HwsK3XM9P7WaS7fMGOt8h1kSqgkZQB9YqiIo+WhHvJa7iPy8QrYFKzx9BBEY6AwfStZAsXXz3LobZseyxsYcLJLs8rY0wVA9NPsxKrHGA==",
                sigBase64);
        }

        [Fact]
        public async Task Standard_4_3_Verify()
        {
            RsaPkcs15Sha256SignatureProvider provider = Make(false, "test-key-rsa", "test-key-rsa");

            string input =
                "\"signature\";key=\"sig1\": :LAH8BjcfcOcLojiuOBFWn0P5keD3xAOuJRGziCLuD8r5MW9S0RoXXLzLSRfGY/3SF8kVIkHjE13SEFdTo4Af/fJ/Pu9wheqoLVdwXyY/UkBIS1M8Brc8IODsn5DFIrG0IrburbLi0uCc+E2ZIIb6HbUJ+o+jP58JelMTe0QE3IpWINTEzpxjqDf5/Df+InHCAkQCTuKsamjWXUpyOT1Wkxi7YPVNOjW4MfNuTZ9HdbD2Tr65+BXeTG9ZS/9SWuXAc+BZ8WyPz0QRz//ec3uWXd7bYYODSjRAxHqX+S1ag3LZElYyUKaAIjZ8MGOt4gXEwCSLDv/zqxZeWLj/PDkn6w==:\n" +
                "\"forwarded\": for=192.0.2.123\n" +
                "\"@signature-params\": (\"signature\";key=\"sig1\" \"forwarded\");created=1618884480;expires=1618884540;keyid=\"test-key-rsa\";alg=\"rsa-v1_5-sha256\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"signature\";key=\"sig1\" \"forwarded\");created=1618884480;expires=1618884540;keyid=\"test-key-rsa\";alg=\"rsa-v1_5-sha256\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("G1WLTL4/9PGSKEQbSAMypZNk+I2dpLJ6qvl2JISahlP31OO/QEUd8/HdO2O7vYLi5k3JIiAK3UPK4U+kvJZyIUidsiXlzRI+Y2se3SGo0D8dLfhG95bKr6ukYXl60QHpsGRTfSiwdtvYKXGpKNrMlISJYd+oGrGRyI9gbCy0aFhc6I/okIMLeK4g9PgzpC3YTwhUQ98KIBNLWHgREfBgJxjPbxFlsgJ9ykPviLj8GKJ81HwsK3XM9P7WaS7fMGOt8h1kSqgkZQB9YqiIo+WhHvJa7iPy8QrYFKzx9BBEY6AwfStZAsXXz3LobZseyxsYcLJLs8rY0wVA9NPsxKrHGA=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        #endregion

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

        private static RsaPkcs15Sha256SignatureProvider Make(
            bool forSigning = false,
            string keyId = null,
            string certName = "rsa-nsign.test.local")
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

            return new RsaPkcs15Sha256SignatureProvider(cert, keyId ?? cert.Thumbprint);
        }
    }
}
