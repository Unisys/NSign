using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Signatures
{
    public sealed class DefaultMessageVerifierTests
    {
        private readonly Mock<IVerifier> mockVerifier = new Mock<IVerifier>(MockBehavior.Strict);
        private readonly Mock<MessageContext> mockContext;
        private readonly SignatureVerificationOptions options = new SignatureVerificationOptions();
        private readonly DefaultMessageVerifier verifier;
        private string? result;

        public DefaultMessageVerifierTests()
        {
            Mock<ILogger<DefaultMessageVerifier>> mockLogger = new Mock<ILogger<DefaultMessageVerifier>>(MockBehavior.Loose);

            mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            mockContext = new Mock<MessageContext>(MockBehavior.Strict, mockLogger.Object);

            options.OnMissingSignatures = (ctx) =>
            {
                result = "MissingSignatures";
                return Task.CompletedTask;
            };

            options.OnSignatureInputError = (ctx, map) =>
            {
                result = $"SignatureInputError:{String.Join('|', map.Select(item => $"{item.Key}={item.Value}"))}";
                return Task.CompletedTask;
            };

            options.OnSignatureVerificationFailed = (ctx, map) =>
            {
                result = $"SignatureVerificationFailed:{String.Join('|', map.Select(item => $"{item.Key}={item.Value}"))}";
                return Task.CompletedTask;
            };

            options.OnSignatureVerificationSucceeded = (ctx) =>
            {
                result = "SignatureVerificationSucceeded";
                return Task.CompletedTask;
            };

            verifier = new DefaultMessageVerifier(mockLogger.Object, mockVerifier.Object);
        }

        [Fact]
        public async Task VerifyMessageAsyncThrowsOnMissingOptions()
        {
            mockContext.SetupGet(c => c.VerificationOptions).Returns((SignatureVerificationOptions?)null);

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => verifier.VerifyMessageAsync(mockContext.Object));
            Assert.Equal("The message context does not have verification options.", ex.Message);

            mockContext.VerifyGet(c => c.VerificationOptions, Times.Once());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task VerifyMessageAsyncReportsMissingSignatures(bool hasResponse)
        {
            options.SignaturesToVerify.Add("unitTest");

            mockContext.SetupGet(c => c.HasResponse).Returns(hasResponse);
            mockContext.SetupGet(c => c.VerificationOptions).Returns(options);

            if (hasResponse)
            {
                mockContext.Setup(c => c.GetHeaderValues(It.IsAny<string>())).Returns(Array.Empty<string>());
            }
            else
            {
                mockContext.Setup(c => c.GetRequestHeaderValues(It.IsAny<string>())).Returns(Array.Empty<string>());
            }

            await verifier.VerifyMessageAsync(mockContext.Object);

            Assert.Equal("MissingSignatures", result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task VerifyMessageAsyncReportsMissingSignaturesWithHeadersPresent(bool hasResponse)
        {
            options.SignaturesToVerify.Add("unitTest");

            mockContext.SetupGet(c => c.HasResponse).Returns(hasResponse);
            mockContext.SetupGet(c => c.VerificationOptions).Returns(options);

            if (hasResponse)
            {
                mockContext.Setup(c => c.GetHeaderValues("signature")).Returns(new string[] { "other=:blah:", });
                mockContext.Setup(c => c.GetHeaderValues("signature-input")).Returns(new string[] { "other=()", });
            }
            else
            {
                mockContext.Setup(c => c.GetRequestHeaderValues("signature")).Returns(new string[] { "other=:blah:", });
                mockContext.Setup(c => c.GetRequestHeaderValues("signature-input")).Returns(new string[] { "other=()", });
            }

            await verifier.VerifyMessageAsync(mockContext.Object);

            Assert.Equal("MissingSignatures", result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task VerifyMessageAsyncReportsSignatureInputError(bool hasResponse)
        {
            options.SignaturesToVerify.Add("first");
            options.SignaturesToVerify.Add("second");

            mockContext.SetupGet(c => c.HasResponse).Returns(hasResponse);
            mockContext.SetupGet(c => c.VerificationOptions).Returns(options);

            if (hasResponse)
            {
                mockContext.Setup(c => c.GetHeaderValues("signature")).Returns(new string[] { "first=:blah:, second=:blah:", });
                mockContext.Setup(c => c.GetHeaderValues("signature-input")).Returns(new string[] { "second=\"blah\"", });
            }
            else
            {
                mockContext.Setup(c => c.GetRequestHeaderValues("signature")).Returns(new string[] { "first=:blah:, second=:blah:", });
                mockContext.Setup(c => c.GetRequestHeaderValues("signature-input")).Returns(new string[] { "second=\"blah\"", });
            }

            await verifier.VerifyMessageAsync(mockContext.Object);

            Assert.Equal("SignatureInputError:first=SignatureInputNotFound|second=SignatureInputMalformed", result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task VerifyMessageAsyncReportsSignatureVerificationFailed(bool hasResponse)
        {
            CancellationToken cancellationToken = new CancellationToken();
            options.SignaturesToVerify.Add("test");
            options.AlgorithmRequired = options.CreatedRequired = options.ExpiresRequired = options.KeyIdRequired = options.NonceRequired = false;

            mockContext.SetupGet(c => c.HasResponse).Returns(hasResponse);
            mockContext.SetupGet(c => c.VerificationOptions).Returns(options);
            mockContext.SetupGet(c => c.Aborted).Returns(cancellationToken);

            if (hasResponse)
            {
                mockContext.Setup(c => c.GetHeaderValues("signature")).Returns(new string[] { "test=:blah:", });
                mockContext.Setup(c => c.GetHeaderValues("signature-input")).Returns(new string[] { "test=()", });
            }
            else
            {
                mockContext.Setup(c => c.GetRequestHeaderValues("signature")).Returns(new string[] { "test=:blah:", });
                mockContext.Setup(c => c.GetRequestHeaderValues("signature-input")).Returns(new string[] { "test=()", });
            }

            mockVerifier.Setup(v => v.VerifyAsync(
                It.IsAny<SignatureParamsComponent>(),
                It.Is<ReadOnlyMemory<byte>>(m => MatchesAscii(m, "\"@signature-params\": ()")),
                It.Is<ReadOnlyMemory<byte>>(m => MatchesBase64(m, "blah")),
                cancellationToken))
                .ReturnsAsync(VerificationResult.SignatureMismatch);

            await verifier.VerifyMessageAsync(mockContext.Object);

            Assert.Equal("SignatureVerificationFailed:test=SignatureMismatch", result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task VerifyMessageAsyncReportsSignatureVerificationSucceeded(bool hasResponse)
        {
            CancellationToken cancellationToken = new CancellationToken();
            options.SignaturesToVerify.Add("blah");
            options.AlgorithmRequired = options.CreatedRequired = options.ExpiresRequired = options.KeyIdRequired = options.NonceRequired = false;

            mockContext.SetupGet(c => c.HasResponse).Returns(hasResponse);
            mockContext.SetupGet(c => c.VerificationOptions).Returns(options);
            mockContext.SetupGet(c => c.Aborted).Returns(cancellationToken);

            if (hasResponse)
            {
                mockContext.Setup(c => c.GetHeaderValues("signature")).Returns(new string[] { "blah=:blah:", });
                mockContext.Setup(c => c.GetHeaderValues("signature-input")).Returns(new string[] { "blah=()", });
            }
            else
            {
                mockContext.Setup(c => c.GetRequestHeaderValues("signature")).Returns(new string[] { "blah=:blah:", });
                mockContext.Setup(c => c.GetRequestHeaderValues("signature-input")).Returns(new string[] { "blah=()", });
            }

            mockVerifier.Setup(v => v.VerifyAsync(
                It.IsAny<SignatureParamsComponent>(),
                It.Is<ReadOnlyMemory<byte>>(m => MatchesAscii(m, "\"@signature-params\": ()")),
                It.Is<ReadOnlyMemory<byte>>(m => MatchesBase64(m, "blah")),
                cancellationToken))
                .ReturnsAsync(VerificationResult.SuccessfullyVerified);

            await verifier.VerifyMessageAsync(mockContext.Object);

            Assert.Equal("SignatureVerificationSucceeded", result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task VerifyMessageAsyncReportsVerificationErrorResponseStatusOnExpiredSignature(int? maxAgeSeconds)
        {
            CancellationToken cancellationToken = new CancellationToken();
            TimeSpan? maxSignatureAge = maxAgeSeconds.HasValue ? TimeSpan.FromSeconds(maxAgeSeconds.Value) : (TimeSpan?)null;
            DateTimeOffset now = DateTimeOffset.Now;

            options.SignaturesToVerify.Add("unittest");
            options.CreatedRequired = options.ExpiresRequired = false;
            options.MaxSignatureAge = maxSignatureAge;

            mockContext.SetupGet(c => c.HasResponse).Returns(true);
            mockContext.SetupGet(c => c.VerificationOptions).Returns(options);
            mockContext.SetupGet(c => c.Aborted).Returns(cancellationToken);

            mockContext.Setup(c => c.GetHeaderValues("signature")).Returns(new string[] { "unittest=:Test:", });
            mockContext.Setup(c => c.GetHeaderValues("signature-input")).Returns(new string[] {
                $"unittest=();created={now.AddHours(-1).ToUnixTimeSeconds()};expires={now.AddHours(-0.5).ToUnixTimeSeconds()}",
            });

            await verifier.VerifyMessageAsync(mockContext.Object);

            Assert.Equal("SignatureVerificationFailed:unittest=SignatureExpired", result);
        }

        [Fact]
        public async Task VerifyMessageAsyncReportsSignatureInputErrorResponseStatusOnMandatoryButAbsentCreated()
        {
            options.SignaturesToVerify.Add("unittest");
            options.CreatedRequired = true;

            mockContext.SetupGet(c => c.HasResponse).Returns(true);
            mockContext.SetupGet(c => c.VerificationOptions).Returns(options);

            mockContext.Setup(c => c.GetHeaderValues("signature")).Returns(new string[] { "unittest=:Test:", });
            mockContext.Setup(c => c.GetHeaderValues("signature-input")).Returns(new string[] { "unittest=()", });

            await verifier.VerifyMessageAsync(mockContext.Object);

            Assert.Equal("SignatureInputError:unittest=SignatureInputComponentMissing", result);
        }

        [Fact]
        public async Task VerifyMessageAsyncReportsSignatureInputErrorResponseStatusOnMandatoryButAbsentExpires()
        {
            options.SignaturesToVerify.Add("unittest");
            options.ExpiresRequired = true;
            options.MaxSignatureAge = null;

            mockContext.SetupGet(c => c.HasResponse).Returns(true);
            mockContext.SetupGet(c => c.VerificationOptions).Returns(options);

            mockContext.Setup(c => c.GetHeaderValues("signature")).Returns(new string[] { "unittest=:Test:", });
            mockContext.Setup(c => c.GetHeaderValues("signature-input")).Returns(new string[] { "unittest=();created=1234", });

            await verifier.VerifyMessageAsync(mockContext.Object);

            Assert.Equal("SignatureInputError:unittest=SignatureInputComponentMissing", result);
        }

        [Fact]
        public async Task VerifyMessageAsyncReportsSignatureInputErrorResponseStatusOnMandatoryButAbsentNonce()
        {
            options.SignaturesToVerify.Add("unittest");
            options.ExpiresRequired = false;
            options.NonceRequired = true;
            options.MaxSignatureAge = null;

            mockContext.SetupGet(c => c.HasResponse).Returns(true);
            mockContext.SetupGet(c => c.VerificationOptions).Returns(options);

            mockContext.Setup(c => c.GetHeaderValues("signature")).Returns(new string[] { "unittest=:Test:", });
            mockContext.Setup(c => c.GetHeaderValues("signature-input")).Returns(new string[] { "unittest=();created=1234", });

            await verifier.VerifyMessageAsync(mockContext.Object);

            Assert.Equal("SignatureInputError:unittest=SignatureInputComponentMissing", result);
        }

        [Fact]
        public async Task VerifyMessageAsyncReportsSignatureInputErrorResponseStatusOnFailedNonceValidation()
        {
            bool nonceVerificationCalled = false;

            options.SignaturesToVerify.Add("unittest");
            options.ExpiresRequired = false;
            options.VerifyNonce = (input) =>
            {
                Assert.Equal("aaa555", input.SignatureParameters.Nonce);
                nonceVerificationCalled = true;
                return false;
            };
            options.MaxSignatureAge = null;

            mockContext.SetupGet(c => c.HasResponse).Returns(true);
            mockContext.SetupGet(c => c.VerificationOptions).Returns(options);

            mockContext.Setup(c => c.GetHeaderValues("signature")).Returns(new string[] { "unittest=:Test:", });
            mockContext.Setup(c => c.GetHeaderValues("signature-input")).Returns(new string[] { "unittest=();created=1234;nonce=\"aaa555\"", });

            await verifier.VerifyMessageAsync(mockContext.Object);

            Assert.Equal("SignatureInputError:unittest=SignatureInputComponentMissing", result);
            Assert.True(nonceVerificationCalled);
        }

        [Fact]
        public async Task VerifyMessageAsyncReportsSignatureInputErrorResponseStatusOnMandatoryButAbsentAlgorithm()
        {
            options.SignaturesToVerify.Add("unittest");
            options.ExpiresRequired = false;
            options.AlgorithmRequired = true;
            options.MaxSignatureAge = null;

            mockContext.SetupGet(c => c.HasResponse).Returns(true);
            mockContext.SetupGet(c => c.VerificationOptions).Returns(options);

            mockContext.Setup(c => c.GetHeaderValues("signature")).Returns(new string[] { "unittest=:Test:", });
            mockContext.Setup(c => c.GetHeaderValues("signature-input")).Returns(new string[] { "unittest=();created=1234", });

            await verifier.VerifyMessageAsync(mockContext.Object);

            Assert.Equal("SignatureInputError:unittest=SignatureInputComponentMissing", result);
        }

        [Fact]
        public async Task VerifyMessageAsyncReportsSignatureInputErrorResponseStatusOnMandatoryButAbsentKeyId()
        {
            options.SignaturesToVerify.Add("unittest");
            options.ExpiresRequired = false;
            options.KeyIdRequired = true;
            options.MaxSignatureAge = null;

            mockContext.SetupGet(c => c.HasResponse).Returns(true);
            mockContext.SetupGet(c => c.VerificationOptions).Returns(options);

            mockContext.Setup(c => c.GetHeaderValues("signature")).Returns(new string[] { "unittest=:Test:", });
            mockContext.Setup(c => c.GetHeaderValues("signature-input")).Returns(new string[] { "unittest=();created=1234", });

            await verifier.VerifyMessageAsync(mockContext.Object);

            Assert.Equal("SignatureInputError:unittest=SignatureInputComponentMissing", result);
        }

        [Fact]
        public async Task VerifyMessageAsyncReportsSignatureInputErrorResponseStatusOnMandatoryButComponent()
        {
            options.SignaturesToVerify.Add("unittest");
            options.AlgorithmRequired = options.CreatedRequired = options.ExpiresRequired = options.KeyIdRequired = options.NonceRequired = false;
            options.MaxSignatureAge = null;
            options.RequiredSignatureComponents.Add(SignatureComponent.Method);

            mockContext.SetupGet(c => c.HasResponse).Returns(true);
            mockContext.SetupGet(c => c.VerificationOptions).Returns(options);

            mockContext.Setup(c => c.GetHeaderValues("signature")).Returns(new string[] { "unittest=:Test:", });
            mockContext.Setup(c => c.GetHeaderValues("signature-input")).Returns(new string[] { "unittest=(\"@status\")", });

            await verifier.VerifyMessageAsync(mockContext.Object);

            Assert.Equal("SignatureInputError:unittest=SignatureInputComponentMissing", result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task VerifyMessageAsyncReportsSignatureVerificationFailedWhenVerificationThrows(bool hasResponse)
        {
            CancellationToken cancellationToken = new CancellationToken();
            options.SignaturesToVerify.Add("blah");
            options.AlgorithmRequired = options.CreatedRequired = options.ExpiresRequired = options.KeyIdRequired = options.NonceRequired = false;

            mockContext.SetupGet(c => c.HasResponse).Returns(hasResponse);
            mockContext.SetupGet(c => c.VerificationOptions).Returns(options);
            mockContext.SetupGet(c => c.Aborted).Returns(cancellationToken);

            if (hasResponse)
            {
                mockContext.Setup(c => c.GetHeaderValues("signature")).Returns(new string[] { "blah=:blah:", });
                mockContext.Setup(c => c.GetHeaderValues("signature-input")).Returns(new string[] { "blah=()", });
            }
            else
            {
                mockContext.Setup(c => c.GetRequestHeaderValues("signature")).Returns(new string[] { "blah=:blah:", });
                mockContext.Setup(c => c.GetRequestHeaderValues("signature-input")).Returns(new string[] { "blah=()", });
            }

            mockVerifier.Setup(v => v.VerifyAsync(
                It.IsAny<SignatureParamsComponent>(),
                It.Is<ReadOnlyMemory<byte>>(m => MatchesAscii(m, "\"@signature-params\": ()")),
                It.Is<ReadOnlyMemory<byte>>(m => MatchesBase64(m, "blah")),
                cancellationToken))
                .ThrowsAsync(new Exception("Injected error"));

            Exception ex = await Assert.ThrowsAsync<Exception>(() => verifier.VerifyMessageAsync(mockContext.Object));
            Assert.Equal("Injected error", ex.Message);
        }

        private static bool MatchesAscii(ReadOnlyMemory<byte> memory, string ascii)
        {
            return Encoding.ASCII.GetString(memory.Span) == ascii;
        }

        private static bool MatchesBase64(ReadOnlyMemory<byte> memory, string base64)
        {
            return Convert.ToBase64String(memory.Span) == base64;
        }
    }
}
