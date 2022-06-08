using Moq;
using Xunit;

namespace NSign.Signatures
{
    public sealed class HttpHeaderComponentTests
    {
        private readonly HttpHeaderComponent header = new HttpHeaderComponent("x-My-HttpHeader");

        [Fact]
        public void ComponentTypeIsHttpHeader()
        {
            Assert.Equal(SignatureComponentType.HttpHeader, header.Type);
        }

        [Fact]
        public void ComponentNameIsNormalized()
        {
            Assert.Equal("x-my-httpheader", header.ComponentName);
        }

        [Fact]
        public void AcceptCallsVisitor()
        {
            Mock<ISignatureComponentVisitor> mockVisitor = new Mock<ISignatureComponentVisitor>(MockBehavior.Strict);

            mockVisitor.Setup(v => v.Visit(It.Is<HttpHeaderComponent>(c => c == header)));

            header.Accept(mockVisitor.Object);

            mockVisitor.Verify(v => v.Visit(It.IsAny<HttpHeaderComponent>()), Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CtorPassesBindRequest(bool bindRequest)
        {
            HttpHeaderComponent comp = new HttpHeaderComponent("blah", bindRequest);
            Assert.Equal(bindRequest, comp.BindRequest);
        }
    }
}
