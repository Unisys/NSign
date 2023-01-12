using Microsoft.Extensions.Logging;
using Moq;
using NSign.Signatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using Xunit;

namespace NSign.Client
{
    public sealed class HttpRequestMessageContextTests : IDisposable
    {
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>(MockBehavior.Strict);
        private readonly HttpRequestMessage request = new HttpRequestMessage(
            HttpMethod.Options, "https://localhost:8443/blah/blotz?a=b&a=c&A=B&d=");
        private readonly MessageSigningOptions signingOptions = new MessageSigningOptions();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly HttpRequestMessageContext context;

        public HttpRequestMessageContextTests()
        {
            context = new HttpRequestMessageContext(
                mockLogger.Object, request, cancellationTokenSource.Token, signingOptions);
        }

        public void Dispose()
        {
            cancellationTokenSource.Dispose();
            request.Dispose();
        }

        [Fact]
        public void RequestIsPassed()
        {
            Assert.Same(request, context.Request);
        }

        [Fact]
        public void HasResponseIsFalse()
        {
            Assert.False(context.HasResponse);
        }

        [Fact]
        public void AbortIsPassed()
        {
            Assert.Equal(cancellationTokenSource.Token, context.Aborted);
        }

        [Fact]
        public void SigningOptionsIsPassed()
        {
            Assert.Same(signingOptions, context.SigningOptions);
        }

        [Theory]
        [InlineData("x-first", new string[] { "a", "b", "c", })]
        [InlineData("x-second", new string[] { "value", })]
        public void AddHeaderAddsHeader(string name, string[] values)
        {
            foreach (string value in values)
            {
                context.AddHeader(name, value);
            }

            Assert.True(request.Headers.TryGetValues(name, out IEnumerable<string>? actualValues));
            Assert.Collection(actualValues,
                values.Select((val) => (Action<string>)((actualValue) => Assert.Equal(val, actualValue))).ToArray());
        }

        [Theory]
        [InlineData("@status", "The '@status' component cannot be included in request signatures.")]
        [InlineData("@signature-params", "The '@signature-params' component cannot be included explicitly.")]
        [InlineData("@query-param", "The '@query-param' component must have the 'name' parameter set.")]
        [InlineData("@blah", "Non-standard derived signature component '@blah' cannot be retrieved.")]
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
        public void GetDerivedComponentValueWorks(string compName, string? expectedValue)
        {
            DerivedComponent comp = new DerivedComponent(compName);
            string? actualValue = context.GetDerivedComponentValue(comp);

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [InlineData("not-found", new string[0])]
        [InlineData("x-first-header", new string[] { "firstValue" })]
        [InlineData("x-second-header", new string[] { "" })]
        [InlineData("x-third-header", new string[] { "1", "2", "3" })]
        [InlineData("x-content-header", new string[] { "blah" })]
        public void GetHeaderValuesWorks(string header, string[] expectedValues)
        {
            request.Headers.Add("x-first-header", "firstValue");
            request.Headers.Add("x-Second-Header", "");
            request.Headers.Add("x-third-header", new string[] { "1", "2", "3", });

            request.Content = new StringContent("blah");
            request.Content.Headers.Add("x-Content-Header", "blah");

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
        public void GetHeaderValuesChecksContentLengthOnContent(bool hasContent, bool hasContentLength)
        {
            if (hasContent)
            {
                request.Content = new StringContent("UnitTest");
                if (!hasContentLength)
                {
                    request.Content.Headers.ContentLength = null;
                }
            }

            IEnumerable<string> actualValues = context.GetHeaderValues("content-length");

            if (hasContent && hasContentLength)
            {
                Assert.Collection(actualValues, (actualVal) => Assert.Equal("8", actualVal));
            }
            else
            {
                Assert.Empty(actualValues);
            }
        }

        [Theory]
        [InlineData("x-first", new string[] { "value1", "value2", })]
        [InlineData("x-second", new string[] { "", })]
        [InlineData("inexistent", null)]
        public void GetRequestHeaderValuesWorks(string headerName, string[]? expectedValue)
        {
            request.Headers.Add("x-first", new String[] { "value1", "value2", });
            request.Headers.Add("x-second", "");

            IEnumerable<string>? actualValues = context.GetRequestHeaderValues(headerName);

            if (null != expectedValue)
            {
                Assert.Collection(actualValues,
                    expectedValue.Select((val) => (Action<string>)((actualVal) => Assert.Equal(val, actualVal))).ToArray());
            }
            else
            {
                Assert.Empty(actualValues);
            }
        }

        [Fact]
        public void GetTrailerValuesThrows()
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => context.GetTrailerValues("blah"));
            Assert.Equal("Trailers in signatures are not supported for request messages.", ex.Message);
        }

        [Fact]
        public void GetRequestTrailerValuesThrows()
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => context.GetRequestTrailerValues("blah"));
            Assert.Equal("Request-based trailers in signatures are not supported.", ex.Message);
        }

        [Theory]
        [InlineData("not-found", new string[0])]
        [InlineData("a", new string[] { "b", "bb", "B" })]
        [InlineData("A", new string[] { "b", "bb", "B" })]
        [InlineData("c", new string[] { "d", })]
        [InlineData("e", new string[] { "", })]
        public void GetQueryParamValuesWorks(string name, string[] expectedValues)
        {
            request.RequestUri = new Uri("http://localhost/blah/?a=b&a=bb&A=B&c=d&e=");

            IEnumerable<string> actualValues = context.GetQueryParamValues(name);

            Assert.Collection(actualValues,
                expectedValues
                    .Select(expectedVal => (Action<string>)((actualVal) => Assert.Equal(expectedVal, actualVal)))
                    .ToArray());
        }

        [Fact]
        public void GetQueryParamValuesWorksOnEmptyQuery()
        {
            request.RequestUri = new Uri("http://localhost/blah/");

            Assert.NotNull(context.GetQueryParamValues("x"));
            Assert.Empty(context.GetQueryParamValues("x"));
        }

        [Theory]
        [InlineData("not-found", false)]
        [InlineData("x-first-header", true)]
        [InlineData("x-second-header", true)]
        [InlineData("x-third-header", true)]
        public void HasHeaderWorks(string header, bool expectedResult)
        {
            request.Headers.Add("x-first-header", "firstValue");
            request.Headers.Add("x-Second-Header", "");
            request.Headers.Add("x-third-header", new string[] { "1", "2", "3", });

            Assert.Equal(expectedResult, context.HasHeader(bindRequest: false, header));
            Assert.Equal(expectedResult, context.HasHeader(bindRequest: true, header));
        }

        [Theory]
        [InlineData("not-found", false)]
        [InlineData("a", true)]
        [InlineData("A", true)]
        [InlineData("c", true)]
        [InlineData("e", true)]
        [InlineData("E", true)]
        public void HasQueryParamWorks(string name, bool expectedResult)
        {
            request.RequestUri = new Uri("http://localhost:8080/?a=b&a=bb&c=d&A=B&e=");

            Assert.Equal(expectedResult, context.HasQueryParam(name));
        }

        [Fact]
        public void MessageHeadersReturnsRequestHeaders()
        {
            PropertyInfo? prop = typeof(HttpRequestMessageContext).GetProperty("MessageHeaders", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.Same(request.Headers, prop!.GetValue(context));
        }

        [Fact]
        public void MessageContentReturnsRequestContent()
        {
            StringContent content = new StringContent("MessageContentReturnsRequestContent");
            request.Content = content;
            PropertyInfo? prop = typeof(HttpRequestMessageContext).GetProperty("MessageContent", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.Same(content, prop!.GetValue(context));
        }

        [Theory]
        [InlineData("not-found")]
        [InlineData("x-first-header")]
        [InlineData("x-second-header")]
        [InlineData("x-third-header")]
        public void HasTrailerAlwaysReturnsFalse(string name)
        {
            Assert.False(context.HasTrailer(bindRequest: false, name));
        }
    }
}
