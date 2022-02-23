using Microsoft.AspNetCore.Http;
using NSign.Signatures;
using System;
using Xunit;

namespace NSign.AspNetCore
{
    public partial class HttpRequestExtensionsTests
    {
        private readonly DefaultHttpContext httpContext = new DefaultHttpContext();

        [Theory]
        [InlineData("@signature-params", "The '@signature-params' component value cannot be retrieved like this.")]
        [InlineData("@query-params", "The '@query-params' component value cannot be retrieved like this.")]
        [InlineData("@status", "The '@status' component value cannot be retrieved for request messages.")]
        [InlineData("@request-response", "The '@request-response' component value cannot be retrieved for request messages.")]
        [InlineData("@blah", "Non-standard derived signature component '@blah' cannot be retrieved.")]
        public void GetDerivedComponentValueThrowsForUnsupportedComponents(string name, string expectedMessage)
        {
            DerivedComponent comp = new DerivedComponent(name);
            NotSupportedException ex = Assert.Throws<NotSupportedException>(
                () => httpContext.Request.GetDerivedComponentValue(comp));

            Assert.Equal(expectedMessage, ex.Message);
        }
    }
}
