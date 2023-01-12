using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Providers
{
    public sealed class ECDsaSignatureProviderTests
    {
        [Fact]
        public void CtorValidatesInput()
        {
            ArgumentException ex;

            ex = Assert.Throws<ArgumentException>(
                () => new TestECDsa(Certificates.GetCertificate("rsa-nsign.test.local.cer"), "blah", null));
            Assert.Equal("certificate", ex.ParamName);
            Assert.Equal("The certificate does not use elliptic curve keys. (Parameter 'certificate')", ex.Message);
        }

        [Fact]
        public async Task SignAsyncThrowsWhenPrivateKeyMissing()
        {
            TestECDsa provider = Make(false);
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.SignAsync(new byte[] { }, default));

            Assert.Equal("Cannot sign using a certificate without a private key.", ex.Message);
        }

        [Fact]
        public void DisposeDoesNotThrow()
        {
            using TestECDsa signingProvider = Make(true);
            using TestECDsa verifyingProvider = Make(false);
        }

        private static TestECDsa Make(bool forSigning = false, string? keyId = null, string certName = "ecdsa-p384-nsign.test.local")
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

            return new TestECDsa(cert, "test-rsa", keyId ?? cert.Thumbprint);
        }

        private sealed class TestECDsa : ECDsaSignatureProvider
        {
            public TestECDsa(X509Certificate2 certificate, string algorithmName, string? keyId)
                : base(certificate, "irrelevant", "P-Test", algorithmName, keyId)
            { }

            protected override HashAlgorithmName SignatureHash => throw new NotImplementedException();

            protected override void CheckKeyAlgorithm(ECDsa publicKey, string parameterName)
            {
                if (null == publicKey)
                {
                    throw new ArgumentException("The certificate does not use elliptic curve keys.", parameterName);
                }
            }
        }
    }
}
