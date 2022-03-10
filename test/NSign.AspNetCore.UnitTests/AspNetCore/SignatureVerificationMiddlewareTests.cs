using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NSign.Signatures;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.AspNetCore
{
    public sealed class SignatureVerificationMiddlewareTests
    {
        private readonly DefaultHttpContext httpContext = new DefaultHttpContext();
        private readonly Mock<IVerifier> mockVerifier = new Mock<IVerifier>(MockBehavior.Strict);
        private readonly RequestSignatureVerificationOptions options = new RequestSignatureVerificationOptions();
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
                new OptionsWrapper<RequestSignatureVerificationOptions>(options));
        }

        [Fact]
        public async Task InvokeAsyncRespondsWithMissingSignatureResponseStatusWhenNoSignaturesArePresent()
        {
            options.MissingSignatureResponseStatus = 444;
            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(444, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));

            // Signature present but SignaturesToVerify option empty.
            options.MissingSignatureResponseStatus = 456;
            httpContext.Request.Headers.Add("signature", "test=:Test:");

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(456, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));

            // Signature and input present but SignaturesToVerify option empty.
            options.MissingSignatureResponseStatus = 456;
            httpContext.Request.Headers.Add("signature-input", "test=()");

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(456, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
        }

        [Theory]
        [InlineData("another=()")] // Input spec 'unittest' not found.
        [InlineData("unittest=(")] // Input spec is malformed.
        [InlineData("unittest=(\"@method\")")] // Component 'content-length' is missing in spec.
        public async Task InvokeAsyncRespondsWithSignatureInputErrorResponseStatusOnSignatureInputIssues(string inputHeader)
        {
            options.SignaturesToVerify.Add("unittest");
            options.CreatedRequired = options.ExpiresRequired = false;
            options.SignatureInputErrorResponseStatus = 468;
            options.RequiredSignatureComponents.Add(SignatureComponent.ContentType);

            httpContext.Request.Headers.Add("signature", "unittest=:Test:");
            httpContext.Request.Headers.Add("signature-input", inputHeader);

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(468, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
        }

        [Fact]
        public async Task InvokeAsyncRespondsWithSignatureInputErrorResponseStatusOnMandatoryButAbsentCreated()
        {
            options.SignaturesToVerify.Add("unittest");
            options.CreatedRequired = true;
            options.SignatureInputErrorResponseStatus = 468;
            options.RequiredSignatureComponents.Add(SignatureComponent.ContentType);

            httpContext.Request.Headers.Add("signature", "unittest=:Test:");
            httpContext.Request.Headers.Add("signature-input", "unittest=()");

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(468, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
        }

        [Fact]
        public async Task InvokeAsyncRespondsWithSignatureInputErrorResponseStatusOnMandatoryButAbsentExpires()
        {
            options.SignaturesToVerify.Add("unittest");
            options.ExpiresRequired = true;
            options.MaxSignatureAge = null;
            options.SignatureInputErrorResponseStatus = 468;
            options.RequiredSignatureComponents.Add(SignatureComponent.ContentType);

            httpContext.Request.Headers.Add("signature", "unittest=:Test:");
            httpContext.Request.Headers.Add("signature-input", "unittest=();created=1234");

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(468, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
        }

        [Fact]
        public async Task InvokeAsyncRespondsWithSignatureInputErrorResponseStatusOnMandatoryButAbsentNonce()
        {
            options.SignaturesToVerify.Add("unittest");
            options.ExpiresRequired = false;
            options.NonceRequired = true;
            options.MaxSignatureAge = null;
            options.SignatureInputErrorResponseStatus = 468;
            options.RequiredSignatureComponents.Add(SignatureComponent.ContentType);

            httpContext.Request.Headers.Add("signature", "unittest=:Test:");
            httpContext.Request.Headers.Add("signature-input", "unittest=();created=1234");

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(468, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
        }

        [Fact]
        public async Task InvokeAsyncRespondsWithSignatureInputErrorResponseStatusOnFailedNonceValidation()
        {
            bool nonceVerificationCalled = false;

            options.SignaturesToVerify.Add("unittest");
            options.ExpiresRequired = false;
            options.VerifyNonce = (input) =>
            {
                Assert.Equal("aaa555", input.SignatureParameters.Nonce);
                nonceVerificationCalled = true;
                return false;
            };
            options.MaxSignatureAge = null;
            options.SignatureInputErrorResponseStatus = 468;
            options.RequiredSignatureComponents.Add(SignatureComponent.ContentType);

            httpContext.Request.Headers.Add("signature", "unittest=:Test:");
            httpContext.Request.Headers.Add("signature-input", "unittest=();created=1234;nonce=\"aaa555\"");

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(468, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
            Assert.True(nonceVerificationCalled);
        }

        [Fact]
        public async Task InvokeAsyncRespondsWithSignatureInputErrorResponseStatusOnMandatoryButAbsentAlgorithm()
        {
            options.SignaturesToVerify.Add("unittest");
            options.ExpiresRequired = false;
            options.AlgorithmRequired = true;
            options.MaxSignatureAge = null;
            options.SignatureInputErrorResponseStatus = 468;
            options.RequiredSignatureComponents.Add(SignatureComponent.ContentType);

            httpContext.Request.Headers.Add("signature", "unittest=:Test:");
            httpContext.Request.Headers.Add("signature-input", "unittest=();created=1234");

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(468, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
        }

        [Fact]
        public async Task InvokeAsyncRespondsWithSignatureInputErrorResponseStatusOnMandatoryButAbsentKeyId()
        {
            options.SignaturesToVerify.Add("unittest");
            options.ExpiresRequired = false;
            options.KeyIdRequired = true;
            options.MaxSignatureAge = null;
            options.SignatureInputErrorResponseStatus = 468;
            options.RequiredSignatureComponents.Add(SignatureComponent.ContentType);

            httpContext.Request.Headers.Add("signature", "unittest=:Test:");
            httpContext.Request.Headers.Add("signature-input", "unittest=();created=1234");

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(468, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task InvokeAsyncRespondsWithVerificationErrorResponseStatusOnExpiredSignature(int? maxAgeSeconds)
        {
            TimeSpan? maxSignatureAge = maxAgeSeconds.HasValue ? TimeSpan.FromSeconds(maxAgeSeconds.Value) : (TimeSpan?)null;
            DateTimeOffset now = DateTimeOffset.Now;

            options.SignaturesToVerify.Add("unittest");
            options.CreatedRequired = options.ExpiresRequired = false;
            options.MaxSignatureAge = maxSignatureAge;
            options.VerificationErrorResponseStatus = 987;

            httpContext.Request.Headers.Add("signature", "unittest=:Test:");
            httpContext.Request.Headers.Add("signature-input",
                $"unittest=(\"@method\");created={now.ToUnixTimeSeconds()};expires={now.AddHours(5).ToUnixTimeSeconds()}");

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(987, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
        }

        [Fact]
        public async Task InvokeAsyncForwardsToNextMiddlewareIfVerificationSucceeds()
        {
            bool nonceVerified = false;
            DateTimeOffset now = DateTimeOffset.Now;

            options.SignaturesToVerify.Add("unittest");
            options.RequiredSignatureComponents.Add(SignatureComponent.Method);
            options.VerifyNonce = (inputSpec) =>
            {
                Assert.Equal("test", inputSpec.SignatureParameters.Nonce);
                nonceVerified = true;
                return true;
            };

            httpContext.Request.Headers.Add("signature", "unittest=:Test:");
            httpContext.Request.Headers.Add("signature-input",
                $"unittest=(\"@method\");nonce=\"test\";created={now.ToUnixTimeSeconds()};expires={now.AddSeconds(5).ToUnixTimeSeconds()}");

            mockVerifier
                .Setup(v => v.VerifyAsync(
                    It.IsAny<SignatureParamsComponent>(), It.IsAny<byte[]>(), It.Is<byte[]>(bytes => VerifyBytes(bytes, 0x4d, 0xeb, 0x2d)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(VerificationResult.SuccessfullyVerified);

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(200, httpContext.Response.StatusCode);
            Assert.Equal(1, Interlocked.Read(ref numCallsToNext));
            Assert.True(nonceVerified);

            mockVerifier.Verify(v => v.VerifyAsync(
                It.IsAny<SignatureParamsComponent>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsyncThrowsIfVerifierThrows()
        {
            DateTimeOffset now = DateTimeOffset.Now;

            options.SignaturesToVerify.Add("unittest");
            httpContext.Request.Headers.Add("signature", "unittest=:Test:");
            httpContext.Request.Headers.Add("signature-input",
                $"unittest=(\"@method\");created={now.ToUnixTimeSeconds()};expires={now.AddSeconds(5).ToUnixTimeSeconds()}");

            mockVerifier
                .Setup(v => v.VerifyAsync(
                    It.IsAny<SignatureParamsComponent>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Injected error"));

            Exception ex = await Assert.ThrowsAsync<Exception>(() => middleware.InvokeAsync(httpContext, CountingMiddleware));

            Assert.Equal("Injected error", ex.Message);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));

            mockVerifier.Verify(v => v.VerifyAsync(
                It.IsAny<SignatureParamsComponent>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private Task CountingMiddleware(HttpContext context)
        {
            Interlocked.Increment(ref numCallsToNext);
            return Task.CompletedTask;
        }

        private bool VerifyBytes(byte[] actual, params byte[] expected)
        {
            if (actual.Length != expected.Length)
            {
                return false;
            }

            return actual.SequenceEqual(expected);
        }
    }
}
