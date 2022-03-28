using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NSign.Signatures;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.AspNetCore
{
    public sealed class RequestSignatureVerificationOptionsTests
    {
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>(MockBehavior.Loose);
        private readonly DefaultHttpContext httpContext = new DefaultHttpContext();
        private readonly RequestMessageContext messageContext;
        private readonly RequestSignatureVerificationOptions options = new RequestSignatureVerificationOptions();
        private long numCallsToNext;

        public RequestSignatureVerificationOptionsTests()
        {
            messageContext = new RequestMessageContext(httpContext, options, CountingMiddleware, mockLogger.Object);
        }

        [Fact]
        public async Task OnMissingSignaturesUsesDefault()
        {
            options.MissingSignatureResponseStatus = 123;

            await options.OnMissingSignatures(messageContext);

            Assert.Equal(123, httpContext.Response.StatusCode);
            Assert.Equal(0, numCallsToNext);
        }

        [Fact]
        public async Task OnSignatureInputErrorUsesDefault()
        {
            options.SignatureInputErrorResponseStatus = 234;

            Dictionary<string, VerificationResult> map = new Dictionary<string, VerificationResult>()
            {
                { "first", VerificationResult.Unknown },
                { "second", VerificationResult.Unknown },
            };

            await options.OnSignatureInputError(messageContext, map);

            Assert.Equal(234, httpContext.Response.StatusCode);
            Assert.Equal(0, numCallsToNext);
        }

        [Fact]
        public async Task OnSignatureVerificationFailedDefault()
        {
            options.VerificationErrorResponseStatus = 345;

            Dictionary<string, VerificationResult> map = new Dictionary<string, VerificationResult>()
            {
                { "a", VerificationResult.Unknown },
                { "b", VerificationResult.Unknown },
            };

            await options.OnSignatureVerificationFailed(messageContext, map);

            Assert.Equal(345, httpContext.Response.StatusCode);
            Assert.Equal(0, numCallsToNext);
        }

        [Fact]
        public void OnSignatureVerificationSucceededUsesDefault()
        {
            Assert.Same(Task.CompletedTask, options.OnSignatureVerificationSucceeded(messageContext));

            Assert.Equal(1, numCallsToNext);
        }

        private Task CountingMiddleware(HttpContext context)
        {
            Interlocked.Increment(ref numCallsToNext);
            return Task.CompletedTask;
        }
    }
}
