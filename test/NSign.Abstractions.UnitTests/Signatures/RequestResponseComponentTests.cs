using Moq;
using System;
using Xunit;

namespace NSign.Signatures
{
    public sealed class RequestResponseComponentTests
    {
        private readonly RequestResponseComponent requestResponse = new RequestResponseComponent("mySig");

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CtorValidatesInput(string key)
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new RequestResponseComponent(key));
            Assert.Equal("key", ex.ParamName);
        }

        [Fact]
        public void ComponentTypeIsDerived()
        {
            Assert.Equal(SignatureComponentType.Derived, requestResponse.Type);
        }

        [Fact]
        public void ComponentNameIsNormalized()
        {
            Assert.Equal("@request-response", requestResponse.ComponentName);
        }

        [Fact]
        public void KeyIsPassedAsIs()
        {
            Assert.Equal("mySig", requestResponse.Key);
        }

        [Fact]
        public void EqualsWorks()
        {
            RequestResponseComponent c1 = new RequestResponseComponent("mySig");
            RequestResponseComponent c2 = new RequestResponseComponent("MySig");
            RequestResponseComponent c3 = new RequestResponseComponent("My-Sig");

            Assert.True(requestResponse.Equals(c1));
            Assert.True(c1.Equals(requestResponse));
            Assert.False(requestResponse.Equals(c2));
            Assert.False(requestResponse.Equals(c3));

            Assert.False(c1.Equals((object?)null));
            Assert.False(c1!.Equals(new object()));

            Assert.True(requestResponse.Equals(requestResponse));
            Assert.True(requestResponse.Equals(c1));
        }

        [Fact]
        public void AcceptCallsVisitor()
        {
            Mock<ISignatureComponentVisitor> mockVisitor = new Mock<ISignatureComponentVisitor>(MockBehavior.Strict);

            mockVisitor.Setup(v => v.Visit(It.Is<RequestResponseComponent>(c => c == requestResponse)));

            requestResponse.Accept(mockVisitor.Object);

            mockVisitor.Verify(v => v.Visit(It.IsAny<RequestResponseComponent>()), Times.Once);
        }
    }
}
