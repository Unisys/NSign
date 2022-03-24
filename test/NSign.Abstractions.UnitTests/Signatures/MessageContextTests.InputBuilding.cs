using Moq;
using System;
using System.Linq;
using System.Text;
using Xunit;

namespace NSign.Signatures
{
    partial class MessageContextTests
    {
        [Fact]
        public void GetSignatureInputThrowsForMissingComponents()
        {
            context.OnGetHeaderValues = context.OnGetRequestHeaderValues = context.OnGetQueryParamValues =
                (headerName) => Array.Empty<string>();

            SignatureComponentMissingException ex;

            SignatureInputSpec spec = MakeSignatureInput(SignatureComponent.ContentType);
            ex = Assert.Throws<SignatureComponentMissingException>(() => context.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component 'content-type' does not exist but is required.", ex.Message);

            spec = MakeSignatureInput(new HttpHeaderDictionaryStructuredComponent("blah", "a"));
            ex = Assert.Throws<SignatureComponentMissingException>(() => context.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component 'blah;key=\"a\"' does not exist but is required.", ex.Message);

            spec = MakeSignatureInput(new HttpHeaderDictionaryStructuredComponent("my-header", "a"));
            ex = Assert.Throws<SignatureComponentMissingException>(() => context.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component 'my-header;key=\"a\"' does not exist but is required.", ex.Message);

            spec = MakeSignatureInput(new QueryParamsComponent("blotz"));
            ex = Assert.Throws<SignatureComponentMissingException>(() => context.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component '@query-params;name=\"blotz\"' does not exist but is required.", ex.Message);

            spec = MakeSignatureInput(new RequestResponseComponent("blimp"));
            ex = Assert.Throws<SignatureComponentMissingException>(() => context.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component '@request-response;key=\"blimp\"' does not exist but is required.", ex.Message);

            context.HasResponseValue = true;
            spec = MakeSignatureInput(new RequestResponseComponent("foo"));
            ex = Assert.Throws<SignatureComponentMissingException>(() => context.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component '@request-response;key=\"foo\"' does not exist but is required.", ex.Message);
        }

        [Fact]
        public void GetSignatureInputGetsCorrectHttpHeaders()
        {
            context.OnGetHeaderValues = (headerName) =>
            {
                return headerName switch
                {
                    "my-header" => new string[] { "blah", },
                    "my-generic-dict" => new string[] { "a=b, b=c, b=z, c" },
                    _ => Array.Empty<string>(),
                };
            };

            SignatureInputSpec spec;
            string inputStr;
            ReadOnlyMemory<byte> input;

            // Simple HTTP header.
            spec = MakeSignatureInput(new HttpHeaderComponent("my-Header"));
            input = context.GetSignatureInput(spec, out inputStr);
            Assert.Equal("(\"my-header\")", inputStr);
            Assert.Equal("\"my-header\": blah\n\"@signature-params\": (\"my-header\")", Encoding.ASCII.GetString(input.Span));

            // Simple HTTP header that happens to be dictionary structured.
            spec = MakeSignatureInput(new HttpHeaderComponent("my-generic-dict"));
            input = context.GetSignatureInput(spec, out inputStr);
            Assert.Equal("(\"my-generic-dict\")", inputStr);
            Assert.Equal("\"my-generic-dict\": a=b, b=c, b=z, c\n\"@signature-params\": (\"my-generic-dict\")", Encoding.ASCII.GetString(input.Span));

            // Dictionary-structured HTTP header.
            spec = MakeSignatureInput(new HttpHeaderDictionaryStructuredComponent("my-generic-dict", "b"));
            input = context.GetSignatureInput(spec, out inputStr);
            Assert.Equal("(\"my-generic-dict\";key=\"b\")", inputStr);
            Assert.Equal("\"my-generic-dict\";key=\"b\": z\n\"@signature-params\": (\"my-generic-dict\";key=\"b\")", Encoding.ASCII.GetString(input.Span));

            // Dictionary-structured HTTP header with implicit 'true' value.
            spec = MakeSignatureInput(new HttpHeaderDictionaryStructuredComponent("my-generic-dict", "c"));
            input = context.GetSignatureInput(spec, out inputStr);
            Assert.Equal("(\"my-generic-dict\";key=\"c\")", inputStr);
            Assert.Equal("\"my-generic-dict\";key=\"c\": ?1\n\"@signature-params\": (\"my-generic-dict\";key=\"c\")", Encoding.ASCII.GetString(input.Span));
        }

        [Theory]
        [InlineData("@query-params")]
        [InlineData("@request-response")]
        [InlineData("@blah")]
        public void GetSignatureInputThrowsNotSupportedExceptionForUnsupportedDerivedComponents(string name)
        {
            context.OnGetDerivedComponentValue = (comp) => throw new NotSupportedException();

            SignatureInputSpec spec = new SignatureInputSpec("foo");
            spec.SignatureParameters.AddComponent(new DerivedComponent(name));
            Assert.Throws<NotSupportedException>(() => context.GetSignatureInput(spec, out _));
        }

        [Fact]
        public void GetSignatureInputDoesNotSupportCustomClassesDerivedFromSignatureComponent()
        {
            SignatureInputSpec spec = new SignatureInputSpec("test");
            Mock<SignatureComponent> mockComp = new Mock<SignatureComponent>(
                MockBehavior.Loose, SignatureComponentType.HttpHeader, "Test-Component");
            mockComp.Setup(c => c.Accept(It.IsAny<ISignatureComponentVisitor>()))
                .Callback((ISignatureComponentVisitor visitor) => visitor.Visit(mockComp.Object));

            spec.SignatureParameters.AddComponent(mockComp.Object);
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => context.GetSignatureInput(spec, out _));
            Assert.Equal("Custom classes derived from SignatureComponent are not supported; component 'test-component'.", ex.Message);
        }

        [Theory]
        [InlineData("@method", "PUT")]
        [InlineData("@target-uri", "https://some.host.local:8443/the/path/to/the/endpoint?My=Param&another")]
        [InlineData("@authority", "some.host.local:8443")]
        [InlineData("@scheme", "https")]
        [InlineData("@request-target", "/the/path/to/the/endpoint?My=Param&another")]
        [InlineData("@path", "/the/path/to/the/endpoint")]
        [InlineData("@query", "?My=Param&another")]
        public void GetSignatureInputGetsCorrectDerivedComponentValue(string name, string expectedValue)
        {
            context.OnGetDerivedComponentValue = (comp) =>
            {
                return comp.ComponentName switch
                {
                    "@method" => "PUT",
                    "@target-uri" => "https://some.host.local:8443/the/path/to/the/endpoint?My=Param&another",
                    "@scheme" => "https",
                    "@authority" => "some.host.local:8443",
                    "@request-target" => "/the/path/to/the/endpoint?My=Param&another",
                    "@path" => "/the/path/to/the/endpoint",
                    "@query" => "?My=Param&another",
                    _ => null,
                };
            };

            SignatureInputSpec spec = new SignatureInputSpec("blah");
            spec.SignatureParameters.AddComponent(new DerivedComponent(name));
            ReadOnlyMemory<byte> input = context.GetSignatureInput(spec, out string inputStr);

            Assert.Equal($"(\"{name}\")", inputStr);
            Assert.Equal($"\"{name}\": {expectedValue}\n\"@signature-params\": {inputStr}", Encoding.ASCII.GetString(input.Span));
        }

        [Theory]
        [InlineData("My", new string[] { "param", })]
        [InlineData("a", new string[] { "b", "cc", })]
        [InlineData("A", new string[] { "b", "cc", })]
        [InlineData("another", new string[] { "", })]
        public void GetSignatureInputGetsCorrectQueryParamsValue(string paramName, string[] expectedValues)
        {
            context.OnGetQueryParamValues = (paramName) =>
            {
                return paramName.ToLowerInvariant() switch
                {
                    "a" => new string[] { "b", "cc", },
                    "my" => new string[] { "param", },
                    "another" => new string[] { "", },
                    _ => Array.Empty<string>(),
                };
            };

            SignatureInputSpec spec = new SignatureInputSpec("blah");
            spec.SignatureParameters.AddComponent(new QueryParamsComponent(paramName));
            ReadOnlyMemory<byte> input = context.GetSignatureInput(spec, out string inputStr);

            Assert.Equal($"(\"@query-params\";name=\"{paramName.ToLower()}\")", inputStr);
            string expectedValue = String.Join("", expectedValues.Select(v => $"\"@query-params\";name=\"{paramName.ToLower()}\": {v}\n"));
            Assert.Equal($"{expectedValue}\"@signature-params\": {inputStr}", Encoding.ASCII.GetString(input.Span));
        }

        [Fact]
        public void GetSignatureInputThrowsForRequestResponseComponent()
        {
            SignatureInputSpec spec = new SignatureInputSpec("test");
            spec.SignatureParameters.AddComponent(new RequestResponseComponent("another"));
            SignatureComponentMissingException ex = Assert.Throws<SignatureComponentMissingException>(
                () => context.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component '@request-response;key=\"another\"' does not exist but is required.", ex.Message);

            TestMessageContext localContext = new TestMessageContext(mockLogger.Object);
            localContext.HasResponseValue = true;
            localContext.OnGetRequestHeaderValues = (headerName) => Array.Empty<string>();

            ex = Assert.Throws<SignatureComponentMissingException>(() => context.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component '@request-response;key=\"another\"' does not exist but is required.", ex.Message);

        }

        [Theory]
        [InlineData(200)]
        [InlineData(404)]
        [InlineData(503)]
        public void GetSignatureInputWorks(int status)
        {
            context.HasResponseValue = true;
            context.OnGetDerivedComponentValue = (comp) =>
            {
                return comp.ComponentName switch
                {
                    "@status" => status.ToString(),
                    "@authority" => "some.other.host.local:8443",
                    _ => "blah",
                };
            };
            context.OnGetRequestHeaderValues = (headerName) =>
            {
                return headerName.ToLowerInvariant() switch
                {
                    "signature" => new string[] { "test=:dGVzdA==:", },
                    _ => Array.Empty<string>(),
                };
            };
            context.OnGetHeaderValues = (headerName) =>
            {
                return headerName switch
                {
                    "content-type" => new string[] { "text/test; charset=utf-8", },
                    "content-length" => new string[] { "2345", },
                    _ => Array.Empty<string>(),
                };
            };

            SignatureInputSpec spec = new SignatureInputSpec("test");

            spec.SignatureParameters
                .AddComponent(SignatureComponent.Status)
                .AddComponent(new RequestResponseComponent("test"))
                .AddComponent(SignatureComponent.ContentType)
                .AddComponent(SignatureComponent.ContentLength)
                .AddComponent(SignatureComponent.Authority)
                .WithCreated(DateTimeOffset.UnixEpoch.AddMinutes(1))
                .WithKeyId("my-key")
                .WithNonce("test-nonce");
            spec.SignatureParameters.Expires = DateTimeOffset.UnixEpoch.AddMinutes(6);
            spec.SignatureParameters.Algorithm = "my";

            ReadOnlyMemory<byte> input = context.GetSignatureInput(spec, out string inputStr);
            Assert.Equal(
                "(\"@status\" \"@request-response\";key=\"test\" \"content-type\" \"content-length\" \"@authority\");created=60;expires=360;nonce=\"test-nonce\";alg=\"my\";keyid=\"my-key\"",
                inputStr);
            Assert.Equal(
                $"\"@status\": {status}\n" +
                "\"@request-response\";key=\"test\": :dGVzdA==:\n" +
                "\"content-type\": text/test; charset=utf-8\n" +
                "\"content-length\": 2345\n" +
                "\"@authority\": some.other.host.local:8443\n" +
                $"\"@signature-params\": {inputStr}", Encoding.ASCII.GetString(input.Span));
        }

        private static SignatureInputSpec MakeSignatureInput(SignatureComponent component)
        {
            SignatureInputSpec spec = new SignatureInputSpec("unitTest");
            spec.SignatureParameters.AddComponent(component);

            return spec;
        }
    }
}
