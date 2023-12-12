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
    public sealed class RsaPkcs15Sha256SignatureProviderTests
    {
        private readonly Random rng = new Random();
        private readonly RSA publicKeyFromStandard = GetPublicKeyFromStandard();

        #region From standard

        [Fact]
        public async Task Standard_4_3_Sign()
        {
            RsaPkcs15Sha256SignatureProvider provider = Make(true, "test-key-rsa", "test-key-rsa");

            string input =
                "\"signature\";key=\"sig1\": :LAH8BjcfcOcLojiuOBFWn0P5keD3xAOuJRGziCLuD8r5MW9S0RoXXLzLSRfGY/3SF8kVIkHjE13SEFdTo4Af/fJ/Pu9wheqoLVdwXyY/UkBIS1M8Brc8IODsn5DFIrG0IrburbLi0uCc+E2ZIIb6HbUJ+o+jP58JelMTe0QE3IpWINTEzpxjqDf5/Df+InHCAkQCTuKsamjWXUpyOT1Wkxi7YPVNOjW4MfNuTZ9HdbD2Tr65+BXeTG9ZS/9SWuXAc+BZ8WyPz0QRz//ec3uWXd7bYYODSjRAxHqX+S1ag3LZElYyUKaAIjZ8MGOt4gXEwCSLDv/zqxZeWLj/PDkn6w==:\n" +
                "\"forwarded\": for=192.0.2.123\n" +
                "\"@signature-params\": (\"signature\";key=\"sig1\" \"forwarded\");created=1618884480;expires=1618884540;keyid=\"test-key-rsa\";alg=\"rsa-v1_5-sha256\"";

            ReadOnlyMemory<byte> signature = await provider.SignAsync(Encoding.ASCII.GetBytes(input), default);
            string sigBase64 = Convert.ToBase64String(signature.Span);
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

        [Fact]
        public async Task Standard_4_3_Sign_Updated_Draft16()
        {
            RsaPkcs15Sha256SignatureProvider provider = Make(true, "test-key-rsa", "test-key-rsa");

            string input =
                "\"signature\";key=\"sig1\": :hNojB+wWw4A7SYF3qK1S01Y4UP5i2JZFYa2WOlMB4Np5iWmJSO0bDe2hrYRbcIWqVAFjuuCBRsB7lYQJkzbb6g==:\n" +
                "\"@authority\": origin.host.internal.example\n" +
                "\"forwarded\": for=192.0.2.123\n" +
                "\"@signature-params\": (\"signature\";key=\"sig1\" \"@authority\" \"forwarded\");created=1618884480;keyid=\"test-key-rsa\";alg=\"rsa-v1_5-sha256\";expires=1618884540";

            ReadOnlyMemory<byte> signature = await provider.SignAsync(Encoding.ASCII.GetBytes(input), default);
            string sigBase64 = Convert.ToBase64String(signature.Span);
            Assert.Equal(
                "YvYVO11F+Q+N4WZNeBdjFKluswwE3vQ4cTXpBwEiMz2hwu0J+wSJLRhHlIZ1N83epfnKDxY9cbNaVlbtr2UOLkw5O5Q5M5yrjx3s1mgDOsV7fuItD6iDyNISCiKRuevl+M+TyYBo10ubG83As5CeeoUdmrtI4G6QX7RqEeX0Xj/CYofHljr/dVzARxskjHEQbTztYVg4WD+LWo1zjx9w5fw26tsOMagfXLpDb4zb4/lgpgyNKoXFwG7c89KId5q+0BC+kryWuA35ZcQGaRPAz/NqzeKq/c7p7b/fmHS71fy1jOaFgWFmD+Z77bJLO8AVKuF0y2fpL3KUYHyITQHOsA==",
                sigBase64);
        }

        [Fact]
        public async Task Standard_4_3_Verify_Updated_Draft16()
        {
            RsaPkcs15Sha256SignatureProvider provider = Make(false, "test-key-rsa", "test-key-rsa");

            string input =
                "\"signature\";key=\"sig1\": :hNojB+wWw4A7SYF3qK1S01Y4UP5i2JZFYa2WOlMB4Np5iWmJSO0bDe2hrYRbcIWqVAFjuuCBRsB7lYQJkzbb6g==:\n" +
                "\"@authority\": origin.host.internal.example\n" +
                "\"forwarded\": for=192.0.2.123\n" +
                "\"@signature-params\": (\"signature\";key=\"sig1\" \"@authority\" \"forwarded\");created=1618884480;keyid=\"test-key-rsa\";alg=\"rsa-v1_5-sha256\";expires=1618884540";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"signature\";key=\"sig1\" \"@authority\" \"forwarded\");created=1618884480;keyid=\"test-key-rsa\";alg=\"rsa-v1_5-sha256\";expires=1618884540");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String(
                    "YvYVO11F+Q+N4WZNeBdjFKluswwE3vQ4cTXpBwEiMz2hwu0J+wSJLRhHlIZ1N83epfnKDxY9cbNaVlbtr2UOLkw5O5Q5M5yrjx3s1mgDOsV7fuItD6iDyNISCiKRuevl+M+TyYBo10ubG83As5CeeoUdmrtI4G6QX7RqEeX0Xj/CYofHljr/dVzARxskjHEQbTztYVg4WD+LWo1zjx9w5fw26tsOMagfXLpDb4zb4/lgpgyNKoXFwG7c89KId5q+0BC+kryWuA35ZcQGaRPAz/NqzeKq/c7p7b/fmHS71fy1jOaFgWFmD+Z77bJLO8AVKuF0y2fpL3KUYHyITQHOsA=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        [Fact]
        public async Task Standard_4_3_Verify_PublicKeyOnly()
        {
            RsaPkcs15Sha256SignatureProvider provider = new RsaPkcs15Sha256SignatureProvider(null, publicKeyFromStandard, "test-key-rsa");

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

        [Fact]
        public async Task Draft17_4_3_Example1_Sign()
        {
            RsaPkcs15Sha256SignatureProvider provider = Make(true, "test-key-rsa", "test-key-rsa");

            string input =
                "\"@method\": POST\n" +
                "\"@authority\": origin.host.internal.example\n" +
                "\"@path\": /foo\n" +
                "\"content-digest\": sha-512=:WZDPaVn/7XgHaAy8pmojAkGWoRx2UFChF41A2svX+TaPm+AbwAgBWnrIiYllu7BNNyealdVLvRwEmTHWXvJwew==:\n" +
                "\"content-type\": application/json\n" +
                "\"content-length\": 18\n" +
                "\"forwarded\": for=192.0.2.123;host=example.com;proto=https\n" +
                "\"@signature-params\": (\"@method\" \"@authority\" \"@path\" \"content-digest\" \"content-type\" \"content-length\" \"forwarded\");created=1618884480;keyid=\"test-key-rsa\";alg=\"rsa-v1_5-sha256\";expires=1618884540";

            ReadOnlyMemory<byte> signature = await provider.SignAsync(Encoding.ASCII.GetBytes(input), default);
            string sigBase64 = Convert.ToBase64String(signature.Span);
            Assert.Equal(
                "S6ZzPXSdAMOPjN/6KXfXWNO/f7V6cHm7BXYUh3YD/fRad4BCaRZxP+JH+8XY1I6+8Cy+CM5g92iHgxtRPz+MjniOaYmdkDcnL9cCpXJleXsOckpURl49GwiyUpZ10KHgOEe11sx3G2gxI8S0jnxQB+Pu68U9vVcasqOWAEObtNKKZd8tSFu7LB5YAv0RAGhB8tmpv7sFnIm9y+7X5kXQfi8NMaZaA8i2ZHwpBdg7a6CMfwnnrtflzvZdXAsD3LH2TwevU+/PBPv0B6NMNk93wUs/vfJvye+YuI87HU38lZHowtznbLVdp770I6VHR6WfgS9ddzirrswsE1w5o0LV/g==",
                sigBase64);
        }

        [Fact]
        public async Task Draft17_4_3_Example1_Verify()
        {
            RsaPkcs15Sha256SignatureProvider provider = Make(false, "test-key-rsa", "test-key-rsa");

            string input =
                "\"@method\": POST\n" +
                "\"@authority\": origin.host.internal.example\n" +
                "\"@path\": /foo\n" +
                "\"content-digest\": sha-512=:WZDPaVn/7XgHaAy8pmojAkGWoRx2UFChF41A2svX+TaPm+AbwAgBWnrIiYllu7BNNyealdVLvRwEmTHWXvJwew==:\n" +
                "\"content-type\": application/json\n" +
                "\"content-length\": 18\n" +
                "\"forwarded\": for=192.0.2.123;host=example.com;proto=https\n" +
                "\"@signature-params\": (\"@method\" \"@authority\" \"@path\" \"content-digest\" \"content-type\" \"content-length\" \"forwarded\");created=1618884480;keyid=\"test-key-rsa\";alg=\"rsa-v1_5-sha256\";expires=1618884540";
            SignatureParamsComponent sigParams = new SignatureParamsComponent(
                "(\"@method\" \"@authority\" \"@path\" \"content-digest\" \"content-type\" \"content-length\" \"forwarded\");created=1618884480;keyid=\"test-key-rsa\";alg=\"rsa-v1_5-sha256\";expires=1618884540");

            VerificationResult result = await provider.VerifyAsync(
                sigParams,
                Encoding.ASCII.GetBytes(input),
                Convert.FromBase64String("S6ZzPXSdAMOPjN/6KXfXWNO/f7V6cHm7BXYUh3YD/fRad4BCaRZxP+JH+8XY1I6+8Cy+CM5g92iHgxtRPz+MjniOaYmdkDcnL9cCpXJleXsOckpURl49GwiyUpZ10KHgOEe11sx3G2gxI8S0jnxQB+Pu68U9vVcasqOWAEObtNKKZd8tSFu7LB5YAv0RAGhB8tmpv7sFnIm9y+7X5kXQfi8NMaZaA8i2ZHwpBdg7a6CMfwnnrtflzvZdXAsD3LH2TwevU+/PBPv0B6NMNk93wUs/vfJvye+YuI87HU38lZHowtznbLVdp770I6VHR6WfgS9ddzirrswsE1w5o0LV/g=="),
                default);
            Assert.Equal(VerificationResult.SuccessfullyVerified, result);
        }

        #endregion

        [Theory]
        [InlineData(null)]
        [InlineData("my-key-id")]
        public async Task OwnSignatureCanBeVerified(string? keyId)
        {
            RsaPkcs15Sha256SignatureProvider signingProvider = Make(true, keyId);
            RsaPkcs15Sha256SignatureProvider verifyingProvider = Make(false, keyId);
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
            RsaPkcs15Sha256SignatureProvider signingProvider = Make(true, keyId);
            RsaPkcs15Sha256SignatureProvider verifyingProvider = Make(false, keyId);
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
            RsaPkcs15Sha256SignatureProvider signingProvider = Make(true, keyId);
            RsaPkcs15Sha256SignatureProvider verifyingProvider = Make(false, keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent().WithAlgorithm(SignatureAlgorithm.RsaPssSha512);
            byte[] random = new byte[2048];

            rng.NextBytes(random);
            ReadOnlyMemory<byte> signature = await signingProvider.SignAsync(random, CancellationToken.None);

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
            string? keyId = null,
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

        private static RSA GetPublicKeyFromStandard()
        {
            RSA publicKey = RSA.Create();

            publicKey.ImportFromPem(@"-----BEGIN RSA PUBLIC KEY-----
MIIBCgKCAQEAhAKYdtoeoy8zcAcR874L8cnZxKzAGwd7v36APp7Pv6Q2jdsPBRrw
WEBnez6d0UDKDwGbc6nxfEXAy5mbhgajzrw3MOEt8uA5txSKobBpKDeBLOsdJKFq
MGmXCQvEG7YemcxDTRPxAleIAgYYRjTSd/QBwVW9OwNFhekro3RtlinV0a75jfZg
kne/YiktSvLG34lw2zqXBDTC5NHROUqGTlML4PlNZS5Ri2U4aCNx2rUPRcKIlE0P
uKxI4T+HIaFpv8+rdV6eUgOrB2xeI1dSFFn/nnv5OoZJEIB+VmuKn3DCUcCZSFlQ
PSXSfBDiUGhwOw76WuSSsf1D4b/vLoJ10wIDAQAB
-----END RSA PUBLIC KEY-----");

            return publicKey;
        }
    }
}
