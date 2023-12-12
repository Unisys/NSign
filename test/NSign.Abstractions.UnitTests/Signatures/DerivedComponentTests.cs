using Moq;
using System;
using Xunit;

namespace NSign.Signatures
{
    public sealed class DerivedComponentTests
    {
        private readonly DerivedComponent derived = new DerivedComponent("@my-derived");

        [Theory]
        [InlineData("header-component")]
        public void CtorThrowsForUnsupportedComponentName(string name)
        {
            ArgumentOutOfRangeException aoorex = Assert.Throws<ArgumentOutOfRangeException>(() => new DerivedComponent(name));

            Assert.Equal("name", aoorex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void CtorThrowsForUnsupportedComponentNameFromBase(string? name)
        {
            ArgumentNullException aoorex = Assert.Throws<ArgumentNullException>(() => new DerivedComponent(name!));

            Assert.Equal("componentName", aoorex.ParamName);
        }

        [Fact]
        public void ComponentTypeIsderived()
        {
            Assert.Equal(SignatureComponentType.Derived, derived.Type);
        }

        [Fact]
        public void ComponentNameIsNormalized()
        {
            Assert.Equal("@my-derived", derived.ComponentName);
        }

        [Fact]
        public void AcceptCallsVisitor()
        {
            Mock<ISignatureComponentVisitor> mockVisitor = new Mock<ISignatureComponentVisitor>(MockBehavior.Strict);

            mockVisitor.Setup(v => v.Visit(It.Is<DerivedComponent>(c => c == derived)));

            derived.Accept(mockVisitor.Object);

            mockVisitor.Verify(v => v.Visit(It.IsAny<DerivedComponent>()), Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CtorPassesBindRequest(bool bindRequest)
        {
            DerivedComponent comp = new DerivedComponent("@method", bindRequest);
            Assert.Equal(bindRequest, comp.BindRequest);
        }
    }
}
