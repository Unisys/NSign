using Moq;
using System;
using Xunit;

namespace NSign.Signatures
{
    public sealed class QueryParamsComponentTests
    {
        private readonly QueryParamsComponent queryParams = new QueryParamsComponent("myParam");

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CtorValidatesInput(string name)
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new QueryParamsComponent(name));
            Assert.Equal("name", ex.ParamName);
        }

        [Fact]
        public void ComponentTypeIsDerived()
        {
            Assert.Equal(SignatureComponentType.Derived, queryParams.Type);
        }

        [Fact]
        public void ComponentNameIsNormalized()
        {
            Assert.Equal("@query-params", queryParams.ComponentName);
        }

        [Fact]
        public void NameParameterIsNormalized()
        {
            Assert.Equal("myparam", queryParams.Name);
        }

        [Fact]
        public void EqualsWorks()
        {
            QueryParamsComponent c1 = new QueryParamsComponent("myParam");
            QueryParamsComponent c2 = new QueryParamsComponent("MyParam");
            QueryParamsComponent c3 = new QueryParamsComponent("my-param");

            Assert.True(queryParams.Equals(c1));
            Assert.True(c1.Equals(queryParams));
            Assert.True(queryParams.Equals(c2));
            Assert.False(queryParams.Equals(c3));

            Assert.False(c1.Equals(null));
            Assert.False(c1.Equals(new object()));

            Assert.True(queryParams.Equals(queryParams));
        }

        [Fact]
        public void AcceptCallsVisitor()
        {
            Mock<ISignatureComponentVisitor> mockVisitor = new Mock<ISignatureComponentVisitor>(MockBehavior.Strict);

            mockVisitor.Setup(v => v.Visit(It.Is<QueryParamsComponent>(c => c == queryParams)));

            queryParams.Accept(mockVisitor.Object);

            mockVisitor.Verify(v => v.Visit(It.IsAny<QueryParamsComponent>()), Times.Once);
        }
    }
}
