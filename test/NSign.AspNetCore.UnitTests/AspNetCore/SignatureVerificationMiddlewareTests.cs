using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NSign.Http;
using NSign.Signatures;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.AspNetCore
{
    public sealed class SignatureVerificationMiddlewareTests
    {
        private readonly DefaultHttpContext httpContext = new DefaultHttpContext();
        private readonly Mock<IMessageVerifier> mockVerifier = new Mock<IMessageVerifier>(MockBehavior.Strict);
        private readonly HttpFieldOptions httpFieldOptions = new HttpFieldOptions();
        private readonly RequestSignatureVerificationOptions signatureVerificationOptions = new RequestSignatureVerificationOptions();
        private readonly SignatureVerificationMiddleware middleware;
        private long numCallsToNext;

        public SignatureVerificationMiddlewareTests()
        {
            Mock<ILogger<SignatureVerificationMiddleware>> mockLogger =
                new Mock<ILogger<SignatureVerificationMiddleware>>(MockBehavior.Loose);
            mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            middleware = new SignatureVerificationMiddleware(
                mockLogger.Object,
                mockVerifier.Object,
                new OptionsWrapper<HttpFieldOptions>(httpFieldOptions),
                new OptionsWrapper<RequestSignatureVerificationOptions>(signatureVerificationOptions));
        }

        [Fact]
        public async Task InvokeAsyncUsesVerifier()
        {
            mockVerifier
                .Setup(v => v.VerifyMessageAsync(It.Is<RequestMessageContext>(c => c.HttpContext == httpContext)))
                .Returns(Task.CompletedTask);

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
            mockVerifier.Verify(v => v.VerifyMessageAsync(It.IsAny<MessageContext>()), Times.Once());
        }

        private Task CountingMiddleware(HttpContext context)
        {
            Interlocked.Increment(ref numCallsToNext);
            return Task.CompletedTask;
        }
    }
}
