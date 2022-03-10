using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NSign.Signatures;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.AspNetCore
{
    public sealed class ResponseSigningMiddlewareTests
    {
        private readonly DefaultHttpContext httpContext = new DefaultHttpContext();
        private readonly Mock<ISigner> mockSigner = new Mock<ISigner>(MockBehavior.Strict);
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
                mockSigner.Object,
                new OptionsWrapper<MessageSigningOptions>(options));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public async Task InvokeAsyncThrowsForMissingSignatureName(string signatureName)
        {
            options.SignatureName = signatureName;

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => middleware.InvokeAsync(httpContext, CountingMiddleware));
            Assert.Equal("The SignatureName must be set to a non-blank string. Signing failed.", ex.Message);

            Assert.Equal(0, numCallsToNext);
        }

        [Fact]
        public async Task InvokeAsyncRegistersResponseContextForHandling()
        {
            Mock<IHttpResponseFeature> responseFeature = new Mock<IHttpResponseFeature>(MockBehavior.Strict);

            responseFeature.Setup(f => f.OnStarting(It.IsAny<Func<object, Task>>(), It.IsAny<object>()));
            httpContext.Features.Set(responseFeature.Object);
            options.SignatureName = "unittest";

            await middleware.InvokeAsync(httpContext, CountingMiddleware);
            Assert.Equal(1, numCallsToNext);

            responseFeature.Verify(f => f.OnStarting(It.IsAny<Func<object, Task>>(), It.IsAny<object>()), Times.Once);
        }

        [Theory]
        [InlineData(true, true, 200)]
        [InlineData(true, false, 201)]
        [InlineData(false, true, 202)]
        [InlineData(false, false, 203)]
        public async Task ResponseContextHandlingInsertsSignature(bool useUpdateSignatureParams, bool useSetParameters, int statusCode)
        {
            Mock<IHttpResponseFeature> mockRespFeature = new Mock<IHttpResponseFeature>(MockBehavior.Strict);
            Func<object, Task> callback = null;
            object state = null;
            HeaderDictionary responseHeaders = new HeaderDictionary();

            mockRespFeature.Setup(f => f.OnStarting(It.IsAny<Func<object, Task>>(), It.IsAny<object>()))
                .Callback<Func<object, Task>, object>((cb, stateObj) =>
                {
                    callback = cb;
                    state = stateObj;
                });
            mockRespFeature.SetupGet(f => f.Headers).Returns(responseHeaders);
            mockRespFeature.SetupGet(f => f.StatusCode).Returns(statusCode);

            mockSigner.Setup(s => s.UpdateSignatureParams(
               It.Is<SignatureParamsComponent>(c => c.Nonce == (useSetParameters ? "ResponseContextHandlingInsertsSignature" : null))));
            string expectedParams = useSetParameters ? ";nonce=\"ResponseContextHandlingInsertsSignature\"" : "";
            mockSigner
                .Setup(s => s.SignAsync(
                    It.Is<byte[]>(input =>
                        Encoding.ASCII.GetString(input) == $"\"@status\": {statusCode}\n" +
                            $"\"@signature-params\": (\"@status\"){expectedParams}"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new byte[] { 0x41, });

            httpContext.Features.Set(mockRespFeature.Object);
            options.SignatureName = "unittest";
            options.UseUpdateSignatureParams = useUpdateSignatureParams;
            if (useSetParameters)
            {
                options.SetParameters = (x) => x.WithNonce("ResponseContextHandlingInsertsSignature");
            }
            options.WithMandatoryComponent(SignatureComponent.Status)
                .WithOptionalComponent(new HttpHeaderComponent("x-missing"));

            await middleware.InvokeAsync(httpContext, CountingMiddleware);
            Assert.Equal(1, numCallsToNext);

            await callback(state);

            mockRespFeature.Verify(f => f.OnStarting(It.IsAny<Func<object, Task>>(), It.IsAny<object>()), Times.Once);
            mockSigner.Verify(s => s.SignAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
            mockSigner.Verify(s => s.UpdateSignatureParams(It.IsAny<SignatureParamsComponent>()),
                useUpdateSignatureParams ? Times.Once : Times.Never);
        }

        private Task CountingMiddleware(HttpContext context)
        {
            Interlocked.Increment(ref numCallsToNext);
            return Task.CompletedTask;
        }
    }
}
