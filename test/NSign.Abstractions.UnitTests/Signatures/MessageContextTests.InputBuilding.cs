using Moq;
using System;
using System.Collections.Generic;
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
            context.HasResponseValue = true;

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

            spec = MakeSignatureInput(new HttpHeaderStructuredFieldComponent("my-header"));
            ex = Assert.Throws<SignatureComponentMissingException>(() => context.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component 'my-header;sf' does not exist but is required.", ex.Message);

            spec = MakeSignatureInput(new QueryParamComponent("blotz"));
            ex = Assert.Throws<SignatureComponentMissingException>(() => context.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component '@query-param;name=\"blotz\"' does not exist but is required.", ex.Message);

            // With bindRequest: true
            spec = MakeSignatureInput(new HttpHeaderComponent("blah", bindRequest: true));
            ex = Assert.Throws<SignatureComponentMissingException>(() => context.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component 'blah' does not exist but is required.", ex.Message);

            spec = MakeSignatureInput(new HttpHeaderDictionaryStructuredComponent("my-header", "a", bindRequest: true));
            ex = Assert.Throws<SignatureComponentMissingException>(() => context.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component 'my-header;key=\"a\"' does not exist but is required.", ex.Message);

            spec = MakeSignatureInput(new HttpHeaderStructuredFieldComponent("my-header", bindRequest: true));
            ex = Assert.Throws<SignatureComponentMissingException>(() => context.GetSignatureInput(spec, out _));
            Assert.Equal("The signature component 'my-header;sf' does not exist but is required.", ex.Message);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetSignatureInputGetsCorrectHttpHeaders(bool bindRequest)
        {
            Func<string, IEnumerable<string>> getHeaderValues = (headerName) =>
            {
                return headerName switch
                {
                    "my-header" => new string[] { "blah", },
                    "my-generic-dict" => new string[] { "a=b, b=c, b=z, c" },
                    _ => Array.Empty<string>(),
                };
            };
            if (bindRequest)
            {
                context.OnGetRequestHeaderValues = getHeaderValues;
            }
            else
            {
                context.OnGetHeaderValues = getHeaderValues;
            }
            context.HasResponseValue = bindRequest;
            string suffix = bindRequest ? ";req" : String.Empty;

            SignatureInputSpec spec;
            string inputStr;
            ReadOnlyMemory<byte> input;

            // Simple HTTP header.
            spec = MakeSignatureInput(new HttpHeaderComponent("my-Header", bindRequest));
            input = context.GetSignatureInput(spec, out inputStr);
            Assert.Equal($"(\"my-header\"{suffix})", inputStr);
            Assert.Equal($"\"my-header\"{suffix}: blah\n\"@signature-params\": (\"my-header\"{suffix})",
                Encoding.ASCII.GetString(input.Span));

            // Simple HTTP header that happens to be dictionary structured.
            spec = MakeSignatureInput(new HttpHeaderComponent("my-generic-dict", bindRequest));
            input = context.GetSignatureInput(spec, out inputStr);
            Assert.Equal($"(\"my-generic-dict\"{suffix})", inputStr);
            Assert.Equal($"\"my-generic-dict\"{suffix}: a=b, b=c, b=z, c\n\"@signature-params\": (\"my-generic-dict\"{suffix})",
                Encoding.ASCII.GetString(input.Span));

            // Dictionary-structured HTTP header.
            spec = MakeSignatureInput(new HttpHeaderDictionaryStructuredComponent("my-generic-dict", "b", bindRequest));
            input = context.GetSignatureInput(spec, out inputStr);
            Assert.Equal($"(\"my-generic-dict\"{suffix};key=\"b\")", inputStr);
            Assert.Equal($"\"my-generic-dict\"{suffix};key=\"b\": z\n\"@signature-params\": (\"my-generic-dict\"{suffix};key=\"b\")",
                Encoding.ASCII.GetString(input.Span));

            // Dictionary-structured HTTP header with implicit 'true' value.
            spec = MakeSignatureInput(new HttpHeaderDictionaryStructuredComponent("my-generic-dict", "c", bindRequest));
            input = context.GetSignatureInput(spec, out inputStr);
            Assert.Equal($"(\"my-generic-dict\"{suffix};key=\"c\")", inputStr);
            Assert.Equal($"\"my-generic-dict\"{suffix};key=\"c\": ?1\n\"@signature-params\": (\"my-generic-dict\"{suffix};key=\"c\")",
                Encoding.ASCII.GetString(input.Span));

            // Structured field HTTP header.
            spec = MakeSignatureInput(new HttpHeaderStructuredFieldComponent("my-generic-dict", bindRequest));
            input = context.GetSignatureInput(spec, out inputStr);
            Assert.Equal($"(\"my-generic-dict\"{suffix};sf)", inputStr);
            Assert.Equal($"\"my-generic-dict\"{suffix};sf: a=b, b=z, c\n\"@signature-params\": (\"my-generic-dict\"{suffix};sf)",
                Encoding.ASCII.GetString(input.Span));
        }

        [Theory]
        [InlineData("@query-param")]
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
        [InlineData(false, "@method", "PUT")]
        [InlineData(false, "@target-uri", "https://some.host.local:8443/the/path/to/the/endpoint?My=Param&another")]
        [InlineData(false, "@authority", "some.host.local:8443")]
        [InlineData(false, "@scheme", "https")]
        [InlineData(false, "@request-target", "/the/path/to/the/endpoint?My=Param&another")]
        [InlineData(false, "@path", "/the/path/to/the/endpoint")]
        [InlineData(false, "@query", "?My=Param&another")]
        [InlineData(true, "@method", "PUT")]
        [InlineData(true, "@target-uri", "https://some.host.local:8443/the/path/to/the/endpoint?My=Param&another")]
        [InlineData(true, "@authority", "some.host.local:8443")]
        [InlineData(true, "@scheme", "https")]
        [InlineData(true, "@request-target", "/the/path/to/the/endpoint?My=Param&another")]
        [InlineData(true, "@path", "/the/path/to/the/endpoint")]
        [InlineData(true, "@query", "?My=Param&another")]
        public void GetSignatureInputGetsCorrectDerivedComponentValue(bool bindRequest, string name, string expectedValue)
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
            string suffix = bindRequest ? ";req" : String.Empty;
            context.HasResponseValue = bindRequest;

            SignatureInputSpec spec = new SignatureInputSpec("blah");
            spec.SignatureParameters.AddComponent(new DerivedComponent(name, bindRequest));
            ReadOnlyMemory<byte> input = context.GetSignatureInput(spec, out string inputStr);

            Assert.Equal($"(\"{name}\"{suffix})", inputStr);
            Assert.Equal($"\"{name}\"{suffix}: {expectedValue}\n\"@signature-params\": {inputStr}", Encoding.ASCII.GetString(input.Span));
        }

        [Theory]
        [InlineData(false, "My", new string[] { "param", })]
        [InlineData(false, "a", new string[] { "b", "cc", })]
        [InlineData(false, "A", new string[] { "b", "cc", })]
        [InlineData(false, "another", new string[] { "", })]
        [InlineData(true, "My", new string[] { "param", })]
        [InlineData(true, "a", new string[] { "b", "cc", })]
        [InlineData(true, "A", new string[] { "b", "cc", })]
        [InlineData(true, "another", new string[] { "", })]
        public void GetSignatureInputGetsCorrectQueryParamsValue(bool bindRequest, string paramName, string[] expectedValues)
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
            string suffix = bindRequest ? ";req" : String.Empty;
            context.HasResponseValue = bindRequest;

            SignatureInputSpec spec = new SignatureInputSpec("blah");
            spec.SignatureParameters.AddComponent(new QueryParamComponent(paramName, bindRequest));
            ReadOnlyMemory<byte> input = context.GetSignatureInput(spec, out string inputStr);

            Assert.Equal($"(\"@query-param\"{suffix};name=\"{paramName.ToLower()}\")", inputStr);
            string expectedValue = String.Join("", expectedValues.Select(v => $"\"@query-param\"{suffix};name=\"{paramName.ToLower()}\": {v}\n"));
            Assert.Equal($"{expectedValue}\"@signature-params\": {inputStr}", Encoding.ASCII.GetString(input.Span));
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
                .AddComponent(new HttpHeaderDictionaryStructuredComponent("SignaturE", "test", bindRequest: true))
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
                "(\"@status\" \"signature\";req;key=\"test\" \"content-type\" \"content-length\" \"@authority\")" +
                ";created=60;expires=360;nonce=\"test-nonce\";alg=\"my\";keyid=\"my-key\"",
                inputStr);
            Assert.Equal(
                $"\"@status\": {status}\n" +
                "\"signature\";req;key=\"test\": :dGVzdA==:\n" +
                "\"content-type\": text/test; charset=utf-8\n" +
                "\"content-length\": 2345\n" +
                "\"@authority\": some.other.host.local:8443\n" +
                $"\"@signature-params\": {inputStr}", Encoding.ASCII.GetString(input.Span));
        }

        [Theory]
        [InlineData("(\"@status\" \"content-type\";req \"@authority\";req);keyid=\"my-key\";expires=360;alg=\"my\";created=60")]
        public void GetSignatureInputPassesOriginalInputWhenAvailable(string inputSigParams)
        {
            SignatureInputSpec spec = new SignatureInputSpec("test", inputSigParams);
            context.HasResponseValue = true;
            context.OnGetDerivedComponentValue = (comp) =>
            {
                return comp.ComponentName switch
                {
                    "@status" => "123",
                    "@authority" => "unit.tests:1234",
                    _ => throw new Exception("Unexpected"),
                };
            };
            context.OnGetRequestHeaderValues = (headerName) =>
            {
                return headerName switch
                {
                    "content-type" => new string[] { "text/test; charset=utf-8", },
                    _ => throw new Exception("Unexpected"),
                };
            };

            ReadOnlyMemory<byte> input = context.GetSignatureInput(spec, out string outputSigParams);

            Assert.Equal(inputSigParams, outputSigParams);
            Assert.Equal(
                $"\"@status\": 123\n" +
                "\"content-type\";req: text/test; charset=utf-8\n" +
                "\"@authority\";req: unit.tests:1234\n" +
                $"\"@signature-params\": {inputSigParams}", Encoding.ASCII.GetString(input.Span));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetSignatureInputPassesOriginalInputOfNewComponentsWhenAvailable(bool bindRequest)
        {
            string suffix = bindRequest ? ";req" : String.Empty;
            context.HasResponseValue = bindRequest;
            context.OnGetDerivedComponentValue = (comp) =>
            {
                return comp.ComponentName switch
                {
                    "@authority" => "unit.tests:1234",
                    _ => throw new Exception("Unexpected"),
                };
            };
            context.OnGetHeaderValues = context.OnGetRequestHeaderValues = (headerName) =>
            {
                return headerName switch
                {
                    "xyz" => new string[] { "a=b", },
                    _ => throw new Exception("Unexpected"),
                };
            };
            context.OnGetQueryParamValues = (name) =>
            {
                return name switch
                {
                    "abc" => new string[] { "def", "ghi", },
                    _ => throw new Exception("Unexpected"),
                };
            };

            SignatureInputSpec spec = new SignatureInputSpec("unitTest");
            spec.SignatureParameters
                .AddComponent(new DerivedComponent("@authority", bindRequest)
                {
                    // Introduce a mistake on purpose, so we can verify.
                    OriginalIdentifier = $"\"@test-authority\"{suffix}",
                })
                .AddComponent(new HttpHeaderDictionaryStructuredComponent("xyz", "a", bindRequest)
                {
                    // Introduce a mistake on purpose, so we can verify.
                    OriginalIdentifier = $"\"test-xyz\"{suffix};key=\"test-a\"",
                })
                .AddComponent(new QueryParamComponent("abc", bindRequest)
                {
                    // Introduce a mistake on purpose, so we can verify.
                    OriginalIdentifier = $"\"@query-param\"{suffix};name=\"test-abc\"",
                })
                ;

            ReadOnlyMemory<byte> input = context.GetSignatureInput(spec, out string outputSigParams);

            Assert.Equal($"(\"@test-authority\"{suffix} \"test-xyz\"{suffix};key=\"test-a\" \"@query-param\"{suffix};name=\"test-abc\")",
                outputSigParams);
            Assert.Equal(
                $"\"@test-authority\"{suffix}: unit.tests:1234\n" +
                $"\"test-xyz\"{suffix};key=\"test-a\": b\n" +
                $"\"@query-param\"{suffix};name=\"test-abc\": def\n" +
                $"\"@query-param\"{suffix};name=\"test-abc\": ghi\n" +
                $"\"@signature-params\": {outputSigParams}", Encoding.ASCII.GetString(input.Span));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetInputSignatureUsesByteSequenceEncodingCorrectly(bool bindRequest)
        {
            string suffix = bindRequest ? ";req" : String.Empty;
            string VeryLongValue = new String('*', 1025);

            context.HasResponseValue = bindRequest;
            context.OnGetHeaderValues = context.OnGetRequestHeaderValues = (headerName) =>
            {
                return headerName switch
                {
                    "xyz" => new string[] { "  a = b   ", "", "c=d, e=f", VeryLongValue, },
                    _ => throw new Exception("Unexpected"),
                };
            };

            SignatureInputSpec spec = new SignatureInputSpec("unitTest");
            spec.SignatureParameters
                .AddComponent(new HttpHeaderComponent("xyz", bindRequest, useByteSequence: true, fromTrailers: false))
                ;

            ReadOnlyMemory<byte> input = context.GetSignatureInput(spec, out string outputSigParams);

            Assert.Equal($"(\"xyz\"{suffix};bs)", outputSigParams);
            Assert.Equal(
                $"\"xyz\"{suffix};bs: :YSA9IGI=:, ::, :Yz1kLCBlPWY=:, :{Convert.ToBase64String(Encoding.ASCII.GetBytes(VeryLongValue))}:\n" +
                $"\"@signature-params\": {outputSigParams}", Encoding.ASCII.GetString(input.Span));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetInputSignatureUsesByteSequenceEncodingCorrectlyWithOriginalIdentifiers(bool bindRequest)
        {
            string suffix = bindRequest ? ";req" : String.Empty;

            context.HasResponseValue = bindRequest;
            context.OnGetHeaderValues = context.OnGetRequestHeaderValues = (headerName) =>
            {
                return headerName switch
                {
                    "xyz" => new string[] { "  a = b   ", "c=d, e=f", },
                    _ => throw new Exception("Unexpected"),
                };
            };

            SignatureInputSpec spec = new SignatureInputSpec("unitTest");
            spec.SignatureParameters
                .AddComponent(new HttpHeaderComponent("xyz", bindRequest, useByteSequence: true, fromTrailers: false)
                {
                    // Introduce a mistake on purpose, so we can verify that the original identifier is passed.
                    OriginalIdentifier = $"\"test-xyz\";bs{suffix}",
                })
                ;

            ReadOnlyMemory<byte> input = context.GetSignatureInput(spec, out string outputSigParams);

            Assert.Equal($"(\"test-xyz\";bs{suffix})", outputSigParams);
            Assert.Equal(
                $"\"test-xyz\";bs{suffix}: :YSA9IGI=:, :Yz1kLCBlPWY=:\n" +
                $"\"@signature-params\": {outputSigParams}", Encoding.ASCII.GetString(input.Span));
        }

        private static SignatureInputSpec MakeSignatureInput(SignatureComponent component)
        {
            SignatureInputSpec spec = new SignatureInputSpec("unitTest");
            spec.SignatureParameters.AddComponent(component);

            return spec;
        }
    }
}
