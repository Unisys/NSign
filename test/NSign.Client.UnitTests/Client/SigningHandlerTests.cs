using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NSign.Signatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Client
{
    public sealed class SigningHandlerTests
    {
        private readonly Mock<HttpMessageHandler> mockInnerHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        private readonly HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:8080/UnitTests/");
        private readonly HttpResponseMessage response = new HttpResponseMessage();
        private readonly Mock<ISigner> mockSigner = new Mock<ISigner>(MockBehavior.Strict);
        private readonly MessageSigningOptions options = new MessageSigningOptions();
        private readonly SigningHandler handler;

        public SigningHandlerTests()
        {
            mockInnerHandler.Protected().Setup("Dispose", ItExpr.Is<bool>(d => d == true));

            options.SignatureName = "unit-tests";

            request.Headers.Add("my-header", "blah");
            request.Headers.Add("my-generic-dict", "a=b, b=c");

            handler = new SigningHandler(
                new NullLogger<SigningHandler>(),
                mockSigner.Object,
                new OptionsWrapper<MessageSigningOptions>(options))
            {
                InnerHandler = mockInnerHandler.Object,
            };
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendAsyncThrowsIfSignatureNameMissing(string name)
        {
            using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

            options.SignatureName = name;

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => invoker.SendAsync(request, default));
            Assert.Equal("The SignatureName must be set to a non-blank string. Signing failed.", ex.Message);
        }

        [Fact]
        public async Task SendAsyncThrowsSignatureComponentMissingExceptionForMissingMandatoryComponents()
        {
            SignatureComponentMissingException ex;
            using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

            // HTTP header component.
            options.ComponentsToInclude.Clear();
            options.WithMandatoryComponent(SignatureComponent.ContentType);
            options.UseUpdateSignatureParams = false;

            ex = await Assert.ThrowsAsync<SignatureComponentMissingException>(
                () => invoker.SendAsync(request, default));
            Assert.Equal("The signature component 'content-type' does not exist but is required.", ex.Message);

            // HTTP dictionary-structured header component.
            options.ComponentsToInclude.Clear();
            options.WithMandatoryComponent(new HttpHeaderDictionaryStructuredComponent("my-dict", "foo"));
            options.UseUpdateSignatureParams = false;

            ex = await Assert.ThrowsAsync<SignatureComponentMissingException>(
                () => invoker.SendAsync(request, default));
            Assert.Equal("The signature component 'my-dict;key=\"foo\"' does not exist but is required.", ex.Message);

            // HTTP dictionary-structured header component for existing header but missing key.
            options.ComponentsToInclude.Clear();
            options.WithMandatoryComponent(new HttpHeaderDictionaryStructuredComponent("my-generic-dict", "foo"));
            options.UseUpdateSignatureParams = false;

            ex = await Assert.ThrowsAsync<SignatureComponentMissingException>(
                () => invoker.SendAsync(request, default));
            Assert.Equal("The signature component 'my-generic-dict;key=\"foo\"' does not exist but is required.", ex.Message);
        }

        [Fact]
        public async Task SendAsyncDoesNotThrowForMissingOptionalComponents()
        {
            using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

            options.ComponentsToInclude.Clear();
            options.WithOptionalComponent(SignatureComponent.ContentType);
            options.WithOptionalComponent(new HttpHeaderDictionaryStructuredComponent("missing-dict", "abc"));
            options.WithOptionalComponent(new QueryParamsComponent("missing"));
            options.WithOptionalComponent(new DerivedComponent("@blah"));
            options.UseUpdateSignatureParams = false;

            mockInnerHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r == request && VerifySignature(r, "unit-tests=()", "unit-tests=:Eg==:")),
                ItExpr.Is<CancellationToken>(c => c == CancellationToken.None))
                .ReturnsAsync(response);

            mockSigner.Setup(s => s.SignAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new byte[] { 0x12, });

            Assert.Same(response, await invoker.SendAsync(request, default));

            mockSigner.Verify(s => s.SignAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);

            mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
                "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task SendAsyncSetsParametersAndUsesUpdateSignatureParams()
        {
            using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

            options.ComponentsToInclude.Clear();
            options.UseUpdateSignatureParams = true;
            options.SetParameters = (x) => x.WithCreated(DateTimeOffset.UnixEpoch).Expires = DateTimeOffset.UnixEpoch.AddSeconds(5);

            mockInnerHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r == request && VerifySignature(r, "unit-tests=();created=0;expires=5", "unit-tests=:QQ==:")),
                ItExpr.Is<CancellationToken>(c => c == CancellationToken.None))
                .ReturnsAsync(response);

            mockSigner.Setup(s => s.UpdateSignatureParams(
                It.Is<SignatureParamsComponent>(c => c.Expires == DateTimeOffset.UnixEpoch.AddSeconds(5))));
            mockSigner.Setup(s => s.SignAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new byte[] { 0x41, });

            Assert.Same(response, await invoker.SendAsync(request, default));

            mockSigner.Verify(s => s.UpdateSignatureParams(It.IsAny<SignatureParamsComponent>()), Times.Once);
            mockSigner.Verify(s => s.SignAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
            mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
                "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Theory]
        [InlineData("@query-params", "The '@query-params' component must have the 'name' parameter set.")]
        [InlineData("@status", "The '@status' component cannot be included in request signatures.")]
        [InlineData("@request-response", "The '@request-response' component must have the 'key' parameter set.")]
        [InlineData("@blah", "Non-standard derived signature component '@blah' cannot be retrieved.")]
        public async Task SendAsyncThrowsNotSupportedExceptionForDerivedComponentsNotSupportedOnRequests(string name, string expectedMessage)
        {
            using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

            options.ComponentsToInclude.Clear();
            options.WithMandatoryComponent(new DerivedComponent(name));
            options.UseUpdateSignatureParams = false;

            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(
                () => invoker.SendAsync(request, default));
            Assert.Equal(expectedMessage, ex.Message);
        }

        private static bool VerifySignature(HttpRequestMessage request, string expectedInput, string expectedSignature)
        {
            return request.Headers.TryGetValues("Signature-Input", out IEnumerable<string> actualInput) &&
                actualInput.SequenceEqual(new string[] { expectedInput }) &&
                request.Headers.TryGetValues("Signature", out IEnumerable<string> actualSignatures) &&
                actualSignatures.SequenceEqual(new string[] { expectedSignature });
        }
    }
}
