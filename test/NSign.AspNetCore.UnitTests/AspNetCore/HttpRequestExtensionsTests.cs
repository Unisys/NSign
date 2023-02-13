using Microsoft.AspNetCore.Http;
using NSign.Signatures;
using System;
using Xunit;

namespace NSign.AspNetCore
{
    public sealed class HttpRequestExtensionsTests
    {
        private readonly DefaultHttpContext httpContext = new DefaultHttpContext();

        [Theory]
        [InlineData("@signature-params", "The '@signature-params' component value cannot be retrieved like this.")]
        [InlineData("@query-param", "The '@query-param' component value cannot be retrieved like this.")]
        [InlineData("@status", "The '@status' component value cannot be retrieved for request messages.")]
        [InlineData("@blah", "Non-standard derived signature component '@blah' cannot be retrieved.")]
        public void GetDerivedComponentValueThrowsForUnsupportedComponents(string name, string expectedMessage)
        {
            DerivedComponent comp = new DerivedComponent(name);
            NotSupportedException ex = Assert.Throws<NotSupportedException>(
                () => httpContext.Request.GetDerivedComponentValue(comp));

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData("@method", "PATCH")]
        [InlineData("@target-uri", "https://localhost:8443/blah/blotz/blimp?a=b&c=")]
        [InlineData("@authority", "localhost:8443")]
        [InlineData("@scheme", "https")]
        [InlineData("@request-target", "/blah/blotz/blimp?a=b&c=")]
        [InlineData("@path", "/blah/blotz/blimp")]
        [InlineData("@query", "?a=b&c=")]
        public void GetDerivedComponentValueReturnsComponentValue(string name, string expectedValue)
        {
            HttpRequest? request = httpContext.Request;
            request.Method = "PATCH";
            request.Scheme = "https";
            request.Host = new HostString("localhost", 8443);
            request.PathBase = "/blah";
            request.Path = "/blotz/blimp";
            request.QueryString = new QueryString("?a=b&c=");

            DerivedComponent comp = new DerivedComponent(name);
            string actualValue = httpContext.Request.GetDerivedComponentValue(comp);

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [InlineData("https", 443, "localhost")]
        [InlineData("http", 80, "localhost")]
        [InlineData("https", null, "localhost")]
        [InlineData("http", null, "localhost")]
        [InlineData("https", 8443, "localhost:8443")]
        [InlineData("http", 8080, "localhost:8080")]
        public void GetDerivedComponentValueReturnsNormalizedAuthority(string scheme, int? port, string expectedValue)
        {
            HttpRequest? request = httpContext.Request;
            request.Scheme = scheme;
            request.Host = port.HasValue ? new HostString("localhost", port.Value) : new HostString("localhost");

            DerivedComponent comp = new DerivedComponent("@authority");
            string actualValue = httpContext.Request.GetDerivedComponentValue(comp);

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [InlineData(null, "?")]
        [InlineData("", "?")]
        [InlineData("?", "?")]
        [InlineData("?a", "?a")]
        public void GetDerivedComponentValueAlwaysHasQuestionMarkInQuery(string? queryStringInput, string expectedQuery)
        {
            HttpRequest? request = httpContext.Request;
            request.QueryString = new QueryString(queryStringInput);

            string actualValue = httpContext.Request.GetDerivedComponentValue(SignatureComponent.Query);

            Assert.Equal(expectedQuery, actualValue);
        }
    }
}
