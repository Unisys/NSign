using NSign.Signatures;
using System;
using System.Net.Http;
using Xunit;

namespace NSign.Client
{
    public sealed class HttpRequestMessageExtensionsTests
    {
        private readonly HttpRequestMessage request = new HttpRequestMessage(
            HttpMethod.Patch, "https://localhost:8443/blah/blotz/blimp?a=b&c=");

        [Theory]
        [InlineData("@signature-params", "The '@signature-params' component cannot be included explicitly.")]
        [InlineData("@query-param", "The '@query-param' component must have the 'name' parameter set.")]
        [InlineData("@status", "The '@status' component cannot be included in request signatures.")]
        [InlineData("@blah", "Non-standard derived signature component '@blah' cannot be retrieved.")]
        public void GetDerivedComponentValueThrowsForUnsupportedComponents(string name, string expectedMessage)
        {
            DerivedComponent comp = new DerivedComponent(name);
            NotSupportedException ex = Assert.Throws<NotSupportedException>(
                () => request.GetDerivedComponentValue(comp));

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
            DerivedComponent comp = new DerivedComponent(name);
            string actualValue = request.GetDerivedComponentValue(comp);

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [InlineData(null, "?")]
        [InlineData("", "?")]
        [InlineData("?", "?")]
        [InlineData("?a", "?a")]
        public void GetDerivedComponentValueAlwaysHasQuestionMarkInQuery(string? queryStringInput, string expectedQuery)
        {
            request.RequestUri = new Uri("https://localhost:8443/" + queryStringInput);

            string actualValue = request.GetDerivedComponentValue(SignatureComponent.Query);

            Assert.Equal(expectedQuery, actualValue);
        }
    }
}
