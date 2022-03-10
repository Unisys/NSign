using NSign.Signatures;
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Xunit;

namespace NSign.Client
{
    public sealed class SignatureVerificationHandlerVisitorTests
    {
        private static readonly Type VisitorType = Type.GetType("NSign.Client.SignatureVerificationHandler+Visitor, NSign.Client");
        private static readonly PropertyInfo SignatureInputProperty = VisitorType.GetProperty("SignatureInput");

        private readonly HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:8080/UnitTests/?a=b");
        private readonly HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        private readonly ISignatureComponentVisitor visitor;

        public SignatureVerificationHandlerVisitorTests()
        {
            visitor = Activator.CreateInstance(VisitorType, request, response) as ISignatureComponentVisitor;
        }

        [Fact]
        public void VisitorThrowsForSignatureParamsComponentWithoutOriginalValue()
        {
            SignatureInputSpec inputSpec = new SignatureInputSpec("test");
            InvalidOperationException ex;

            ex = Assert.Throws<InvalidOperationException>(() => inputSpec.SignatureParameters.Accept(visitor));

            Assert.Equal("Signature input can only be created for SignatureParamsComponents received from HTTP requests.", ex.Message);
        }

        [Fact]
        public void EmptyParamsProducesMinimalSignatureInput()
        {
            SignatureInputSpec inputSpec = new SignatureInputSpec("test", @"()");
            inputSpec.SignatureParameters.Accept(visitor);

            Assert.Equal(@"""@signature-params"": ()", GetSignatureInput());
        }

        [Theory]
        [InlineData(@"(""x-missing"")", @"The signature component 'x-missing' does not exist but is required.")]
        [InlineData(@"(""x-missing"";key=""foo"")", @"The signature component 'x-missing;key=""foo""' does not exist but is required.")]
        [InlineData(@"(""x-dict"";key=""foo"")", @"The signature component 'x-dict;key=""foo""' does not exist but is required.")]
        [InlineData(@"(""x-non-dict"";key=""foo"")", @"The signature component 'x-non-dict;key=""foo""' does not exist but is required.")]
        public void VisitorThrowsForMissingHttpHeader(string input, string expectedExceptionMessage)
        {
            SignatureInputSpec inputSpec = new SignatureInputSpec("test", input);

            request.Headers.Add("x-non-dict", "#");
            request.Headers.Add("x-dict", "a=1234,b=9876");

            response.Headers.Add("x-non-dict", "#");
            response.Headers.Add("x-dict", "a=1234,b=9876");

            SignatureComponentMissingException ex = Assert.Throws<SignatureComponentMissingException>(
                () => inputSpec.SignatureParameters.Accept(visitor));

            Assert.Equal(expectedExceptionMessage, ex.Message);
        }

        [Theory]
        [InlineData(@"(""@query-params"";name=""x"")", @"The signature component '@query-params;name=""x""' does not exist but is required.")]
        [InlineData(@"(""@request-response"";key=""any"")", @"The signature component '@request-response;key=""any""' does not exist but is required.")]
        public void VisitorThrowsForMissingDerivedComponent(string input, string expectedExceptionMessage)
        {
            SignatureInputSpec inputSpec = new SignatureInputSpec("test", input);

            request.Headers.Add("signature", "other=:1234:");

            SignatureComponentMissingException ex = Assert.Throws<SignatureComponentMissingException>(
                () => inputSpec.SignatureParameters.Accept(visitor));

            Assert.Equal(expectedExceptionMessage, ex.Message);
        }

        [Theory]
        [InlineData(@"(""@request-response"";key=""any"")", @"The signature component '@request-response;key=""any""' does not exist but is required.")]
        public void VisitorThrowsForMissingRequestResponse(string input, string expectedExceptionMessage)
        {
            SignatureInputSpec inputSpec = new SignatureInputSpec("test", input);

            SignatureComponentMissingException ex = Assert.Throws<SignatureComponentMissingException>(
                () => inputSpec.SignatureParameters.Accept(visitor));

            Assert.Equal(expectedExceptionMessage, ex.Message);
        }

        [Fact]
        public void VisitorThrowsForUnsupportedComponentImpl()
        {
            TestComponent comp = new TestComponent(SignatureComponentType.HttpHeader, "blah");

            Assert.Throws<NotSupportedException>(() => comp.Accept(visitor));
        }

        [Theory]
        [InlineData(@"(""@blah"")")]
        public void VisitorThrowsForUnsupportedDerivedComponents(string input)
        {
            SignatureInputSpec inputSpec = new SignatureInputSpec("test", input);
            Assert.Throws<NotSupportedException>(() => inputSpec.SignatureParameters.Accept(visitor));
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
        [InlineData(@"(""@status"")", "\"@status\": 406")]
        [InlineData(@"(""@request-response"";key=""reqsig"")", "\"@request-response\";key=\"reqsig\": :dGVzdA==:")]
        public void VisitorProducesCorrectInputString(string rawInputSpec, string expectedInputMinusSignatureParams)
        {
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri("https://test:8443/sub/dir/endpoint/name/?a=b&A=C&x=");
            request.Headers.Add("signature", "reqsig=:dGVzdA==:");

            response.StatusCode = HttpStatusCode.NotAcceptable;
            response.Headers.Add("x-header", "the-value");
            response.Headers.Add("x-dict", "a=a, c=d, a=b");
            response.Headers.Add("x-empty", "");

            SignatureInputSpec inputSpec = new SignatureInputSpec("test", rawInputSpec);
            inputSpec.SignatureParameters.Accept(visitor);

            Assert.Equal($"{expectedInputMinusSignatureParams}\n\"@signature-params\": {rawInputSpec}", GetSignatureInput());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("?")]
        public void VisitorProducesCorrectInputStringForEmptyQuery(string query)
        {
            request.RequestUri = new Uri("http://localhost/" + query);

            string rawInputSpec = "(\"@query\")";
            SignatureInputSpec inputSpec = new SignatureInputSpec("test", rawInputSpec);
            inputSpec.SignatureParameters.Accept(visitor);

            Assert.Equal($"\"@query\": ?\n\"@signature-params\": {rawInputSpec}", GetSignatureInput());
        }

        private string GetSignatureInput()
        {
            return (string)SignatureInputProperty.GetValue(visitor);
        }
    }
}
