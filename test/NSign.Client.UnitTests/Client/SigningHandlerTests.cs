using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NSign.Http;
using NSign.Signatures;
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
        private readonly Mock<IMessageSigner> mockSigner = new Mock<IMessageSigner>(MockBehavior.Strict);
        private readonly HttpFieldOptions httpFieldOptions = new HttpFieldOptions();
        private readonly MessageSigningOptions signingOptions = new MessageSigningOptions();
        private readonly SigningHandler handler;

        public SigningHandlerTests()
        {
            mockInnerHandler.Protected().Setup("Dispose", ItExpr.Is<bool>(d => d == true));
            mockInnerHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r == request), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            handler = new SigningHandler(
                new NullLogger<SigningHandler>(),
                mockSigner.Object,
                new OptionsWrapper<HttpFieldOptions>(httpFieldOptions),
                new OptionsWrapper<MessageSigningOptions>(signingOptions))
                {
                    InnerHandler = mockInnerHandler.Object,
                };
        }

        [Fact]
        public async Task SendAsyncCallsSignMessageAsync()
        {
            using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            mockSigner.Setup(s => s.SignMessageAsync(
                It.Is<HttpRequestMessageContext>(c => c.Request == request && !c.HasResponse && c.Aborted == cancellationTokenSource.Token)))
                .Returns(Task.CompletedTask);

            await invoker.SendAsync(request, cancellationTokenSource.Token);

            mockSigner.Verify(s => s.SignMessageAsync(It.IsAny<MessageContext>()), Times.Once());
        }
    }
}
