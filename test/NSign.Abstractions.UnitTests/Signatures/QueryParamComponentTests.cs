using Moq;
using System;
using Xunit;

namespace NSign.Signatures
{
    public sealed class QueryParamComponentTests
    {
        private readonly QueryParamComponent queryParam = new QueryParamComponent("myParam");

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CtorValidatesInput(string name)
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new QueryParamComponent(name));
            Assert.Equal("name", ex.ParamName);
        }

        [Fact]
        public void ComponentTypeIsDerived()
        {
            Assert.Equal(SignatureComponentType.Derived, queryParam.Type);
        }

        [Fact]
        public void ComponentNameIsNormalized()
        {
            Assert.Equal("@query-param", queryParam.ComponentName);
        }

        [Fact]
        public void NameParameterIsNormalized()
        {
            Assert.Equal("myparam", queryParam.Name);
        }

        [Fact]
        public void EqualsWorks()
        {
            QueryParamComponent c1 = new QueryParamComponent("myParam");
            QueryParamComponent c2 = new QueryParamComponent("MyParam");
            QueryParamComponent c3 = new QueryParamComponent("my-param");

            Assert.True(queryParam.Equals(c1));
            Assert.True(c1.Equals(queryParam));
            Assert.True(queryParam.Equals(c2));
            Assert.False(queryParam.Equals(c3));

            Assert.False(c1.Equals((object?)null));
            Assert.False(c1!.Equals(new object()));

            Assert.True(queryParam.Equals(queryParam));
        }

        [Fact]
        public void AcceptCallsVisitor()
        {
            Mock<ISignatureComponentVisitor> mockVisitor = new Mock<ISignatureComponentVisitor>(MockBehavior.Strict);

            mockVisitor.Setup(v => v.Visit(It.Is<QueryParamComponent>(c => c == queryParam)));

            queryParam.Accept(mockVisitor.Object);

            mockVisitor.Verify(v => v.Visit(It.IsAny<QueryParamComponent>()), Times.Once);
        }
    }
}
