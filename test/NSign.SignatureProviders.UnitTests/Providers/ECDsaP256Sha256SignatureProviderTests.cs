using NSign.Signatures;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Providers
{
    public sealed class ECDsaP256Sha256SignatureProviderTests
    {
        private readonly Random rng = new Random();

        #region From standard

        [Fact]
        public async Task Standard_2_2_11_Verify()
        {
            ECDsaP256Sha256SignatureProvider provider = Make(false, "test-key-ecc-p256", "test-key-ecc-p256");

            string input =
                "\"@status\": 503\n" +
                "\"content-length\": 62\n" +
                "\"content-type\": application/json\n" +
                "\"@request-response\";key=\"sig1\": :LAH8BjcfcOcLojiuOBFWn0P5keD3xAOuJRGziCLuD8r5MW9S0RoXXLzLSRfGY/3SF8kVIkHjE13SEFdTo4Af/fJ/Pu9wheqoLVdwXyY/UkBIS1M8Brc8IODsn5DFIrG0IrburbLi0uCc+E2ZIIb6HbUJ+o+jP58JelMTe0QE3IpWINTEzpxjqDf5/Df+InHCAkQCTuKsamjWXUpyOT1Wkxi7YPVNOjW4MfNuTZ9HdbD2Tr65+BXeTG9ZS/9SWuXAc+BZ8WyPz0QRz//ec3uWXd7bYYODSjRAxHqX+S1ag3LZElYyUKaAIjZ8MGOt4gXEwCSLDv/zqxZeWLj/PDkn6w==:\n" +
                "\"@signature-params\": (\"@status\" \"content-length\" \"content-type\" \"@request-response\";key=\"sig1\");created=1618884479;keyid=\"test-key-ecc-p256\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"@status\" \"content-length\" \"content-type\" \"@request-response\";key=\"sig1\");created=1618884479;keyid=\"test-key-ecc-p256\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("JqzXLIjNd6VWVg/M7enbjWkOgsPmIK9vcoFQEkLD0SXNbFjR6d+olsof1dv7xC7ygF1q0YKjVrbV2QlCpDxrHg=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Fact]
        public async Task Standard_4_3_Verify_OnReverseProxy_Draft16()
        {
            ECDsaP256Sha256SignatureProvider provider = Make(false, "test-key-ecc-p256", "test-key-ecc-p256");

            string input =
                "\"@method\": POST\n" +
                "\"@authority\": example.com\n" +
                "\"@path\": /foo\n" +
                "\"content-digest\": sha-512=:WZDPaVn/7XgHaAy8pmojAkGWoRx2UFChF41A2svX+TaPm+AbwAgBWnrIiYllu7BNNyealdVLvRwEmTHWXvJwew==:\n" +
                "\"content-type\": application/json\n" +
                "\"content-length\": 18\n" +
                "\"@signature-params\": (\"@method\" \"@authority\" \"@path\" \"content-digest\" \"content-type\" \"content-length\");created=1618884475;keyid=\"test-key-ecc-p256\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"@method\" \"@authority\" \"@path\" \"content-digest\" \"content-type\" \"content-length\");created=1618884475;keyid=\"test-key-ecc-p256\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("hNojB+wWw4A7SYF3qK1S01Y4UP5i2JZFYa2WOlMB4Np5iWmJSO0bDe2hrYRbcIWqVAFjuuCBRsB7lYQJkzbb6g=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Fact]
        public async Task Standard_B_2_4_Verify()
        {
            ECDsaP256Sha256SignatureProvider provider = Make(false, "test-key-ecc-p256", "test-key-ecc-p256");

            string input =
                "\"@status\": 200\n" +
                "\"content-type\": application/json\n" +
                "\"content-digest\": sha-512=:JlEy2bfUz7WrWIjc1qV6KVLpdr/7L5/L4h7Sxvh6sNHpDQWDCL+GauFQWcZBvVDhiyOnAQsxzZFYwi0wDH+1pw==:\n" +
                "\"content-length\": 23\n" +
                "\"@signature-params\": (\"@status\" \"content-type\" \"content-digest\" \"content-length\");created=1618884473;keyid=\"test-key-ecc-p256\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"@status\" \"content-type\" \"content-digest\" \"content-length\");created=1618884473;keyid=\"test-key-ecc-p256\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("0Ry6HsvzS5VmA6HlfBYS/fYYeNs7fYuA7s0tAdxfUlPGv0CSVuwrrzBOjcCFHTxVRJ01wjvSzM2BetJauj8dsw=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Fact]
        public async Task Standard_B_3_Verify()
        {
            ECDsaP256Sha256SignatureProvider provider = Make(false, "test-key-ecc-p256", "test-key-ecc-p256");

            string input =
                "\"@path\": /foo\n" +
                "\"@query\": ?param=Value&Pet=dog\n" +
                "\"@method\": POST\n" +
                "\"@authority\": service.internal.example\n" +
                "\"client-cert\": :MIIBqDCCAU6gAwIBAgIBBzAKBggqhkjOPQQDAjA6MRswGQYDVQQKDBJMZXQncyBBdXRoZW50aWNhdGUxGzAZBgNVBAMMEkxBIEludGVybWVkaWF0ZSBDQTAeFw0yMDAxMTQyMjU1MzNaFw0yMTAxMjMyMjU1MzNaMA0xCzAJBgNVBAMMAkJDMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE8YnXXfaUgmnMtOXU/IncWalRhebrXmckC8vdgJ1p5Be5F/3YC8OthxM4+k1M6aEAEFcGzkJiNy6J84y7uzo9M6NyMHAwCQYDVR0TBAIwADAfBgNVHSMEGDAWgBRm3WjLa38lbEYCuiCPct0ZaSED2DAOBgNVHQ8BAf8EBAMCBsAwEwYDVR0lBAwwCgYIKwYBBQUHAwIwHQYDVR0RAQH/BBMwEYEPYmRjQGV4YW1wbGUuY29tMAoGCCqGSM49BAMCA0gAMEUCIBHda/r1vaL6G3VliL4/Di6YK0Q6bMjeSkC3dFCOOB8TAiEAx/kHSB4urmiZ0NX5r5XarmPk0wmuydBVoU4hBVZ1yhk=:\n" +
                "\"@signature-params\": (\"@path\" \"@query\" \"@method\" \"@authority\" \"client-cert\");created=1618884473;keyid=\"test-key-ecc-p256\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"@path\" \"@query\" \"@method\" \"@authority\" \"client-cert\");created=1618884473;keyid=\"test-key-ecc-p256\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("xVMHVpawaAC/0SbHrKRs9i8I3eOs5RtTMGCWXm/9nvZzoHsIg6Mce9315T6xoklyy0yzhD9ah4JHRwMLOgmizw=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Fact]
        public async Task Draft17_2_4_Example1_Verify()
        {
            ECDsaP256Sha256SignatureProvider provider = Make(false, "test-key-ecc-p256", "test-key-ecc-p256");

            string input =
                "\"@status\": 503\n" +
                "\"content-digest\": sha-512=:0Y6iCBzGg5rZtoXS95Ijz03mslf6KAMCloESHObfwnHJDbkkWWQz6PhhU9kxsTbARtY2PTBOzq24uJFpHsMuAg==:\n" +
                "\"content-type\": application/json\n" +
                "\"@authority\";req: origin.host.internal.example\n" +
                "\"@method\";req: POST\n" +
                "\"@path\";req: /foo\n" +
                "\"content-digest\";req: sha-512=:WZDPaVn/7XgHaAy8pmojAkGWoRx2UFChF41A2svX+TaPm+AbwAgBWnrIiYllu7BNNyealdVLvRwEmTHWXvJwew==:\n" +
                "\"@signature-params\": (\"@status\" \"content-digest\" \"content-type\" \"@authority\";req \"@method\";req \"@path\";req \"content-digest\";req);created=1618884479;keyid=\"test-key-ecc-p256\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"@status\" \"content-digest\" \"content-type\" \"@authority\";req \"@method\";req \"@path\";req \"content-digest\";req);created=1618884479;keyid=\"test-key-ecc-p256\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("9MG6AOgykOZTc/h2rnDc/g8L+/aXgdkV4hNDvpCxfbVrmLevWPfyvEC/8jBh+3XnVwBqqcJyhUXoFgWv1SMI7A=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Fact]
        public async Task Draft17_2_4_Example2_Verify()
        {
            ECDsaP256Sha256SignatureProvider provider = Make(false, "test-key-ecc-p256", "test-key-ecc-p256");

            string input =
                "\"@status\": 503\n" +
                "\"content-digest\": sha-512=:0Y6iCBzGg5rZtoXS95Ijz03mslf6KAMCloESHObfwnHJDbkkWWQz6PhhU9kxsTbARtY2PTBOzq24uJFpHsMuAg==:\n" +
                "\"content-type\": application/json\n" +
                "\"@authority\";req: origin.host.internal.example\n" +
                "\"@method\";req: POST\n" +
                "\"@path\";req: /foo\n" +
                "\"@query\";req: ?param=Value&Pet=dog\n" +
                "\"content-digest\";req: sha-512=:WZDPaVn/7XgHaAy8pmojAkGWoRx2UFChF41A2svX+TaPm+AbwAgBWnrIiYllu7BNNyealdVLvRwEmTHWXvJwew==:\n" +
                "\"content-type\";req: application/json\n" +
                "\"content-length\";req: 18\n" +
                "\"@signature-params\": (\"@status\" \"content-digest\" \"content-type\" \"@authority\";req \"@method\";req \"@path\";req \"@query\";req \"content-digest\";req \"content-type\";req \"content-length\";req);created=1618884479;keyid=\"test-key-ecc-p256\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"@status\" \"content-digest\" \"content-type\" \"@authority\";req \"@method\";req \"@path\";req \"@query\";req \"content-digest\";req \"content-type\";req \"content-length\";req);created=1618884479;keyid=\"test-key-ecc-p256\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("zU7zd1MN56WapeNxfVNleCx5rFxBhBcZngnX4d+MurOk3tNu3rFfTFnwhglZH8qNBoygvhVMfQq9wIvLqyVNog=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Fact]
        public async Task Draft19_2_4_Example1_Verify()
        {
            ECDsaP256Sha256SignatureProvider provider = Make(false, "test-key-ecc-p256", "test-key-ecc-p256");

            string input =
                "\"@status\": 503\n" +
                "\"content-digest\": sha-512=:0Y6iCBzGg5rZtoXS95Ijz03mslf6KAMCloESHObfwnHJDbkkWWQz6PhhU9kxsTbARtY2PTBOzq24uJFpHsMuAg==:\n" +
                "\"content-type\": application/json\n" +
                "\"@authority\";req: example.com\n" +
                "\"@method\";req: POST\n" +
                "\"@path\";req: /foo\n" +
                "\"content-digest\";req: sha-512=:WZDPaVn/7XgHaAy8pmojAkGWoRx2UFChF41A2svX+TaPm+AbwAgBWnrIiYllu7BNNyealdVLvRwEmTHWXvJwew==:\n" +
                "\"@signature-params\": (\"@status\" \"content-digest\" \"content-type\" \"@authority\";req \"@method\";req \"@path\";req \"content-digest\";req);created=1618884479;keyid=\"test-key-ecc-p256\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"@status\" \"content-digest\" \"content-type\" \"@authority\";req \"@method\";req \"@path\";req \"content-digest\";req);created=1618884479;keyid=\"test-key-ecc-p256\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("dMT/A/76ehrdBTD/2Xx8QuKV6FoyzEP/I9hdzKN8LQJLNgzU4W767HK05rx1i8meNQQgQPgQp8wq2ive3tV5Ag=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Fact]
        public async Task Draft19_2_4_Example2_Verify()
        {
            ECDsaP256Sha256SignatureProvider provider = Make(false, "test-key-ecc-p256", "test-key-ecc-p256");

            string input =
                "\"@status\": 503\n" +
                "\"content-digest\": sha-512=:0Y6iCBzGg5rZtoXS95Ijz03mslf6KAMCloESHObfwnHJDbkkWWQz6PhhU9kxsTbARtY2PTBOzq24uJFpHsMuAg==:\n" +
                "\"content-type\": application/json\n" +
                "\"@authority\";req: example.com\n" +
                "\"@method\";req: POST\n" +
                "\"@path\";req: /foo\n" +
                "\"@query\";req: ?param=Value&Pet=dog\n" +
                "\"content-digest\";req: sha-512=:WZDPaVn/7XgHaAy8pmojAkGWoRx2UFChF41A2svX+TaPm+AbwAgBWnrIiYllu7BNNyealdVLvRwEmTHWXvJwew==:\n" +
                "\"content-type\";req: application/json\n" +
                "\"content-length\";req: 18\n" +
                "\"@signature-params\": (\"@status\" \"content-digest\" \"content-type\" \"@authority\";req \"@method\";req \"@path\";req \"@query\";req \"content-digest\";req \"content-type\";req \"content-length\";req);created=1618884479;keyid=\"test-key-ecc-p256\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"@status\" \"content-digest\" \"content-type\" \"@authority\";req \"@method\";req \"@path\";req \"@query\";req \"content-digest\";req \"content-type\";req \"content-length\";req);created=1618884479;keyid=\"test-key-ecc-p256\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("C73J41GVKc+TYXbSobvZf0CmNcptRiWN+NY1Or0A36ISg6ymdRN6ZgR2QfrtopFNzqAyv+CeWrMsNbcV2Ojsgg=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Fact]
        public async Task Draft17_4_3_Example2_Verify()
        {
            ECDsaP256Sha256SignatureProvider provider = Make(false, "test-key-ecc-p256", "test-key-ecc-p256");

            string input =
                "\"@method\": POST\n" +
                "\"@authority\": example.com\n" +
                "\"@path\": /foo\n" +
                "\"content-digest\": sha-512=:WZDPaVn/7XgHaAy8pmojAkGWoRx2UFChF41A2svX+TaPm+AbwAgBWnrIiYllu7BNNyealdVLvRwEmTHWXvJwew==:\n" +
                "\"content-type\": application/json\n" +
                "\"content-length\": 18\n" +
                "\"@signature-params\": (\"@method\" \"@authority\" \"@path\" \"content-digest\" \"content-type\" \"content-length\");created=1618884475;keyid=\"test-key-ecc-p256\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"@method\" \"@authority\" \"@path\" \"content-digest\" \"content-type\" \"content-length\");created=1618884475;keyid=\"test-key-ecc-p256\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("X5spyd6CFnAG5QnDyHfqoSNICd+BUP4LYMz2Q0JXlb//4Ijpzp+kve2w4NIyqeAuM7jTDX+sNalzA8ESSaHD3A=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        #endregion

        [Theory]
        [InlineData("ecdsa-p192-nsign.test.local", "P192")]
        [InlineData("ecdsa-p384-nsign.test.local", "P384")]
        [InlineData("rsa-nsign.test.local", "The certificate does not use elliptic curve keys.")]
        public void CtorFailsForNonP256Curve(string cert, string expectedCurve)
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
            ECDsaP256Sha256SignatureProvider signingProvider = Make(true, keyId);
            ECDsaP256Sha256SignatureProvider verifyingProvider = Make(false, keyId);
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
            ECDsaP256Sha256SignatureProvider signingProvider = Make(true, keyId);
            ECDsaP256Sha256SignatureProvider verifyingProvider = Make(false, keyId);
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
            ECDsaP256Sha256SignatureProvider signingProvider = Make(true, keyId);
            ECDsaP256Sha256SignatureProvider verifyingProvider = Make(false, keyId);
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
            ECDsaP256Sha256SignatureProvider signingProvider = Make(true);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            Assert.Null(signatureParams.Algorithm);
            signingProvider.UpdateSignatureParams(signatureParams);
            Assert.Equal("ecdsa-p256-sha256", signatureParams.Algorithm);
        }

        private static ECDsaP256Sha256SignatureProvider Make(
            bool forSigning = false,
            string? keyId = null,
            string certName = "ecdsa-p256-nsign.test.local")
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
