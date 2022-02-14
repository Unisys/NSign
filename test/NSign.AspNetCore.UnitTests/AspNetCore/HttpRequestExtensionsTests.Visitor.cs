using Microsoft.AspNetCore.Http;
using NSign.Signatures;
using System;
using System.Text;
using Xunit;

namespace NSign.AspNetCore
{
    partial class HttpRequestExtensionsTests
    {
        [Fact]
        public void VisitorThrowsForSignatureParamsComponentWithoutOriginalValue()
        {
            SignatureInputSpec inputSpec = new SignatureInputSpec("test");
            InvalidOperationException ex;

            ex = Assert.Throws<InvalidOperationException>(() => httpContext.Request.GetSignatureInput(inputSpec));

            Assert.Equal("Signature input can only be created for SignatureParamsComponents received from HTTP requests.", ex.Message);
        }

        [Fact]
        public void EmptyParamsProducesMinimalSignatureInput()
        {
            SignatureInputSpec inputSpec = new SignatureInputSpec("test", @"()");
            byte[] input = httpContext.Request.GetSignatureInput(inputSpec);

            Assert.Equal(@"""@signature-params"": ()", GetString(input));
        }

        [Theory]
        [InlineData(@"(""x-missing"")", @"The signature component 'x-missing' does not exist but is required.")]
        [InlineData(@"(""x-missing"";key=""foo"")", @"The signature component 'x-missing;key=""foo""' does not exist but is required.")]
        [InlineData(@"(""x-dict"";key=""foo"")", @"The signature component 'x-dict;key=""foo""' does not exist but is required.")]
        [InlineData(@"(""x-non-dict"";key=""foo"")", @"The signature component 'x-non-dict;key=""foo""' does not exist but is required.")]
        [InlineData(@"(""@query-params"";name=""x"")", @"The signature component '@query-params;name=""x""' does not exist but is required.")]
        public void VisitorThrowsForMissingHttpHeader(string input, string expectedExceptionMessage)
        {
            SignatureInputSpec inputSpec = new SignatureInputSpec("test", input);
            SignatureComponentMissingException ex;
            httpContext.Request.QueryString = new QueryString("?a=b");
            httpContext.Request.Headers.Add("x-non-dict", "#");
            httpContext.Request.Headers.Add("x-dict", "a=1234,b=9876");

            ex = Assert.Throws<SignatureComponentMissingException>(() => httpContext.Request.GetSignatureInput(inputSpec));

            Assert.Equal(expectedExceptionMessage, ex.Message);
        }

        [Theory]
        [InlineData(@"(""@status"")")]
        [InlineData(@"(""@request-response"";key=""sig1"")")]
        [InlineData(@"(""@blah"")")]
        public void VisitorThrowsForUnsupportedDerivedComponents(string input)
        {
            SignatureInputSpec inputSpec = new SignatureInputSpec("test", input);
            Assert.Throws<NotSupportedException>(() => httpContext.Request.GetSignatureInput(inputSpec));
        }

        [Theory]
        [InlineData(@"(""x-header"");created=123", "\"x-header\": the-value")]
        [InlineData(@"(""x-empty"");created=123", "\"x-empty\": ")]
        [InlineData(@"(""x-dict"");expires=123", "\"x-dict\": a=a, c=d, a=b")]
        [InlineData(@"(""x-dict"";key=""a"");nonce=""123-abc""", "\"x-dict\";key=\"a\": b")]
        [InlineData(@"(""@method"");nonce=""555-def""", "\"@method\": POST")]
        [InlineData(@"(""@target-uri"")", "\"@target-uri\": https://test:8443/sub/dir/endpoint/name/?a=b&A=C&x=")]
        [InlineData(@"(""@authority"");alg=""blah""", "\"@authority\": test:8443")]
        [InlineData(@"(""@scheme"")", "\"@scheme\": https")]
        [InlineData(@"(""@request-target"")", "\"@request-target\": /sub/dir/endpoint/name/?a=b&A=C&x=")]
        [InlineData(@"(""@path"")", "\"@path\": /sub/dir/endpoint/name/")]
        [InlineData(@"(""@query"")", "\"@query\": ?a=b&A=C&x=")]
        [InlineData(@"(""@query-params"";name=""a"")", "\"@query-params\";name=\"a\": b\n\"@query-params\";name=\"a\": C")]
        [InlineData(@"(""@query-params"";name=""x"")", "\"@query-params\";name=\"x\": ")]
        public void VisitorProducesCorrectInputString(string rawInputSpec, string expectedInputMinusSignatureParams)
        {
            httpContext.Request.Headers.Add("x-header", "the-value");
            httpContext.Request.Headers.Add("x-dict", "a=a, c=d, a=b");
            httpContext.Request.Headers.Add("x-empty", "");
            httpContext.Request.Method = "POST";
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("test:8443");
            httpContext.Request.PathBase = "/sub/dir";
            httpContext.Request.Path = "/endpoint/name/";
            httpContext.Request.QueryString = new QueryString("?a=b&A=C&x=");

            SignatureInputSpec inputSpec = new SignatureInputSpec("test", rawInputSpec);
            byte[] input = httpContext.Request.GetSignatureInput(inputSpec);

            Assert.Equal($"{expectedInputMinusSignatureParams}\n\"@signature-params\": {rawInputSpec}", GetString(input));
        }

        private static string GetString(byte[] input)
        {
            return Encoding.ASCII.GetString(input);
        }
    }
}
