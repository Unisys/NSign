using Microsoft.Extensions.Logging;
using Moq;
using NSign.Signatures;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Providers
{
    public sealed class SignatureProviderTests
    {
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>(MockBehavior.Loose);
        private readonly TestMessageContext messageContext;

        public SignatureProviderTests()
        {
            messageContext = new TestMessageContext(mockLogger.Object);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("MyKey")]
        [InlineData("AnotherKey")]
        public void CtorPassesKeyIdAsIs(string? keyId)
        {
            Assert.Equal(keyId, new TestProvider(keyId).KeyId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("MyKey")]
        [InlineData("AnotherKey")]
        public async Task UpdateSignatureParamsSetsKeyIdParameter(string? keyId)
        {
            SignatureProvider provider = new TestProvider(keyId);
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            Assert.Null(signatureParams.KeyId);
            await provider.UpdateSignatureParamsAsync(signatureParams, messageContext, CancellationToken.None);
            Assert.Equal(keyId, signatureParams.KeyId);
        }

        private sealed class TestProvider : SignatureProvider
        {
            public TestProvider(string? keyId) : base(keyId)
            {
            }

            public override Task<ReadOnlyMemory<byte>> SignAsync(ReadOnlyMemory<byte> input, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override Task<VerificationResult> VerifyAsync(
                SignatureParamsComponent signatureParams,
                ReadOnlyMemory<byte> input,
                ReadOnlyMemory<byte> expectedSignature,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
