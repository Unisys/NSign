using NSign.Signatures;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Providers
{
    public sealed class SignatureProviderTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("MyKey")]
        [InlineData("AnotherKey")]
        public void CtorPassesKeyIdAsIs(string keyId)
        {
            Assert.Equal(keyId, new TestProvider(keyId).KeyId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("MyKey")]
        [InlineData("AnotherKey")]
        public void UpdateSignatureParamsSetsKeyIdParameter(string keyId)
        {
            SignatureProvider provider = new TestProvider(keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            Assert.Null(signatureParams.KeyId);
            provider.UpdateSignatureParams(signatureParams);
            Assert.Equal(keyId, signatureParams.KeyId);
        }

        private sealed class TestProvider : SignatureProvider
        {
            public TestProvider(string keyId) : base(keyId)
            {
            }

            public override Task<byte[]> SignAsync(byte[] input, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override Task<VerificationResult> VerifyAsync(SignatureParamsComponent signatureParams, byte[] input, byte[] expectedSignature, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
