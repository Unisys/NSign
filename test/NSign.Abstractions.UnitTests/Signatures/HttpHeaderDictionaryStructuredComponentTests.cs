using Moq;
using System;
using Xunit;

namespace NSign.Signatures
{
    public sealed class HttpHeaderDictionaryStructuredComponentTests
    {
        private readonly HttpHeaderDictionaryStructuredComponent dictHeader =
            new HttpHeaderDictionaryStructuredComponent("x-My-Dict-Header", "MyKey");

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CtorValidatesInput(string key)
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
                () => new HttpHeaderDictionaryStructuredComponent("some-header", key));
            Assert.Equal("key", ex.ParamName);
        }

        [Fact]
        public void ComponentTypeIsHttpHeader()
        {
            Assert.Equal(SignatureComponentType.HttpHeader, dictHeader.Type);
        }

        [Fact]
        public void ComponentNameIsNormalized()
        {
            Assert.Equal("x-my-dict-header", dictHeader.ComponentName);
        }

        [Fact]
        public void ToStringWorks()
        {
            Assert.Equal("x-my-dict-header;key=MyKey", dictHeader.ToString());
        }

        [Fact]
        public void KeyIsPassedAsIs()
        {
            Assert.Equal("MyKey", dictHeader.Key);
        }

        [Fact]
        public void EqualsWorks()
        {
            HttpHeaderDictionaryStructuredComponent c1 = new HttpHeaderDictionaryStructuredComponent("x-My-Dict-Header", "MyKey");
            HttpHeaderDictionaryStructuredComponent c2 = new HttpHeaderDictionaryStructuredComponent("x-My-Dict-Header", "mykey");
            HttpHeaderDictionaryStructuredComponent c3 = new HttpHeaderDictionaryStructuredComponent("x-my-dict-header", "MyKey");
            HttpHeaderDictionaryStructuredComponent c4 = new HttpHeaderDictionaryStructuredComponent("x-my-dict-header", "OtherKey");
            Comp c5 = new Comp("x-my-dict-header", "MyKey");

            Assert.True(dictHeader.Equals(c1));
            Assert.True(c1.Equals(dictHeader));
            Assert.False(dictHeader.Equals(c2));
            Assert.True(dictHeader.Equals(c3));
            Assert.False(dictHeader.Equals(c4));
            Assert.False(dictHeader.Equals(c5));

            Assert.False(c1.Equals((object?)null));
            Assert.False(c1!.Equals(new object()));

            Assert.True(dictHeader.Equals(dictHeader));
        }

        [Fact]
        public void AcceptCallsVisitor()
        {
            Mock<ISignatureComponentVisitor> mockVisitor = new Mock<ISignatureComponentVisitor>(MockBehavior.Strict);

            mockVisitor.Setup(v => v.Visit(It.Is<HttpHeaderDictionaryStructuredComponent>(c => c == dictHeader)));

            dictHeader.Accept(mockVisitor.Object);

            mockVisitor.Verify(v => v.Visit(It.IsAny<HttpHeaderDictionaryStructuredComponent>()), Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CtorPassesBindRequest(bool bindRequest)
        {
            HttpHeaderDictionaryStructuredComponent comp = new HttpHeaderDictionaryStructuredComponent(
                "blah", "mykey", bindRequest);
            Assert.Equal(bindRequest, comp.BindRequest);
        }

        private sealed class Comp : HttpHeaderDictionaryStructuredComponent
        {
            public Comp(string name, string key) : base(name, key)
            {
            }
        }
    }
}
