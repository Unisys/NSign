using Microsoft.Extensions.Logging;
using Moq;
using NSign.Signatures;
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using Xunit;

namespace NSign.Client
{
    public sealed class HttpResponseMessageContextTests : IDisposable
    {
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>(MockBehavior.Strict);
        private readonly HttpRequestMessage request = new HttpRequestMessage(
            HttpMethod.Options, "https://localhost:8443/blah/blotz?a=b&a=c&A=B&d=");
        private readonly HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.RedirectKeepVerb);
        private readonly SignatureVerificationOptions verificationOptions = new SignatureVerificationOptions();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly HttpResponseMessageContext context;

        public HttpResponseMessageContextTests()
        {
            context = new HttpResponseMessageContext(
                mockLogger.Object, request, response, cancellationTokenSource.Token, verificationOptions);
        }

        public void Dispose()
        {
            cancellationTokenSource.Dispose();
            request.Dispose();
            response.Dispose();
        }

        [Fact]
        public void RequestAndResponseArePassed()
        {
            Assert.Same(request, context.Request);
            Assert.Same(response, context.Response);
        }

        [Fact]
        public void HasResponseIsTrue()
        {
            Assert.True(context.HasResponse);
        }

        [Fact]
        public void SigningOptionsIsNull()
        {
            Assert.Null(context.SigningOptions);
        }

        [Fact]
        public void VerificationOptionsIsPassed()
        {
            Assert.Same(verificationOptions, context.VerificationOptions);
        }

        [Fact]
        public void AddHeaderThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => context.AddHeader("a", "b"));
        }

        [Theory]
        [InlineData("@blotz", "Non-standard derived signature component '@blotz' cannot be retrieved.")]
        public void GetDerivedComponentValueThrowsNotSupportedExceptionForUnsupportedComponents(string compName, string expectedMessage)
        {
            DerivedComponent comp = new DerivedComponent(compName);
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => context.GetDerivedComponentValue(comp));

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData("@method", "OPTIONS")]
        [InlineData("@target-uri", "https://localhost:8443/blah/blotz?a=b&a=c&A=B&d=")]
        [InlineData("@authority", "localhost:8443")]
        [InlineData("@scheme", "https")]
        [InlineData("@request-target", "/blah/blotz?a=b&a=c&A=B&d=")]
        [InlineData("@path", "/blah/blotz")]
        [InlineData("@query", "?a=b&a=c&A=B&d=")]
        [InlineData("@status", "307")]
        public void GetDerivedComponentValueWorks(string compName, string? expectedValue)
        {
            DerivedComponent comp = new DerivedComponent(compName);
            string? actualValue = context.GetDerivedComponentValue(comp);

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void MessageHeadersReturnsResponseHeaders()
        {
            PropertyInfo? prop = typeof(HttpRequestMessageContext).GetProperty("MessageHeaders", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.Same(response.Headers, prop!.GetValue(context));
        }

        [Fact]
        public void MessageContentReturnsResponseHeaders()
        {
            StringContent content = new StringContent("MessageContentReturnsRequestContent");
            response.Content = content;
            PropertyInfo? prop = typeof(HttpRequestMessageContext).GetProperty("MessageContent", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.Same(response.Content, prop!.GetValue(context));
        }
    }
}
