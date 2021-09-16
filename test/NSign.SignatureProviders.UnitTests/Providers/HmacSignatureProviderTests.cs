using System;
using System.Security.Cryptography;
using Xunit;

namespace NSign.Providers
{
    public sealed class HmacSignatureProviderTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void CtorThrowsOnMissingAlg(string alg)
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new TestHmac(alg, null));
            Assert.Equal("algorithmName", ex.ParamName);
        }

        private sealed class TestHmac : HmacSignatureProvider
        {
            public TestHmac(string algorithmName, string keyId) : base(algorithmName, keyId)
            {
            }

            protected override HMAC GetAlgorithm()
            {
                throw new NotImplementedException();
            }
        }
    }
}
