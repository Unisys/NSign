using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using NSign.Signatures;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace NSign.AspNetCore
{
    public sealed class RequestResponseMessageContextTests
    {
        private readonly DefaultHttpContext httpContext = new DefaultHttpContext();
        private readonly Mock<IHttpRequestTrailersFeature> mockRequestTrailers =
            new Mock<IHttpRequestTrailersFeature>(MockBehavior.Strict);
        private readonly Mock<IHttpResponseTrailersFeature> mockResponseTrailers =
            new Mock<IHttpResponseTrailersFeature>(MockBehavior.Strict);
        private readonly Mock<IMessageSigner> mockSigner;
        private readonly MessageSigningOptions options = new MessageSigningOptions();
        private readonly RequestResponseMessageContext context;

        public RequestResponseMessageContextTests()
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>(MockBehavior.Loose);
            mockSigner = new Mock<IMessageSigner>(MockBehavior.Strict);

            httpContext.Features.Set(mockRequestTrailers.Object);
            httpContext.Features.Set(mockResponseTrailers.Object);

            context = new RequestResponseMessageContext(mockSigner.Object, httpContext, options, mockLogger.Object);
        }

        [Fact]
        public void VerificationOptionsAreNull()
        {
            Assert.Null(context.VerificationOptions);
        }

        [Fact]
        public void SigningOptionsArePassed()
        {
            Assert.Same(options, context.SigningOptions);
        }

        [Fact]
        public void HasResponseIsTrue()
        {
            Assert.True(context.HasResponse);
        }

        [Fact]
        public async Task OnResponseStartingAsyncInvokesSigner()
        {
            mockSigner.Setup(s => s.SignMessageAsync(context)).Returns(Task.CompletedTask);

            await context.OnResponseStartingAsync();

            mockSigner.Verify(s => s.SignMessageAsync(context), Times.Once());
        }

        [Fact]
        public void AddHeaderAddsResponseHeader()
        {
            string headerName = $"x-unit-test-{Random.Shared.Next()}";
            string headerVal = $"random; value={Random.Shared.Next()}";

            Assert.False(httpContext.Response.Headers.ContainsKey(headerName));

            context.AddHeader(headerName, headerVal);

            Assert.True(httpContext.Response.Headers.TryGetValue(headerName, out StringValues values));
            Assert.Collection(values, (val) => Assert.Equal(headerVal, val));
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
        [InlineData("@method", "PUT")]
        [InlineData("@target-uri", "https://localhost:8443/base/path/path/to/resource?a=b&a=bb&c=d&e=")]
        [InlineData("@authority", "localhost:8443")]
        [InlineData("@scheme", "https")]
        [InlineData("@request-target", "/base/path/path/to/resource?a=b&a=bb&c=d&e=")]
        [InlineData("@path", "/base/path/path/to/resource")]
        [InlineData("@query", "?a=b&a=bb&c=d&e=")]
        [InlineData("@status", "409")]
        public void GetDerivedComponentValueWorks(string compName, string? expectedValue)
        {
            httpContext.Request.Method = "PUT";
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost", 8443);
            httpContext.Request.PathBase = "/base/path";
            httpContext.Request.Path = "/path/to/resource";
            httpContext.Request.QueryString = new QueryString("?a=b&a=bb&c=d&e=");

            httpContext.Response.StatusCode = 409;

            DerivedComponent comp = new DerivedComponent(compName);
            string? actualValue = context.GetDerivedComponentValue(comp);

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void MessageHeadersReturnsResponseHeaders()
        {
            PropertyInfo? prop = typeof(RequestMessageContext).GetProperty("MessageHeaders", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.Same(httpContext.Response.Headers, prop!.GetValue(context));
        }

        [Fact]
        public void MessageTrailersReturnsResponseTrailers()
        {
            HeaderDictionary trailers = new HeaderDictionary()
            {
                { "x-first-header", "firstValue" },
                { "x-Second-Header", "" },
                { "x-third-header", new StringValues(new string[] { "1", "2", "3", }) },
            };
            mockResponseTrailers.SetupGet(t => t.Trailers).Returns(trailers);

            PropertyInfo? prop = typeof(RequestMessageContext).GetProperty("MessageTrailers", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.Same(trailers, prop!.GetValue(context));
        }

        [Fact]
        public void MessageTrailersThrowsWhenFeatureNotAvailable()
        {
            httpContext.Features.Set<IHttpResponseTrailersFeature>(null);

            PropertyInfo? prop = typeof(RequestMessageContext).GetProperty("MessageTrailers", BindingFlags.Instance | BindingFlags.NonPublic);

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => context.HasTrailer(bindRequest: false, "blah"));
            Assert.Equal("Trailers are not supported for this response.", ex.Message);
        }
    }
}
