using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using NSign.Signatures;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.AspNetCore
{
    public sealed class ResponseSigningMiddlewareTests
    {
        private readonly DefaultHttpContext httpContext = new DefaultHttpContext();
        private readonly Mock<IMessageSigner> mockSigner = new Mock<IMessageSigner>(MockBehavior.Strict);
        private readonly MessageSigningOptions options = new MessageSigningOptions();
        private readonly ResponseSigningMiddleware middleware;
        private long numCallsToNext;

        public ResponseSigningMiddlewareTests()
        {
            Mock<ILogger<ResponseSigningMiddleware>> mockLogger =
                new Mock<ILogger<ResponseSigningMiddleware>>(MockBehavior.Loose);
            mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            middleware = new ResponseSigningMiddleware(
                mockLogger.Object,
                new OptionsWrapper<MessageSigningOptions>(options),
                mockSigner.Object);
        }

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("  ")]
        //public async Task InvokeAsyncThrowsForMissingSignatureName(string signatureName)
        //{
        //    //options.SignatureName = signatureName;

        //    InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
        //        () => middleware.InvokeAsync(httpContext, CountingMiddleware));
        //    Assert.Equal("The SignatureName must be set to a non-blank string. Signing failed.", ex.Message);

        //    Assert.Equal(0, numCallsToNext);
        //}

        [Fact]
        public async Task InvokeAsyncRegistersResponseContextForHandling()
        {
            Mock<IHttpResponseFeature> responseFeature = new Mock<IHttpResponseFeature>(MockBehavior.Strict);

            responseFeature.Setup(f => f.OnStarting(It.IsAny<Func<object, Task>>(), It.IsAny<object>()));
            httpContext.Features.Set(responseFeature.Object);
            //options.SignatureName = "unittest";

            await middleware.InvokeAsync(httpContext, CountingMiddleware);
            Assert.Equal(1, numCallsToNext);

            responseFeature.Verify(f => f.OnStarting(It.IsAny<Func<object, Task>>(), It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task ResponseContextHandlingInsertsSignature()
        {
            Mock<IHttpResponseFeature> mockRespFeature = new Mock<IHttpResponseFeature>(MockBehavior.Strict);
            Func<object?, Task>? callback = null;
            object? state = null;
            HeaderDictionary responseHeaders = new HeaderDictionary();

            mockRespFeature.Setup(f => f.OnStarting(It.IsAny<Func<object, Task>>(), It.IsAny<object>()))
                .Callback<Func<object?, Task>, object>((cb, stateObj) =>
                {
                    callback = cb;
                    state = stateObj;
                });
            mockRespFeature.SetupGet(f => f.Headers).Returns(responseHeaders);

            mockSigner.Setup(s => s.SignMessageAsync(It.IsAny<MessageContext>()))
                .Callback((MessageContext ctx) => ctx.AddHeader("x-unit-test", "success"))
                .Returns(Task.CompletedTask);

            httpContext.Features.Set(mockRespFeature.Object);
            //options.SignatureName = "unittest";

            await middleware.InvokeAsync(httpContext, CountingMiddleware);
            Assert.Equal(1, numCallsToNext);

            // Indicate that the response is starting.
            await callback!(state);

            Assert.True(httpContext.Response.Headers.TryGetValue("x-unit-test", out StringValues value));
            Assert.Equal("success", value);

            mockRespFeature.Verify(f => f.OnStarting(It.IsAny<Func<object, Task>>(), It.IsAny<object>()), Times.Once);
            mockSigner.Verify(s => s.SignMessageAsync(It.IsAny<MessageContext>()), Times.Once);
        }

        private Task CountingMiddleware(HttpContext context)
        {
            Interlocked.Increment(ref numCallsToNext);
            return Task.CompletedTask;
        }
    }
}
