using Moq;
using Xunit;

namespace NSign.Signatures
{
    public sealed class HttpHeaderStructuredFieldComponentTests
    {
        private readonly HttpHeaderStructuredFieldComponent component =
            new HttpHeaderStructuredFieldComponent("X-Unit-Test");


        [Fact]
        public void ComponentTypeIsHttpHeader()
        {
            Assert.Equal(SignatureComponentType.HttpHeader, component.Type);
        }

        [Fact]
        public void ComponentNameIsNormalized()
        {
            Assert.Equal("x-unit-test", component.ComponentName);
        }

        [Fact]
        public void ToStringWorks()
        {
            Assert.Equal("x-unit-test;sf", component.ToString());
        }

        [Fact]
        public void AcceptCallsVisitor()
        {
            Mock<ISignatureComponentVisitor> mockVisitor = new Mock<ISignatureComponentVisitor>(MockBehavior.Strict);

            mockVisitor.Setup(v => v.Visit(It.Is<HttpHeaderStructuredFieldComponent>(c => c == component)));

            component.Accept(mockVisitor.Object);

            mockVisitor.Verify(v => v.Visit(It.IsAny<HttpHeaderStructuredFieldComponent>()), Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CtorPassesBindRequest(bool bindRequest)
        {
            HttpHeaderComponent comp = new HttpHeaderStructuredFieldComponent("blah", bindRequest);
            Assert.Equal(bindRequest, comp.BindRequest);
        }
    }
}
