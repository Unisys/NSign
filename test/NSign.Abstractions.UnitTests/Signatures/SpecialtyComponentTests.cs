using Moq;
using System;
using Xunit;

namespace NSign.Signatures
{
    public sealed class SpecialtyComponentTests
    {
        private readonly SpecialtyComponent specialty = new SpecialtyComponent("@my-specialty");

        [Theory]
        [InlineData("header-component")]
        public void CtorThrowsForUnsupportedComponentName(string name)
        {
            ArgumentOutOfRangeException aoorex = Assert.Throws<ArgumentOutOfRangeException>(() => new SpecialtyComponent(name));

            Assert.Equal("name", aoorex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void CtorThrowsForUnsupportedComponentNameFromBase(string name)
        {
            ArgumentNullException aoorex = Assert.Throws<ArgumentNullException>(() => new SpecialtyComponent(name));

            Assert.Equal("componentName", aoorex.ParamName);
        }

        [Fact]
        public void ComponentTypeIsSpecialty()
        {
            Assert.Equal(SignatureComponentType.Specialty, specialty.Type);
        }

        [Fact]
        public void ComponentNameIsNormalized()
        {
            Assert.Equal("@my-specialty", specialty.ComponentName);
        }

        [Fact]
        public void AcceptCallsVisitor()
        {
            Mock<ISignatureComponentVisitor> mockVisitor = new Mock<ISignatureComponentVisitor>(MockBehavior.Strict);

            mockVisitor.Setup(v => v.Visit(It.Is<SpecialtyComponent>(c => c == specialty)));

            specialty.Accept(mockVisitor.Object);

            mockVisitor.Verify(v => v.Visit(It.IsAny<SpecialtyComponent>()), Times.Once);
        }
    }
}
