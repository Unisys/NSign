using Microsoft.Extensions.Logging;
using Moq;
using NSign.Signatures;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NSign
{
    public sealed class SignatureVerificationOptionsTests
    {
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>(MockBehavior.Loose);
        private readonly Mock<MessageContext> mockContext;
        private readonly SignatureVerificationOptions options = new SignatureVerificationOptions();

        public SignatureVerificationOptionsTests()
        {
            mockContext = new Mock<MessageContext>(MockBehavior.Strict, mockLogger.Object);
        }

        [Fact]
        public void ShouldVerifyUsesDefault()
        {
            Assert.False(options.ShouldVerify("blah"));

            options.SignaturesToVerify.Add("blah");

            Assert.True(options.ShouldVerify("blah"));
        }

        [Fact]
        public async Task OnMissingSignaturesUsesDefault()
        {
            await Assert.ThrowsAsync<SignatureMissingException>(() => options.OnMissingSignatures(mockContext.Object));
        }

        [Fact]
        public async Task OnSignatureInputErrorUsesDefault()
        {
            Dictionary<string, VerificationResult> map = new Dictionary<string, VerificationResult>()
            {
                { "first", VerificationResult.Unknown },
                { "second", VerificationResult.Unknown },
            };

            SignatureInputException ex = await Assert.ThrowsAsync<SignatureInputException>(
                () => options.OnSignatureInputError(mockContext.Object, map));

            Assert.Equal("Some signatures have input errors: first, second.", ex.Message);
        }

        [Fact]
        public async Task OnSignatureVerificationFailedDefault()
        {
            Dictionary<string, VerificationResult> map = new Dictionary<string, VerificationResult>()
            {
                { "a", VerificationResult.Unknown },
                { "b", VerificationResult.Unknown },
            };

            SignatureVerificationFailedException ex = await Assert.ThrowsAsync<SignatureVerificationFailedException>(
                () => options.OnSignatureVerificationFailed(mockContext.Object, map));

            Assert.Equal("Some signatures have failed verification: a, b.", ex.Message);
        }

        [Fact]
        public void OnSignatureVerificationSucceededUsesDefault()
        {
            Assert.Same(Task.CompletedTask, options.OnSignatureVerificationSucceeded(mockContext.Object));
        }
    }
}
