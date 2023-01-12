using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using NSign.Signatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.AspNetCore
{
    public sealed class RequestMessageContextTests
    {
        private readonly DefaultHttpContext httpContext = new DefaultHttpContext();
        private readonly Mock<IHttpRequestTrailersFeature> mockRequestTrailers =
            new Mock<IHttpRequestTrailersFeature>(MockBehavior.Strict);
        private readonly SignatureVerificationOptions options = new SignatureVerificationOptions();
        private readonly RequestMessageContext context;
        private long numCallsToNext;

        public RequestMessageContextTests()
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>(MockBehavior.Loose);

            httpContext.Features.Set(mockRequestTrailers.Object);

            context = new RequestMessageContext(httpContext, options, CountingMiddleware, mockLogger.Object);
        }

        [Fact]
        public void VerificationOptionsArePassed()
        {
            Assert.Same(options, context.VerificationOptions);
        }

        [Fact]
        public void HasResponseIsFalse()
        {
            Assert.False(context.HasResponse);
        }

        [Fact]
        public void AbortedIsFromHttpContext()
        {
            using CancellationTokenSource source = new();
            httpContext.RequestAborted = source.Token;

            Assert.False(context.Aborted.IsCancellationRequested);
            source.Cancel();
            Assert.True(context.Aborted.IsCancellationRequested);
        }

        [Fact]
        public void AddHeaderThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => context.AddHeader("unit", "test"));
        }

        [Theory]
        [InlineData("@status", "The '@status' component value cannot be retrieved for request messages.")]
        [InlineData("@signature-params", "The '@signature-params' component value cannot be retrieved like this.")]
        [InlineData("@query-param", "The '@query-param' component value cannot be retrieved like this.")]
        [InlineData("@blah", "Non-standard derived signature component '@blah' cannot be retrieved.")]
        public void GetDerivedComponentValueThrowsNotSupportedExceptionForUnsupportedComponents(string compName, string expectedMessage)
        {
            DerivedComponent comp = new DerivedComponent(compName);
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => context.GetDerivedComponentValue(comp));

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData("@method", "PATCH")]
        [InlineData("@target-uri", "https://localhost:8443/base/path/path/to/resource?a=b&a=bb&c=d&e=")]
        [InlineData("@authority", "localhost:8443")]
        [InlineData("@scheme", "https")]
        [InlineData("@request-target", "/base/path/path/to/resource?a=b&a=bb&c=d&e=")]
        [InlineData("@path", "/base/path/path/to/resource")]
        [InlineData("@query", "?a=b&a=bb&c=d&e=")]
        public void GetDerivedComponentValueWorks(string compName, string? expectedValue)
        {
            httpContext.Request.Method = "PATCH";
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost", 8443);
            httpContext.Request.PathBase = "/base/path";
            httpContext.Request.Path = "/path/to/resource";
            httpContext.Request.QueryString = new QueryString("?a=b&a=bb&c=d&e=");

            DerivedComponent comp = new DerivedComponent(compName);
            string? actualValue = context.GetDerivedComponentValue(comp);

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [InlineData("not-found", new string[0])]
        [InlineData("x-first-header", new string[] { "firstValue" })]
        [InlineData("x-second-header", new string[] { "" })]
        [InlineData("x-third-header", new string[] { "1", "2", "3" })]
        public void GetHeaderValuesWorks(string header, string[] expectedValues)
        {
            httpContext.Request.Headers.Add("x-first-header", "firstValue");
            httpContext.Request.Headers.Add("x-Second-Header", "");
            httpContext.Request.Headers.Add("x-third-header", new StringValues(new string[] { "1", "2", "3", }));

            IEnumerable<string> actualValues = context.GetHeaderValues(header);

            Assert.Collection(actualValues,
                expectedValues
                    .Select(expectedVal => (Action<string>)((actualVal) => Assert.Equal(expectedVal, actualVal)))
                    .ToArray());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void GetHeaderValuesChecksContentLengthSpecialHeader(bool hasHeader, bool hasSpecial)
        {
            if (hasHeader)
            {
                httpContext.Request.Headers.Add("Content-Length", "123");
            }
            if (hasSpecial)
            {
                httpContext.Request.Headers.ContentLength = 234;
            }

            IEnumerable<string> actualValues = context.GetHeaderValues("content-length");

            if (hasSpecial)
            {
                Assert.Collection(actualValues, (actualVal) => Assert.Equal("234", actualVal));
            }
            else if (hasHeader)
            {
                Assert.Collection(actualValues, (actualVal) => Assert.Equal("123", actualVal));
            }
            else
            {
                Assert.Empty(actualValues);
            }
        }

        [Theory]
        [InlineData("x-first", "value1, value2")]
        [InlineData("x-second", "")]
        [InlineData("inexistent", null)]
        public void GetRequestHeaderValuesWorks(string headerName, string? expectedValue)
        {
            httpContext.Request.Headers.Add("x-first", "value1, value2");
            httpContext.Request.Headers.Add("x-second", "");

            IEnumerable<string>? actualValues = context.GetRequestHeaderValues(headerName);

            if (null != expectedValue)
            {
                Assert.Collection(actualValues, (val) => Assert.Equal(expectedValue, val));
            }
            else
            {
                Assert.Empty(actualValues);
            }
        }

        [Theory]
        [InlineData("not-found", new string[0])]
        [InlineData("x-first-header", new string[] { "firstValue" })]
        [InlineData("x-second-header", new string[] { "" })]
        [InlineData("x-third-header", new string[] { "1", "2", "3" })]
        public void GetTrailerValuesWorks(string name, string[] expectedValues)
        {
            HeaderDictionary trailers = new HeaderDictionary()
            {
                { "x-first-header", "firstValue" },
                { "x-Second-Header", "" },
                { "x-third-header", new StringValues(new string[] { "1", "2", "3", }) },
            };
            mockRequestTrailers.SetupGet(t => t.Trailers).Returns(trailers);

            IEnumerable<string> actualValues = context.GetTrailerValues(name);

            Assert.Collection(actualValues,
                expectedValues
                    .Select(expectedVal => (Action<string>)((actualVal) => Assert.Equal(expectedVal, actualVal)))
                    .ToArray());
        }

        [Theory]
        [InlineData("x-first", "value1, value2")]
        [InlineData("x-second", "")]
        [InlineData("inexistent", null)]
        public void GetRequestTrailerValuesWorks(string name, string? expectedValue)
        {
            HeaderDictionary trailers = new HeaderDictionary()
            {
                { "x-first", "value1, value2" },
                { "x-second", "" },
            };
            mockRequestTrailers.SetupGet(t => t.Trailers).Returns(trailers);

            IEnumerable<string>? actualValues = context.GetRequestTrailerValues(name);

            if (null != expectedValue)
            {
                Assert.Collection(actualValues, (val) => Assert.Equal(expectedValue, val));
            }
            else
            {
                Assert.Empty(actualValues);
            }
        }

        [Theory]
        [InlineData("not-found", new string[0])]
        [InlineData("a", new string[] { "b", "bb", "B", })]
        [InlineData("A", new string[] { "b", "bb", "B", })]
        [InlineData("c", new string[] { "d", })]
        [InlineData("e", new string[] { "", })]
        public void GetQueryParamValuesWorks(string name, string[] expectedValues)
        {
            httpContext.Request.QueryString = new QueryString("?a=b&a=bb&c=d&A=B&e=");

            IEnumerable<string> actualValues = context.GetQueryParamValues(name);

            Assert.Collection(actualValues,
                expectedValues
                    .Select(expectedVal => (Action<string>)((actualVal) => Assert.Equal(expectedVal, actualVal)))
                    .ToArray());
        }

        [Theory]
        [InlineData("not-found", false)]
        [InlineData("x-first-header", true)]
        [InlineData("x-second-header", true)]
        [InlineData("x-third-header", true)]
        public void HasHeaderWorks(string header, bool expectedResult)
        {
            httpContext.Request.Headers.Add("x-first-header", "firstValue");
            httpContext.Request.Headers.Add("x-Second-Header", "");
            httpContext.Request.Headers.Add("x-third-header", new StringValues(new string[] { "1", "2", "3", }));

            Assert.Equal(expectedResult, context.HasHeader(bindRequest: false, header));
        }

        [Theory]
        [InlineData("not-found", false)]
        [InlineData("x-first-header", true)]
        [InlineData("x-second-header", true)]
        [InlineData("x-third-header", true)]
        public void HasTrailerWorks(string name, bool expectedResult)
        {
            HeaderDictionary trailers = new HeaderDictionary()
            {
                { "x-first-header", "firstValue" },
                { "x-Second-Header", "" },
                { "x-third-header", new StringValues(new string[] { "1", "2", "3", }) },
            };
            mockRequestTrailers.SetupGet(t => t.Trailers).Returns(trailers);

            Assert.Equal(expectedResult, context.HasTrailer(bindRequest: false, name));
        }

        [Theory]
        [InlineData("not-found", false)]
        [InlineData("a", true)]
        [InlineData("A", true)]
        [InlineData("c", true)]
        [InlineData("e", true)]
        public void HasQueryParamWorks(string name, bool expectedResult)
        {
            httpContext.Request.QueryString = new QueryString("?a=b&a=bb&c=d&e=");

            Assert.Equal(expectedResult, context.HasQueryParam(name));
        }

        [Fact]
        public void MessageHeadersReturnsRequestHeaders()
        {
            PropertyInfo? prop = typeof(RequestMessageContext).GetProperty("MessageHeaders", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.Same(httpContext.Request.Headers, prop!.GetValue(context));
        }

        [Fact]
        public void TrailerReferencesThrowWhenTrailerFeatureNotAvailable()
        {
            httpContext.Features.Set<IHttpRequestTrailersFeature>(null);

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => context.HasTrailer(bindRequest: false, "xyx"));
            Assert.Equal("This request does not support trailers.", ex.Message);
        }

        private Task CountingMiddleware(HttpContext context)
        {
            Interlocked.Increment(ref numCallsToNext);
            return Task.CompletedTask;
        }
    }
}
