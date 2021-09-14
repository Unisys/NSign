using System;
using Xunit;

namespace NSign.Signatures
{
    public sealed class SignatureAlgorithmsExtensionsTests
    {
        [Theory]
        [InlineData(SignatureAlgorithm.EcdsaP256Sha256, "ecdsa-p256-sha256")]
        [InlineData(SignatureAlgorithm.HmacSha256, "hmac-sha256")]
        [InlineData(SignatureAlgorithm.RsaPkcs15Sha256, "rsa-v1_5-sha256")]
        [InlineData(SignatureAlgorithm.RsaPssSha512, "rsa-pss-sha512")]
        public void GetNameWorks(SignatureAlgorithm alg, string expectedName)
        {
            Assert.Equal(expectedName, alg.GetName());
        }

        [Theory]
        [InlineData(SignatureAlgorithm.Unknown)]
        [InlineData((SignatureAlgorithm)999)]
        public void GetNameThrowsForUnsupportedAlgorithm(SignatureAlgorithm alg)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => alg.GetName());
            Assert.Equal($"Unsupported signature algorithm: {alg}", ex.Message);
        }

        [Theory]
        [InlineData("ecdsa-p256-sha256", SignatureAlgorithm.EcdsaP256Sha256)]
        [InlineData("hmac-sha256", SignatureAlgorithm.HmacSha256)]
        [InlineData("rsa-v1_5-sha256", SignatureAlgorithm.RsaPkcs15Sha256)]
        [InlineData("rsa-pss-sha512", SignatureAlgorithm.RsaPssSha512)]
        public void ToSignatureAlgorithmWorks(string name, SignatureAlgorithm expectedAlg)
        {
            Assert.Equal(expectedAlg, SignatureAlgorithmsExtensions.ToSignatureAlgorithm(name));
        }

        [Theory]
        [InlineData("unknown")]
        [InlineData("999")]
        public void ToSignatureAlgorithmThrowsForUnsupportedAlgorithm(string algName)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => algName.ToSignatureAlgorithm());
            Assert.Equal($"Unsupported signature algorithm: '{algName}'", ex.Message);
        }
    }
}
