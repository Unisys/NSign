using Moq;
using System;
using Xunit;

namespace NSign.Signatures
{
    partial class MessageContextTests
    {
        [Fact]
        public void HasSignatureComponentDoesNotSupportCustomClassesDerivedFromSignatureComponent()
        {
            Mock<SignatureComponent> mockComp = new Mock<SignatureComponent>(
                MockBehavior.Loose, SignatureComponentType.HttpHeader, "test-component");
            mockComp.Setup(c => c.Accept(It.IsAny<ISignatureComponentVisitor>()))
                .Callback((ISignatureComponentVisitor visitor) => visitor.Visit(mockComp.Object));

            Assert.Throws<NotSupportedException>(() => context.HasSignatureComponent(mockComp.Object));
        }

        [Fact]
        public void HasSignatureComponentChecksRequestAndContentHeaders()
        {
            context.OnGetHeaderValues = (headerName) =>
            {
                return headerName switch
                {
                    "x-header" => new string[] { "text", },
                    "x-header-empty" => new string[] { "", },
                    "x-dict" => new string[] { "a=b, c", },
                    "x-dict-malformed" => new string[] { "#", },
                    _ => Array.Empty<string>(),
                };
            };
            context.OnGetQueryParamValues = (paramName) =>
            {
                return paramName switch
                {
                    "a" => new string[] { "b", },
                    "c" => new string[] { "d", "cc", },
                    "e" => new string[] { "", },
                    _ => Array.Empty<string>(),
                };
            };
            context.OnGetRequestHeaderValues = (headerName) =>
            {
                return headerName switch
                {
                    _ => Array.Empty<string>(),
                };
            };

            Assert.True(context.HasSignatureComponent(new HttpHeaderComponent("x-header")));
            Assert.True(context.HasSignatureComponent(new HttpHeaderComponent("x-header-empty")));
            Assert.False(context.HasSignatureComponent(new HttpHeaderComponent("x-missing")));
            Assert.True(context.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-dict", "a")));
            Assert.True(context.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-dict", "c")));
            Assert.False(context.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-dict", "x")));
            Assert.False(context.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-missing-dict", "x")));
            Assert.False(context.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-dict-malformed", "x")));

            Assert.True(context.HasSignatureComponent(new QueryParamsComponent("a")));
            Assert.True(context.HasSignatureComponent(new QueryParamsComponent("c")));
            Assert.True(context.HasSignatureComponent(new QueryParamsComponent("e")));
            Assert.False(context.HasSignatureComponent(new QueryParamsComponent("x")));

            Assert.True(context.HasSignatureComponent(SignatureComponent.Method));
            Assert.True(context.HasSignatureComponent(SignatureComponent.RequestTargetUri));
            Assert.True(context.HasSignatureComponent(SignatureComponent.Authority));
            Assert.True(context.HasSignatureComponent(SignatureComponent.Scheme));
            Assert.True(context.HasSignatureComponent(SignatureComponent.RequestTarget));
            Assert.True(context.HasSignatureComponent(SignatureComponent.Path));
            Assert.True(context.HasSignatureComponent(SignatureComponent.Query));
            Assert.False(context.HasSignatureComponent(new DerivedComponent("@foo")));

            Assert.False(context.HasSignatureComponent(new HttpHeaderComponent("content-length")));
            context.OnGetHeaderValues = (headerName) =>
            {
                return headerName switch
                {
                    "content-length" => new string[] { "1234", },
                    _ => Array.Empty<string>(),
                };
            };
            Assert.True(context.HasSignatureComponent(new HttpHeaderComponent("content-length")));

            Assert.False(context.HasSignatureComponent(new RequestResponseComponent("test")));

            Assert.False(context.HasSignatureComponent(SignatureComponent.Status));
            context.HasResponseValue = true;
            Assert.True(context.HasSignatureComponent(SignatureComponent.Status));

            // The context caches parsed signature and signature input headers, so we just create a new object in order
            // to reset the cache.
            TestMessageContext newContext = new TestMessageContext(mockLogger.Object);
            newContext.OnGetRequestHeaderValues = (headerName) =>
            {
                return headerName switch
                {
                    "signature" => new string[] { "test=:dGVzdA==:", },
                    _ => Array.Empty<string>(),
                };
            };
            Assert.False(newContext.HasSignatureComponent(new RequestResponseComponent("missing")));
            Assert.True(newContext.HasSignatureComponent(new RequestResponseComponent("test")));
        }

        [Theory]
        [InlineData("@signature-params")]
        [InlineData("@query-params")]
        [InlineData("@request-response")]
        public void HasSignatureComponentThrowsNotSupportedExceptionForUnsupportedDerivedComponents(string name)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(
                () => context.HasSignatureComponent(new DerivedComponent(name)));
            Assert.Equal($"Derived component '{name}' must be added through the corresponding class.", ex.Message);
        }
    }
}
