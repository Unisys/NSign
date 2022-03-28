using NSign.Signatures;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Providers
{
    public sealed class RsaPssSha512SignatureProviderTests
    {
        private readonly Random rng = new Random();
        private readonly RSA publicKeyFromStandard = GetPublicKeyFromStandard();

        #region From standard

        [Fact]
        public async Task Standard_2_3_Verify()
        {
            RsaPssSha512SignatureProvider provider = new RsaPssSha512SignatureProvider(null, publicKeyFromStandard, "test-key-rsa-pss");

            string input =
                "\"@method\": POST\n" +
                "\"@authority\": example.com\n" +
                "\"@path\": /foo\n" +
                "\"content-digest\": sha-512=:WZDPaVn/7XgHaAy8pmojAkGWoRx2UFChF41A2svX+TaPm+AbwAgBWnrIiYllu7BNNyealdVLvRwEmTHWXvJwew==:\n" +
                "\"content-length\": 18\n" +
                "\"content-type\": application/json\n" +
                "\"@signature-params\": (\"@method\" \"@authority\" \"@path\" \"content-digest\" \"content-length\" \"content-type\");created=1618884473;keyid=\"test-key-rsa-pss\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"@method\" \"@authority\" \"@path\" \"content-digest\" \"content-length\" \"content-type\");created=1618884473;keyid=\"test-key-rsa-pss\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("LAH8BjcfcOcLojiuOBFWn0P5keD3xAOuJRGziCLuD8r5MW9S0RoXXLzLSRfGY/3SF8kVIkHjE13SEFdTo4Af/fJ/Pu9wheqoLVdwXyY/UkBIS1M8Brc8IODsn5DFIrG0IrburbLi0uCc+E2ZIIb6HbUJ+o+jP58JelMTe0QE3IpWINTEzpxjqDf5/Df+InHCAkQCTuKsamjWXUpyOT1Wkxi7YPVNOjW4MfNuTZ9HdbD2Tr65+BXeTG9ZS/9SWuXAc+BZ8WyPz0QRz//ec3uWXd7bYYODSjRAxHqX+S1ag3LZElYyUKaAIjZ8MGOt4gXEwCSLDv/zqxZeWLj/PDkn6w=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Fact]
        public async Task Standard_3_2_Verify()
        {
            RsaPssSha512SignatureProvider provider = new RsaPssSha512SignatureProvider(null, publicKeyFromStandard, "test-key-rsa-pss");

            string input =
                "\"@method\": POST\n" +
                "\"@authority\": example.com\n" +
                "\"@path\": /foo\n" +
                "\"content-digest\": sha-512=:WZDPaVn/7XgHaAy8pmojAkGWoRx2UFChF41A2svX+TaPm+AbwAgBWnrIiYllu7BNNyealdVLvRwEmTHWXvJwew==:\n" +
                "\"content-length\": 18\n" +
                "\"content-type\": application/json\n" +
                "\"@signature-params\": (\"@method\" \"@authority\" \"@path\" \"content-digest\" \"content-length\" \"content-type\");created=1618884473;keyid=\"test-key-rsa-pss\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"@method\" \"@authority\" \"@path\" \"content-digest\" \"content-length\" \"content-type\");created=1618884473;keyid=\"test-key-rsa-pss\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("HIbjHC5rS0BYaa9v4QfD4193TORw7u9edguPh0AW3dMq9WImrlFrCGUDih47vAxi4L2YRZ3XMJc1uOKk/J0ZmZ+wcta4nKIgBkKq0rM9hs3CQyxXGxHLMCy8uqK488o+9jrptQ+xFPHK7a9sRL1IXNaagCNN3ZxJsYapFj+JXbmaI5rtAdSfSvzPuBCh+ARHBmWuNo1UzVVdHXrl8ePL4cccqlazIJdC4QEjrF+Sn4IxBQzTZsL9y9TP5FsZYzHvDqbInkTNigBcE9cKOYNFCn4D/WM7F6TNuZO9EgtzepLWcjTymlHzK7aXq6Am6sfOrpIC49yXjj3ae6HRalVc/g=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Fact]
        public async Task Standard_B_2_1_Verify()
        {
            RsaPssSha512SignatureProvider provider = new RsaPssSha512SignatureProvider(null, publicKeyFromStandard, "test-key-rsa-pss");

            string input =
                "\"@signature-params\": ();created=1618884473;keyid=\"test-key-rsa-pss\";nonce=\"b3k2pp5k7z-50gnwp.yemd\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "();created=1618884473;keyid=\"test-key-rsa-pss\";nonce=\"b3k2pp5k7z-50gnwp.yemd\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("d2pmTvmbncD3xQm8E9ZV2828BjQWGgiwAaw5bAkgibUopemLJcWDy/lkbbHAve4cRAtx31Iq786U7it++wgGxbtRxf8Udx7zFZsckzXaJMkA7ChG52eSkFxykJeNqsrWH5S+oxNFlD4dzVuwe8DhTSja8xxbR/Z2cOGdCbzR72rgFWhzx2VjBqJzsPLMIQKhO4DGezXehhWwE56YCE+O6c0mKZsfxVrogUvA4HELjVKWmAvtl6UnCh8jYzuVG5WSb/QEVPnP5TmcAnLH1g+s++v6d4s8m0gCw1fV5/SITLq9mhho8K3+7EPYTU8IU1bLhdxO5Nyt8C8ssinQ98Xw9Q=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Fact]
        public async Task Standard_B_2_2_Verify()
        {
            RsaPssSha512SignatureProvider provider = new RsaPssSha512SignatureProvider(null, publicKeyFromStandard, "test-key-rsa-pss");

            string input =
                "\"@authority\": example.com\n" +
                "\"content-digest\": sha-512=:WZDPaVn/7XgHaAy8pmojAkGWoRx2UFChF41A2svX+TaPm+AbwAgBWnrIiYllu7BNNyealdVLvRwEmTHWXvJwew==:\n" +
                "\"@signature-params\": (\"@authority\" \"content-digest\");created=1618884473;keyid=\"test-key-rsa-pss\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"@authority\" \"content-digest\");created=1618884473;keyid=\"test-key-rsa-pss\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("Fee1uy9YGZq5UUwwYU6vz4dZNvfw3GYrFl1L6YlVIyUMuWswWDNSvql4dVtSeidYjYZUm7SBCENIb5KYy2ByoC3bI+7gydd2i4OAT5lyDtmeapnAa8uP/b9xUpg+VSPElbBs6JWBIQsd+nMdHDe+ls/IwVMwXktC37SqsnbNyhNp6kcvcWpevjzFcD2VqdZleUz4jN7P+W5A3wHiMGfIjIWn36KXNB+RKyrlGnIS8yaBBrom5rcZWLrLbtg6VlrH1+/07RV+kgTh/l10h8qgpl9zQHu7mWbDKTq0tJ8K4ywcPoC4s2I4rU88jzDKDGdTTQFZoTVZxZmuTM1FvHfzIw=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Fact]
        public async Task Standard_B_2_3_Verify()
        {
            RSA publicKey = RSA.Create();
            publicKey.ImportFromPem(@"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAr4tmm3r20Wd/PbqvP1s2
+QEtvpuRaV8Yq40gjUR8y2Rjxa6dpG2GXHbPfvMs8ct+Lh1GH45x28Rw3Ry53mm+
oAXjyQ86OnDkZ5N8lYbggD4O3w6M6pAvLkhk95AndTrifbIFPNU8PPMO7OyrFAHq
gDsznjPFmTOtCEcN2Z1FpWgchwuYLPL+Wokqltd11nqqzi+bJ9cvSKADYdUAAN5W
Utzdpiy6LbTgSxP7ociU4Tn0g5I6aDZJ7A8Lzo0KSyZYoA485mqcO0GVAdVw9lq4
aOT9v6d+nb4bnNkQVklLQ3fVAvJm+xdDOp9LCNCN48V2pnDOkFV6+U9nV5oyc6XI
2wIDAQAB
-----END PUBLIC KEY-----");

            RsaPssSha512SignatureProvider provider = new RsaPssSha512SignatureProvider(null, publicKey, "test-key-rsa-pss");

            string input =
                "\"date\": Tue, 20 Apr 2021 02:07:55 GMT\n" +
                "\"@method\": POST\n" +
                "\"@path\": /foo\n" +
                "\"@query\": ?param=Value&Pet=dog\n" +
                "\"@authority\": example.com\n" +
                "\"content-type\": application/json\n" +
                "\"content-digest\": sha-512=:WZDPaVn/7XgHaAy8pmojAkGWoRx2UFChF41A2svX+TaPm+AbwAgBWnrIiYllu7BNNyealdVLvRwEmTHWXvJwew==:\n" +
                "\"content-length\": 18\n" +
                "\"@signature-params\": (\"date\" \"@method\" \"@path\" \"@query\" \"@authority\" \"content-type\" \"content-digest\" \"content-length\");created=1618884473;keyid=\"test-key-rsa-pss\"";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"date\" \"@method\" \"@path\" \"@query\" \"@authority\" \"content-type\" \"content-digest\" \"content-length\");created=1618884473;keyid=\"test-key-rsa-pss\"");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("bbN8oArOxYoyylQQUU6QYwrTuaxLwjAC9fbY2F6SVWvh0yBiMIRGOnMYwZ/5MR6fb0Kh1rIRASVxFkeGt683+qRpRRU5p2voTp768ZrCUb38K0fUxN0O0iC59DzYx8DFll5GmydPxSmme9v6ULbMFkl+V5B1TP/yPViV7KsLNmvKiLJH1pFkh/aYA2HXXZzNBXmIkoQoLd7YfW91kE9o/CCoC1xMy7JA1ipwvKvfrs65ldmlu9bpG6A9BmzhuzF8Eim5f8ui9eH8LZH896+QIF61ka39VBrohr9iyMUJpvRX2Zbhl5ZJzSRxpJyoEZAFL2FUo5fTIztsDZKEgM4cUA=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        #endregion

        [Theory]
        [InlineData(null)]
        [InlineData("my-key-id")]
        public async Task OwnSignatureCanBeVerified(string keyId)
        {
            RsaPssSha512SignatureProvider signingProvider = Make(true, keyId);
            RsaPssSha512SignatureProvider verifyingProvider = Make(false, keyId);
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
            RsaPssSha512SignatureProvider signingProvider = Make(true, keyId);
            RsaPssSha512SignatureProvider verifyingProvider = Make(false, keyId);
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
            RsaPssSha512SignatureProvider signingProvider = Make(true, keyId);
            RsaPssSha512SignatureProvider verifyingProvider = Make(false, keyId);
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
            RsaPssSha512SignatureProvider signingProvider = Make(true);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            Assert.Null(signatureParams.Algorithm);
            signingProvider.UpdateSignatureParams(signatureParams);
            Assert.Equal("rsa-pss-sha512", signatureParams.Algorithm);
        }

        private static RsaPssSha512SignatureProvider Make(
            bool forSigning = false,
            string? keyId = null,
            string keyName = "rsa-nsign.test.local")
        {
            X509Certificate2 cert;

            if (forSigning)
            {
                cert = Certificates.GetCertificateWithPrivateKey($"{keyName}.pfx", null);
            }
            else
            {
                cert = Certificates.GetCertificate($"{keyName}.cer");
            }

            return new RsaPssSha512SignatureProvider(cert, keyId ?? cert.Thumbprint);
        }

        private static RSA GetPublicKeyFromStandard()
        {
            RSA publicKey = RSA.Create();

            publicKey.ImportFromPem(@"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAr4tmm3r20Wd/PbqvP1s2
+QEtvpuRaV8Yq40gjUR8y2Rjxa6dpG2GXHbPfvMs8ct+Lh1GH45x28Rw3Ry53mm+
oAXjyQ86OnDkZ5N8lYbggD4O3w6M6pAvLkhk95AndTrifbIFPNU8PPMO7OyrFAHq
gDsznjPFmTOtCEcN2Z1FpWgchwuYLPL+Wokqltd11nqqzi+bJ9cvSKADYdUAAN5W
Utzdpiy6LbTgSxP7ociU4Tn0g5I6aDZJ7A8Lzo0KSyZYoA485mqcO0GVAdVw9lq4
aOT9v6d+nb4bnNkQVklLQ3fVAvJm+xdDOp9LCNCN48V2pnDOkFV6+U9nV5oyc6XI
2wIDAQAB
-----END PUBLIC KEY-----");

            return publicKey;
        }
    }
}
