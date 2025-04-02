using Microsoft.Extensions.Logging;
using Moq;
using NSign.Http;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Signatures
{
    public sealed class DefaultMessageSignerTests
    {
        private readonly Mock<ISigner> mockSigner = new Mock<ISigner>(MockBehavior.Strict);
        private readonly Mock<MessageContext> mockContext;
        private readonly MessageSigningOptions options = new MessageSigningOptions();
        private readonly DefaultMessageSigner signer;

        public DefaultMessageSignerTests()
        {
            Mock<ILogger<DefaultMessageSigner>> mockLogger = new Mock<ILogger<DefaultMessageSigner>>(MockBehavior.Loose);
            mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            mockContext = new Mock<MessageContext>(MockBehavior.Strict, mockLogger.Object, new HttpFieldOptions());

            signer = new DefaultMessageSigner(mockLogger.Object, mockSigner.Object);
        }

        [Fact]
        public async Task SignMessageAsyncThrowsOnMissingOptions()
        {
            mockContext.SetupGet(c => c.SigningOptions).Returns((MessageSigningOptions?)null);

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => signer.SignMessageAsync(mockContext.Object));
            Assert.Equal("The message context does not have signing options.", ex.Message);

            mockContext.VerifyGet(c => c.SigningOptions, Times.Once());
        }

        [Fact]
        public async Task SignMessageAsyncThrowsOnMissingSignatureName()
        {
            mockContext.SetupGet(c => c.SigningOptions).Returns(options);

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => signer.SignMessageAsync(mockContext.Object));
            Assert.Equal("The SignatureName must be set to a non-blank string. Signing failed.", ex.Message);

            mockContext.VerifyGet(c => c.SigningOptions, Times.Exactly(2));
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        [InlineData(false, false, false)]
        public async Task SignMessageAsyncAddsSignature(
            bool useUpdateSignatureParams,
            bool shouldCancel,
            bool hasSetParameters)
        {
            bool setParametersCalled = false;
            CancellationToken cancellationToken = new CancellationToken(shouldCancel);
            options.SignatureName = "SignMessageAsyncAddsSignature";
            options.UseUpdateSignatureParams = useUpdateSignatureParams;
            options.SetParameters = hasSetParameters ? (_) => setParametersCalled = true : null;
            options
                .WithMandatoryComponent(SignatureComponent.Method)
                .WithOptionalComponent(SignatureComponent.RequestTarget);

            mockContext.Setup(c => c.GetDerivedComponentValue(It.Is<DerivedComponent>(comp => comp.ComponentName == "@method")))
                .Returns("OPTIONS");
            mockContext.Setup(c => c.GetDerivedComponentValue(It.Is<DerivedComponent>(comp => comp.ComponentName == "@request-target")))
                .Returns("/base/path?query=foo");

            mockContext.SetupGet(c => c.SigningOptions).Returns(options);
            mockContext.SetupGet(c => c.Aborted).Returns(cancellationToken);
            mockContext.SetupGet(c => c.HasResponse).Returns(hasSetParameters); // Could also do with another param, but not really necessary.
            mockContext.Setup(c => c.AddHeader("signature", "SignMessageAsyncAddsSignature=:blah:"));
            mockContext.Setup(c => c.AddHeader("signature-input", "SignMessageAsyncAddsSignature=(\"@method\" \"@request-target\")"));

            mockSigner.Setup(s => s.UpdateSignatureParamsAsync(It.IsAny<SignatureParamsComponent>(), It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockSigner.Setup(s => s.SignAsync(It.IsAny<ReadOnlyMemory<byte>>(), cancellationToken))
                .ReturnsAsync(new ReadOnlyMemory<byte>(new byte[] { 0x6e, 0x56, 0xa1, })); // base64 encoded to 'blah'

            await signer.SignMessageAsync(mockContext.Object);

            mockSigner.Verify(s => s.UpdateSignatureParamsAsync(It.IsAny<SignatureParamsComponent>(), It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()),
                Times.Exactly(useUpdateSignatureParams ? 1 : 0));
            mockSigner.Verify(s => s.SignAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once());

            mockContext.VerifyGet(c => c.Aborted, Times.Exactly(useUpdateSignatureParams ? 2 : 1));
            mockContext.Verify(c => c.GetDerivedComponentValue(It.IsAny<DerivedComponent>()), Times.Exactly(4));
            mockContext.Verify(c => c.AddHeader(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));

            Assert.Equal(setParametersCalled, hasSetParameters);
        }
    }
}
