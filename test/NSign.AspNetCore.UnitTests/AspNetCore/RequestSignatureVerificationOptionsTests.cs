using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NSign.Http;
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
        private readonly HttpFieldOptions httpFieldOptions = new HttpFieldOptions();
        private readonly RequestSignatureVerificationOptions signatureVerificationOptions = new RequestSignatureVerificationOptions();
        private long numCallsToNext;

        public RequestSignatureVerificationOptionsTests()
        {
            messageContext = new RequestMessageContext(httpContext,
                                                       httpFieldOptions,
                                                       signatureVerificationOptions,
                                                       CountingMiddleware,
                                                       mockLogger.Object);
        }

        [Fact]
        public async Task OnMissingSignaturesUsesDefault()
        {
            signatureVerificationOptions.MissingSignatureResponseStatus = 123;

            await signatureVerificationOptions.OnMissingSignatures(messageContext);

            Assert.Equal(123, httpContext.Response.StatusCode);
            Assert.Equal(0, numCallsToNext);
        }

        [Fact]
        public async Task OnSignatureInputErrorUsesDefault()
        {
            signatureVerificationOptions.SignatureInputErrorResponseStatus = 234;

            Dictionary<string, VerificationResult> map = new Dictionary<string, VerificationResult>()
            {
                { "first", VerificationResult.Unknown },
                { "second", VerificationResult.Unknown },
            };

            await signatureVerificationOptions.OnSignatureInputError(messageContext, map);

            Assert.Equal(234, httpContext.Response.StatusCode);
            Assert.Equal(0, numCallsToNext);
        }

        [Fact]
        public async Task OnSignatureVerificationFailedDefault()
        {
            signatureVerificationOptions.VerificationErrorResponseStatus = 345;

            Dictionary<string, VerificationResult> map = new Dictionary<string, VerificationResult>()
            {
                { "a", VerificationResult.Unknown },
                { "b", VerificationResult.Unknown },
            };

            await signatureVerificationOptions.OnSignatureVerificationFailed(messageContext, map);

            Assert.Equal(345, httpContext.Response.StatusCode);
            Assert.Equal(0, numCallsToNext);
        }

        [Fact]
        public void OnSignatureVerificationSucceededUsesDefault()
        {
            Assert.Same(Task.CompletedTask, signatureVerificationOptions.OnSignatureVerificationSucceeded(messageContext));

            Assert.Equal(1, numCallsToNext);
        }

        private Task CountingMiddleware(HttpContext context)
        {
            Interlocked.Increment(ref numCallsToNext);
            return Task.CompletedTask;
        }
    }
}
