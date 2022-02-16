using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Providers
{
    public sealed class RsaSignatureProviderTests
    {
        [Fact]
        public void CtorValidatesInput()
        {
            ArgumentException ex;

            ex = Assert.Throws<ArgumentNullException>(() => new TestRsa(null, null, null));
            Assert.Equal("certificate", ex.ParamName);

            ex = Assert.Throws<ArgumentException>(
                () => new TestRsa(Certificates.GetCertificate("ecdsa-p256-nsign.test.local.cer"), null, null));
            Assert.Equal("certificate", ex.ParamName);
            Assert.Equal("The certificate does not use RSA keys. (Parameter 'certificate')", ex.Message);

            ex = Assert.Throws<ArgumentNullException>(
                () => new TestRsa(Certificates.GetCertificate("rsa-nsign.test.local.cer"), null, null));
            Assert.Equal("algorithmName", ex.ParamName);
        }

        [Fact]
        public void CtorWithKeysValidatesInput()
        {
            ArgumentException ex;
            RSA publicKey = RSA.Create();

            ex = Assert.Throws<ArgumentNullException>(() => new TestRsa(null, null, null, null));
            Assert.Equal("publicKey", ex.ParamName);

            ex = Assert.Throws<ArgumentNullException>(
                () => new TestRsa(null, publicKey, null, null));
            Assert.Equal("algorithmName", ex.ParamName);
        }

        [Fact]
        public async Task SignAsyncThrowsWhenPrivateKeyMissing()
        {
            TestRsa provider = Make(false);
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.SignAsync(new byte[] { }, default));

            Assert.Equal("Cannot sign using a certificate without a private key.", ex.Message);
        }

        [Fact]
        public void DisposeDoesNotThrow()
        {
            using TestRsa signingProvider = Make(true);
            using TestRsa verifyingProvider = Make(false);
            using TestRsa providerWithPublicKey = new TestRsa(null, RSA.Create(), "test-rsa", null);
        }

        private static TestRsa Make(bool forSigning = false, string keyId = null)
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

            return new TestRsa(cert, "test-rsa", keyId ?? cert.Thumbprint);
        }

        private sealed class TestRsa : RsaSignatureProvider
        {
            public TestRsa(X509Certificate2 certificate, string algorithmName, string keyId)
                : base(certificate, algorithmName, keyId)
            { }

            public TestRsa(RSA privateKey, RSA publicKey, string algorithmName, string keyId)
                : base(privateKey, publicKey, algorithmName, keyId)
            { }

            protected override HashAlgorithmName SignatureHash => throw new NotImplementedException();

            protected override RSASignaturePadding SignaturePadding => throw new NotImplementedException();
        }
    }
}
