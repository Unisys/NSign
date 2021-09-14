using Moq;
using System;
using Xunit;

namespace NSign.Signatures
{
    public sealed class SignatureComponentTests
    {
        [Fact]
        public void CtorValidatesComponentType()
        {
            ArgumentOutOfRangeException aoorex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new CompA(SignatureComponentType.Unknown, null));
            Assert.Equal("type", aoorex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CtorValidatesComponentName(string name)
        {
            ArgumentNullException anex = Assert.Throws<ArgumentNullException>(
                () => new CompA(SignatureComponentType.HttpHeader, name));
            Assert.Equal("componentName", anex.ParamName);
        }

        [Fact]
        public void EqualsWorks()
        {
            CompA c1 = new CompA(SignatureComponentType.HttpHeader, "Equals-Works");
            CompA c2 = new CompA(SignatureComponentType.HttpHeader, "equals-works");
            CompB c3 = new CompB(SignatureComponentType.HttpHeader, "Equals-Works");
            CompA c4 = new CompA(SignatureComponentType.HttpHeader, "equalsworks");

            Assert.False(c1.Equals(null));
            Assert.False(c1.Equals(new object()));

            Assert.True(c1.Equals(c1));
            Assert.True(c1.Equals(c2));
            Assert.True(c2.Equals(c1));

            Assert.False(c1.Equals(c3));
            Assert.False(c1.Equals(c4));
        }

        [Fact]
        public void AcceptCallsVisitor()
        {
            Mock<ISignatureComponentVisitor> mockVisitor = new Mock<ISignatureComponentVisitor>(MockBehavior.Strict);
            CompA comp = new CompA(SignatureComponentType.HttpHeader, "Accept-Calls-Visitor");

            mockVisitor.Setup(v => v.Visit(It.Is<SignatureComponent>(c => c == comp)));

            comp.Accept(mockVisitor.Object);

            mockVisitor.Verify(v => v.Visit(It.IsAny<SignatureComponent>()), Times.Once);
        }

        private sealed class CompA : SignatureComponent
        {
            public CompA(SignatureComponentType type, string componentName) : base(type, componentName)
            {
            }
        }

        private sealed class CompB : SignatureComponent
        {
            public CompB(SignatureComponentType type, string componentName) : base(type, componentName)
            {
            }
        }
    }
}
