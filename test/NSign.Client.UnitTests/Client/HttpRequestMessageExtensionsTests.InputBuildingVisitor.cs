using Moq;
using NSign.Signatures;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using Xunit;

namespace NSign.Client
{
    partial class HttpRequestMessageExtensionsTests
    {
        [Fact]
        public void GetSignatureInputThrowsForMissingComponents()
        {
            request.Headers.Add("my-header", "@");
            request.RequestUri = new Uri("http://localhost/?Blah");
            SignatureComponentMissingException ex;

            SignatureInputSpec spec = MakeSignatureInput(SignatureComponent.ContentType);
            ex = Assert.Throws<SignatureComponentMissingException>(() => request.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component 'content-type' does not exist but is required.", ex.Message);

            spec = MakeSignatureInput(new HttpHeaderDictionaryStructuredComponent("blah", "a"));
            ex = Assert.Throws<SignatureComponentMissingException>(() => request.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component 'blah;key=\"a\"' does not exist but is required.", ex.Message);

            spec = MakeSignatureInput(new HttpHeaderDictionaryStructuredComponent("my-header", "a"));
            ex = Assert.Throws<SignatureComponentMissingException>(() => request.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component 'my-header;key=\"a\"' does not exist but is required.", ex.Message);

            spec = MakeSignatureInput(new QueryParamsComponent("Blah"));
            ex = Assert.Throws<SignatureComponentMissingException>(() => request.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component '@query-params;name=\"blah\"' does not exist but is required.", ex.Message);

            spec = MakeSignatureInput(new QueryParamsComponent("blotz"));
            ex = Assert.Throws<SignatureComponentMissingException>(() => request.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component '@query-params;name=\"blotz\"' does not exist but is required.", ex.Message);
        }

        [Fact]
        public void GetSignatureInputGetsCorrectHttpHeaders()
        {
            request.Headers.Add("my-header", "blah");
            request.Headers.Add("my-generic-dict", "a=b, b=c, b=z, c");
            SignatureInputSpec spec;
            string inputStr;
            byte[] input;

            // Simple HTTP header.
            spec = MakeSignatureInput(new HttpHeaderComponent("my-header"));
            input = request.GetSignatureInput(spec, out inputStr);
            Assert.Equal("(\"my-header\")", inputStr);
            Assert.Equal("\"my-header\": blah\n\"@signature-params\": (\"my-header\")", Encoding.ASCII.GetString(input));

            // Simple HTTP header that happens to be dictionary structured.
            spec = MakeSignatureInput(new HttpHeaderComponent("my-generic-dict"));
            input = request.GetSignatureInput(spec, out inputStr);
            Assert.Equal("(\"my-generic-dict\")", inputStr);
            Assert.Equal("\"my-generic-dict\": a=b, b=c, b=z, c\n\"@signature-params\": (\"my-generic-dict\")", Encoding.ASCII.GetString(input));

            // Dictionary-structured HTTP header.
            spec = MakeSignatureInput(new HttpHeaderDictionaryStructuredComponent("my-generic-dict", "b"));
            input = request.GetSignatureInput(spec, out inputStr);
            Assert.Equal("(\"my-generic-dict\";key=\"b\")", inputStr);
            Assert.Equal("\"my-generic-dict\";key=\"b\": z\n\"@signature-params\": (\"my-generic-dict\";key=\"b\")", Encoding.ASCII.GetString(input));

            // Dictionary-structured HTTP header with implicit 'true' value.
            spec = MakeSignatureInput(new HttpHeaderDictionaryStructuredComponent("my-generic-dict", "c"));
            input = request.GetSignatureInput(spec, out inputStr);
            Assert.Equal("(\"my-generic-dict\";key=\"c\")", inputStr);
            Assert.Equal("\"my-generic-dict\";key=\"c\": ?1\n\"@signature-params\": (\"my-generic-dict\";key=\"c\")", Encoding.ASCII.GetString(input));
        }

        [Theory]
        [InlineData("@query-params")]
        [InlineData("@status")]
        [InlineData("@request-response")]
        [InlineData("@blah")]
        public void GetSignatureInputThrowsNotSupportedExceptionForUnsupportedDerivedComponents(string name)
        {
            SignatureInputSpec spec = new SignatureInputSpec("foo");
            spec.SignatureParameters.AddComponent(new DerivedComponent(name));
            Assert.Throws<NotSupportedException>(() => request.GetSignatureInput(spec, out _));
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
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => request.GetSignatureInput(spec, out _));
            Assert.Equal("Custom classes derived from SignatureComponent are not supported; component 'test-component'.", ex.Message);
        }

        [Theory]
        [InlineData("@method", "PUT")]
        [InlineData("@target-uri", "https://some.host.local:8443/the/path/to/the/endpoint?my=param&another")]
        [InlineData("@authority", "some.host.local:8443")]
        [InlineData("@scheme", "https")]
        [InlineData("@request-target", "/the/path/to/the/endpoint?my=param&another")]
        [InlineData("@path", "/the/path/to/the/endpoint")]
        [InlineData("@query", "?my=param&another")]
        public void GetSignatureInputGetsCorrectDerivedComponentValue(string name, string expectedValue)
        {
            request.Method = HttpMethod.Put;
            request.RequestUri = new Uri("https://some.host.local:8443/the/path/to/the/endpoint?my=param&another");

            SignatureInputSpec spec = new SignatureInputSpec("blah");
            spec.SignatureParameters.AddComponent(new DerivedComponent(name));
            byte[] input = request.GetSignatureInput(spec, out string inputStr);

            Assert.Equal($"(\"{name}\")", inputStr);
            Assert.Equal($"\"{name}\": {expectedValue}\n\"@signature-params\": {inputStr}", Encoding.ASCII.GetString(input));
        }

        [Theory]
        [InlineData("My", new string[] { "param", })]
        [InlineData("a", new string[] { "b", "cc", })]
        [InlineData("another", new string[] { "", })]
        public void GetSignatureInputGetsCorrectQueryParamsValue(string paramName, string[] expectedValues)
        {
            request.Method = HttpMethod.Put;
            request.RequestUri = new Uri("https://some.host.local:8443/the/path/to/the/endpoint?a=b&my=param&A=cc&another=");

            SignatureInputSpec spec = new SignatureInputSpec("blah");
            spec.SignatureParameters.AddComponent(new QueryParamsComponent(paramName));
            byte[] input = request.GetSignatureInput(spec, out string inputStr);

            Assert.Equal($"(\"@query-params\";name=\"{paramName.ToLower()}\")", inputStr);
            string expectedValue = String.Join("", expectedValues.Select(v => $"\"@query-params\";name=\"{paramName.ToLower()}\": {v}\n"));
            Assert.Equal($"{expectedValue}\"@signature-params\": {inputStr}", Encoding.ASCII.GetString(input));
        }

        [Fact]
        public void GetSignatureInputThrowsForRequestResponseComponent()
        {
            SignatureInputSpec spec = new SignatureInputSpec("test");
            spec.SignatureParameters.AddComponent(new RequestResponseComponent("another"));
            Assert.Throws<NotSupportedException>(() => request.GetSignatureInput(spec, out _));
        }

        [Fact]
        public void GetSignatureInputWorks()
        {
            request.Method = HttpMethod.Patch;
            request.RequestUri = new Uri("https://some.host.local:8443/the/path/to/the/endpoint?a=b&my=param&A=cc&another=");
            request.Content = new StringContent("blah blah", Encoding.UTF8, MediaTypeNames.Text.Plain);
            SignatureInputSpec spec = new SignatureInputSpec("test");

            spec.SignatureParameters
                .AddComponent(SignatureComponent.Method)
                .AddComponent(SignatureComponent.ContentType)
                .AddComponent(SignatureComponent.ContentLength)
                .AddComponent(SignatureComponent.Authority)
                .WithCreated(DateTimeOffset.UnixEpoch.AddMinutes(1))
                .WithKeyId("my-key")
                .WithNonce("test-nonce");
            spec.SignatureParameters.Expires = DateTimeOffset.UnixEpoch.AddMinutes(6);
            spec.SignatureParameters.Algorithm = "my";

            byte[] input = request.GetSignatureInput(spec, out string inputStr);
            Assert.Equal(
                "(\"@method\" \"content-type\" \"content-length\" \"@authority\");created=60;expires=360;nonce=\"test-nonce\";alg=\"my\";keyid=\"my-key\"",
                inputStr);
            Assert.Equal(
                "\"@method\": PATCH\n" +
                "\"content-type\": text/plain; charset=utf-8\n" +
                "\"content-length\": 9\n" +
                "\"@authority\": some.host.local:8443\n" +
                $"\"@signature-params\": {inputStr}", Encoding.ASCII.GetString(input));
        }

        private static SignatureInputSpec MakeSignatureInput(SignatureComponent component)
        {
            SignatureInputSpec spec = new SignatureInputSpec("blah");
            spec.SignatureParameters.AddComponent(component);

            return spec;
        }
    }
}
