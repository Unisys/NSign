using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NSign.Signatures;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSign.Client
{
    public sealed class SignatureVerificationHandlerTests
    {
        private readonly Mock<HttpMessageHandler> mockInnerHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        private readonly HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:8080/UnitTests/");
        private readonly HttpResponseMessage response = new HttpResponseMessage();
        private readonly Mock<IMessageVerifier> mockVerifier = new Mock<IMessageVerifier>(MockBehavior.Strict);
        private readonly SignatureVerificationOptions options = new SignatureVerificationOptions();
        private readonly SignatureVerificationHandler handler;

        public SignatureVerificationHandlerTests()
        {
            mockInnerHandler.Protected().Setup("Dispose", ItExpr.Is<bool>(d => d == true));
            mockInnerHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r == request), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            Mock<ILogger<SignatureVerificationHandler>> mockLogger =
                new Mock<ILogger<SignatureVerificationHandler>>(MockBehavior.Loose);
            mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            handler = new SignatureVerificationHandler(
                mockLogger.Object,
                mockVerifier.Object,
                new OptionsWrapper<SignatureVerificationOptions>(options))
            {
                InnerHandler = mockInnerHandler.Object,
            };
        }

        [Fact]
        public async Task SendAsyncCallsSignMessageAsync()
        {
            using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            mockVerifier.Setup(s => s.VerifyMessageAsync(
                It.Is<HttpResponseMessageContext>(c => c.Request == request && c.Response == response &&
                    c.Aborted == cancellationTokenSource.Token)))
                .Returns(Task.CompletedTask);

            await invoker.SendAsync(request, cancellationTokenSource.Token);

            mockVerifier.Verify(s => s.VerifyMessageAsync(It.IsAny<MessageContext>()), Times.Once());
        }
        //[Fact]
        //public async Task NoSignaturesCausesSignatureMissingException()
        //{
        //    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        //    SignatureMissingException ex = await Assert.ThrowsAsync<SignatureMissingException>(
        //        () => invoker.SendAsync(request, default));
        //    Assert.Equal("The message does not contain any signatures for verification. " +
        //                 "Consider removing signature verification or make sure messages are signed.",
        //        ex.Message);

        //    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        //        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        //}

        //[Theory]
        //[InlineData("first,second")]
        //[InlineData("unit")]
        //public async Task MissingSignaturesCausesSignatureMissingException(string availableSignatures)
        //{
        //    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        //    foreach (string sig in availableSignatures.Split(','))
        //    {
        //        response.Headers.Add("signature", $"{sig}=:dGVzdA==:"); // "test"
        //        response.Headers.Add("signature-input", $"{sig}=(\"@status\")");
        //    }

        //    SignatureMissingException ex = await Assert.ThrowsAsync<SignatureMissingException>(
        //        () => invoker.SendAsync(request, default));
        //    Assert.Equal("The message does not contain any signatures for verification. " +
        //                 "Consider removing signature verification or make sure messages are signed.",
        //        ex.Message);

        //    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        //        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        //}

        //[Theory]
        //[InlineData("abc=:1234:|test=:dGVzdA==:", "test=(")]
        //[InlineData("test=:dGVzdA==:|unittest2=:dGVzdA==:", "test=();key")]
        //public async Task MalformedSignatureInputCausesSignatureInputException(string signature, string signatureInput)
        //{
        //    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        //    foreach (string sig in signature.Split('|'))
        //    {
        //        response.Headers.Add("signature", sig);
        //    }
        //    foreach (string sigInput in signatureInput.Split('|'))
        //    {
        //        response.Headers.Add("signature-input", sigInput);
        //    }

        //    SignatureInputException ex = await Assert.ThrowsAsync<SignatureInputException>(
        //        () => invoker.SendAsync(request, default));
        //    Assert.Equal("Some signatures have input errors: test.", ex.Message);

        //    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        //        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        //}

        //[Theory]
        //[InlineData("abc=:1234:|unittest=:dGVzdA==:", "test=()")]
        //[InlineData("abc=:1234:|unittest=:dGVzdA==:", "test=()|unittest=")]
        //public async Task MissingSignatureInputCausesSignatureInputException(string signature, string signatureInput)
        //{
        //    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        //    foreach (string sig in signature.Split('|'))
        //    {
        //        response.Headers.Add("signature", sig);
        //    }
        //    foreach (string sigInput in signatureInput.Split('|'))
        //    {
        //        response.Headers.Add("signature-input", sigInput);
        //    }

        //    SignatureInputException ex = await Assert.ThrowsAsync<SignatureInputException>(
        //        () => invoker.SendAsync(request, default));
        //    Assert.Equal("Some signatures have input errors: unittest.", ex.Message);

        //    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        //        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        //}

        //[Theory]
        //[InlineData("abc=:1234:|unittest=:dGVzdA==:", "unittest=(\"@method\")")]
        //[InlineData("abc=:1234:|unittest=:dGVzdA==:", "unittest=();key=\"blah\"")]
        //public async Task MissingComponentCausesSignatureInputException(string signature, string signatureInput)
        //{
        //    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        //    foreach (string sig in signature.Split('|'))
        //    {
        //        response.Headers.Add("signature", sig);
        //    }
        //    foreach (string sigInput in signatureInput.Split('|'))
        //    {
        //        response.Headers.Add("signature-input", sigInput);
        //    }

        //    options.RequiredSignatureComponents.Add(SignatureComponent.Status);

        //    SignatureInputException ex = await Assert.ThrowsAsync<SignatureInputException>(
        //        () => invoker.SendAsync(request, default));
        //    Assert.Equal("Some signatures have input errors: unittest.", ex.Message);

        //    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        //        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        //}

        //[Theory]
        //[InlineData("unittest=:dGVzdA==:", "unittest=();expires=123", false)]
        //[InlineData("unittest=:dGVzdA==:", "unittest=();created=234", true)]
        //[InlineData("unittest=:dGVzdA==:", "unittest=();created=345;expires=346", true)]
        //public async Task ExpiredSignatureCausesSignatureVerificationFailedException(
        //    string signature,
        //    string signatureInput,
        //    bool useMaxSigAge)
        //{
        //    if (useMaxSigAge)
        //    {
        //        options.MaxSignatureAge = TimeSpan.FromMinutes(5);
        //    }

        //    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        //    foreach (string sig in signature.Split('|'))
        //    {
        //        response.Headers.Add("signature", sig);
        //    }
        //    foreach (string sigInput in signatureInput.Split('|'))
        //    {
        //        response.Headers.Add("signature-input", sigInput);
        //    }

        //    SignatureVerificationFailedException ex = await Assert.ThrowsAsync<SignatureVerificationFailedException>(
        //        () => invoker.SendAsync(request, default));
        //    Assert.Equal("Some signatures have failed verification: unittest.", ex.Message);

        //    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        //        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        //}

        ////[Theory]
        ////[InlineData("unittest=:dGVzdA==:|test=:dGVzdA==:", "unittest=()|test=();keyid=\"fail\"")]
        ////public async Task SignatureMismatchCausesSignatureVerificationFailedException(string signature, string signatureInput)
        ////{
        ////    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        ////    foreach (string sig in signature.Split('|'))
        ////    {
        ////        response.Headers.Add("signature", sig);
        ////    }
        ////    foreach (string sigInput in signatureInput.Split('|'))
        ////    {
        ////        response.Headers.Add("signature-input", sigInput);
        ////    }

        ////    mockVerifier
        ////        .Setup(v => v.VerifyAsync(
        ////            It.IsAny<SignatureParamsComponent>(),
        ////            It.IsAny<ReadOnlyMemory<byte>>(),
        ////            It.IsAny<ReadOnlyMemory<byte>>(),
        ////            It.IsAny<CancellationToken>()))
        ////        .ReturnsAsync(
        ////        (SignatureParamsComponent sigParams, ReadOnlyMemory<byte> input, ReadOnlyMemory<byte> expectedSig, CancellationToken cancel) =>
        ////        {
        ////            if (sigParams.KeyId == "fail")
        ////            {
        ////                return VerificationResult.SignatureMismatch;
        ////            }

        ////            return VerificationResult.SuccessfullyVerified;
        ////        });

        ////    SignatureVerificationFailedException ex = await Assert.ThrowsAsync<SignatureVerificationFailedException>(
        ////        () => invoker.SendAsync(request, default));
        ////    Assert.Equal("Some signatures have failed verification: test.", ex.Message);

        ////    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        ////        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        ////    mockVerifier.Verify(v => v.VerifyAsync(
        ////        It.IsAny<SignatureParamsComponent>(),
        ////        It.IsAny<ReadOnlyMemory<byte>>(),
        ////        It.IsAny<ReadOnlyMemory<byte>>(),
        ////        It.IsAny<CancellationToken>()),
        ////        Times.Exactly(2));
        ////}


        ////[Theory]
        ////[InlineData("unittest=:dGVzdA==:", "unittest=()")]
        ////public async Task SignatureVerificationThrows(string signature, string signatureInput)
        ////{
        ////    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        ////    foreach (string sig in signature.Split('|'))
        ////    {
        ////        response.Headers.Add("signature", sig);
        ////    }
        ////    foreach (string sigInput in signatureInput.Split('|'))
        ////    {
        ////        response.Headers.Add("signature-input", sigInput);
        ////    }

        ////    mockVerifier
        ////        .Setup(v => v.VerifyAsync(
        ////            It.IsAny<SignatureParamsComponent>(),
        ////            It.IsAny<ReadOnlyMemory<byte>>(),
        ////            It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
        ////        .ThrowsAsync(new Exception("Injected error"));

        ////    Exception ex = await Assert.ThrowsAsync<Exception>(
        ////        () => invoker.SendAsync(request, default));
        ////    Assert.Equal("Injected error", ex.Message);

        ////    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        ////        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        ////    mockVerifier.Verify(v => v.VerifyAsync(
        ////        It.IsAny<SignatureParamsComponent>(),
        ////        It.IsAny<ReadOnlyMemory<byte>>(),
        ////        It.IsAny<ReadOnlyMemory<byte>>(),
        ////        It.IsAny<CancellationToken>()),
        ////        Times.Once());
        ////}

        ////[Theory]
        ////[InlineData("unittest=:dGVzdA==:", "unittest=()")]
        ////public async Task SuccessfulVerification(string signature, string signatureInput)
        ////{
        ////    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        ////    foreach (string sig in signature.Split('|'))
        ////    {
        ////        response.Headers.Add("signature", sig);
        ////    }
        ////    foreach (string sigInput in signatureInput.Split('|'))
        ////    {
        ////        response.Headers.Add("signature-input", sigInput);
        ////    }

        ////    mockVerifier
        ////        .Setup(v => v.VerifyAsync(
        ////            It.IsAny<SignatureParamsComponent>(),
        ////            It.IsAny<ReadOnlyMemory<byte>>(),
        ////            It.IsAny<ReadOnlyMemory<byte>>(),
        ////            It.IsAny<CancellationToken>()))
        ////        .ReturnsAsync(
        ////        (SignatureParamsComponent sigParams, ReadOnlyMemory<byte> input, ReadOnlyMemory<byte> expectedSig, CancellationToken cancel) =>
        ////        {
        ////            return VerificationResult.SuccessfullyVerified;
        ////        });

        ////    HttpResponseMessage actualResponse = await invoker.SendAsync(request, default);
        ////    Assert.Same(response, actualResponse);

        ////    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        ////        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        ////    mockVerifier.Verify(v => v.VerifyAsync(
        ////        It.IsAny<SignatureParamsComponent>(),
        ////        It.IsAny<ReadOnlyMemory<byte>>(),
        ////        It.IsAny<ReadOnlyMemory<byte>>(),
        ////        It.IsAny<CancellationToken>()),
        ////        Times.Once());
        ////}

        //[Fact]
        //public async Task MissingCreatedInSignatureThrows()
        //{
        //    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        //    options.CreatedRequired = true;
        //    response.Headers.Add("signature", "unittest=:dGVzdA==:");
        //    response.Headers.Add("signature-input", "unittest=()");

        //    SignatureInputException ex = await Assert.ThrowsAsync<SignatureInputException>(
        //        () => invoker.SendAsync(request, default));
        //    Assert.Equal("Some signatures have input errors: unittest.", ex.Message);

        //    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        //        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        //}

        //[Fact]
        //public async Task MissingExpiresInSignatureThrows()
        //{
        //    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        //    options.ExpiresRequired = true;
        //    response.Headers.Add("signature", "unittest=:dGVzdA==:");
        //    response.Headers.Add("signature-input", "unittest=()");

        //    SignatureInputException ex = await Assert.ThrowsAsync<SignatureInputException>(
        //        () => invoker.SendAsync(request, default));
        //    Assert.Equal("Some signatures have input errors: unittest.", ex.Message);

        //    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        //        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        //}

        //[Fact]
        //public async Task MissingNonceInSignatureThrows()
        //{
        //    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        //    options.NonceRequired = true;
        //    response.Headers.Add("signature", "unittest=:dGVzdA==:");
        //    response.Headers.Add("signature-input", "unittest=()");

        //    SignatureInputException ex = await Assert.ThrowsAsync<SignatureInputException>(
        //        () => invoker.SendAsync(request, default));
        //    Assert.Equal("Some signatures have input errors: unittest.", ex.Message);

        //    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        //        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        //}

        //[Fact]
        //public async Task NonceVerificationFailureInSignatureThrows()
        //{
        //    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        //    options.NonceRequired = true;
        //    options.VerifyNonce = (input) =>
        //    {
        //        Assert.Equal("my-nonce", input.SignatureParameters.Nonce);

        //        return false;
        //    };

        //    response.Headers.Add("signature", "unittest=:dGVzdA==:");
        //    response.Headers.Add("signature-input", "unittest=();nonce=\"my-nonce\"");

        //    SignatureInputException ex = await Assert.ThrowsAsync<SignatureInputException>(
        //        () => invoker.SendAsync(request, default));
        //    Assert.Equal("Some signatures have input errors: unittest.", ex.Message);

        //    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        //        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        //}

        //[Fact]
        //public async Task MissingAlgorithmInSignatureThrows()
        //{
        //    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        //    options.AlgorithmRequired = true;
        //    response.Headers.Add("signature", "unittest=:dGVzdA==:");
        //    response.Headers.Add("signature-input", "unittest=()");

        //    SignatureInputException ex = await Assert.ThrowsAsync<SignatureInputException>(
        //        () => invoker.SendAsync(request, default));
        //    Assert.Equal("Some signatures have input errors: unittest.", ex.Message);

        //    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        //        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        //}

        //[Fact]
        //public async Task MissingKeyIdInSignatureThrows()
        //{
        //    using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        //    options.KeyIdRequired = true;
        //    response.Headers.Add("signature", "unittest=:dGVzdA==:");
        //    response.Headers.Add("signature-input", "unittest=()");

        //    SignatureInputException ex = await Assert.ThrowsAsync<SignatureInputException>(
        //        () => invoker.SendAsync(request, default));
        //    Assert.Equal("Some signatures have input errors: unittest.", ex.Message);

        //    mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
        //        "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        //}
    }
}
