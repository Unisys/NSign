using Microsoft.Extensions.Logging;
using Moq;
using NSign.Http;
using NSign.Signatures;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly HttpFieldOptions httpFieldOptions = new HttpFieldOptions();
        private readonly SignatureVerificationOptions verificationOptions = new SignatureVerificationOptions();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly HttpResponseMessageContext context;

        public HttpResponseMessageContextTests()
        {
            context = new HttpResponseMessageContext(mockLogger.Object,
                                                     httpFieldOptions,
                                                     request,
                                                     response,
                                                     cancellationTokenSource.Token,
                                                     verificationOptions);
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

        [Theory]
        [InlineData("x-resp-header", false)]
        [InlineData("x-first-header", true)]
        [InlineData("x-second-header", true)]
        [InlineData("x-third-header", true)]
        public void HasHeaderWorks(string header, bool expectOnRequest)
        {
            request.Headers.Add("x-first-header", "firstValue");
            request.Headers.Add("x-Second-Header", "");
            request.Headers.Add("x-third-header", new string[] { "1", "2", "3", });
            response.Headers.Add("x-resp-header", "blah");

            Assert.Equal(!expectOnRequest, context.HasHeader(bindRequest: false, header));
            Assert.Equal(expectOnRequest, context.HasHeader(bindRequest: true, header));
        }

#if TestTargetsNetStandard
        [Theory]
        [InlineData("a")]
        [InlineData("header-one")]
        [InlineData("x-header-two")]
        public void GetTrailerValuesThrows(string name)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(
                () => context.GetTrailerValues(name));
            Assert.Equal(
                "Trailers are not supported with .netstandard 2.0. Please update to a more modern version.",
                ex.Message);
        }
#else
        [Theory]
        [InlineData("x-first", new string[] { "value1", "value2", })]
        [InlineData("x-second", new string[] { "", })]
        [InlineData("inexistent", null)]
        public void GetTrailerValuesWorks(string name, string[]? expectedValue)
        {
            response.TrailingHeaders.Add("x-first", new String[] { "value1", "value2", });
            response.TrailingHeaders.Add("x-second", "");

            IEnumerable<string>? actualValues = context.GetTrailerValues(name);

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
#endif

#if TestTargetsNetStandard
        [Theory]
        [InlineData("not-found")]
        [InlineData("x-first-header")]
        [InlineData("x-second-header")]
        [InlineData("x-third-header")]
        public void HasTrailerThrowsWhenNotBindingToRequest(string header)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(
                () => context.HasTrailer(bindRequest: false, header));
            Assert.Equal(
                "Trailers are not supported with .netstandard 2.0. Please update to a more modern version.",
                ex.Message);
        }
#else
        [Theory]
        [InlineData("not-found", false)]
        [InlineData("x-first-header", true)]
        [InlineData("x-second-header", true)]
        [InlineData("x-third-header", true)]
        public void HasTrailerWorks(string header, bool expectOnResponse)
        {
            response.TrailingHeaders.Add("x-first-header", "firstValue");
            response.TrailingHeaders.Add("x-Second-Header", "");
            response.TrailingHeaders.Add("x-third-header", new string[] { "1", "2", "3", });

            Assert.Equal(expectOnResponse, context.HasTrailer(bindRequest: false, header));
            Assert.False(context.HasTrailer(bindRequest: true, header));
        }
#endif

        [Theory]
        [InlineData("not-found")]
        [InlineData("x-first-header")]
        [InlineData("x-second-header")]
        [InlineData("x-third-header")]
        public void GetRequestTrailerValuesThrowsWhenBindingToRequest(string header)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => context.GetRequestTrailerValues(header));
            Assert.Equal("Request-based trailers in signatures are not supported.", ex.Message);
        }

#if TestTargetsNetStandard
        [Theory]
        [InlineData("not-found")]
        [InlineData("x-first-header")]
        [InlineData("x-second-header")]
        [InlineData("x-third-header")]
        public void HasTrailerThrowsWhenBindingToRequest(string header)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(
                () => context.HasTrailer(bindRequest: true, header));
            Assert.Equal(
                "Trailers are not supported with .netstandard 2.0. Please update to a more modern version.",
                ex.Message);
        }
#else
        [Theory]
        [InlineData("not-found")]
        [InlineData("x-first-header")]
        [InlineData("x-second-header")]
        [InlineData("x-third-header")]
        public void HasTrailerReturnsFalseWhenBindingToRequest(string header)
        {
            Assert.False(context.HasTrailer(bindRequest: true, header));
        }
#endif
    }
}
