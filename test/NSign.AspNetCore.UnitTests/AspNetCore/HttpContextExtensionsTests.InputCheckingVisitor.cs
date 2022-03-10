using Microsoft.AspNetCore.Http;
using Moq;
using NSign.Signatures;
using System;
using Xunit;

namespace NSign.AspNetCore
{
    partial class HttpContextExtensionsTests
    {
        [Fact]
        public void HasSignatureComponentDoesNotSupportCustomClassesDerivedFromSignatureComponent()
        {
            Mock<SignatureComponent> mockComp = new Mock<SignatureComponent>(
                MockBehavior.Loose, SignatureComponentType.HttpHeader, "test-component");
            mockComp.Setup(c => c.Accept(It.IsAny<ISignatureComponentVisitor>()))
                .Callback((ISignatureComponentVisitor visitor) => visitor.Visit(mockComp.Object));

            Assert.Throws<NotSupportedException>(() => httpContext.HasSignatureComponent(mockComp.Object));
        }

        [Fact]
        public void HasSignatureComponentChecksRequestAndContentHeaders()
        {
            HttpRequest request = httpContext.Request;
            request.Host = new HostString("localhost", 8000);
            request.PathBase = "/Base";
            request.Path = "/The/Path";
            request.QueryString = new QueryString("?a=b&c=d&c=cc&e=");

            HttpResponse response = httpContext.Response;
            response.Headers.Add("x-header", "exists");
            response.Headers.Add("x-header-empty", "");
            response.Headers.Add("x-dict", "a=b, c");
            response.Headers.Add("x-dict-malformed", "#");

            Assert.True(httpContext.HasSignatureComponent(new HttpHeaderComponent("x-header")));
            Assert.True(httpContext.HasSignatureComponent(new HttpHeaderComponent("x-header-empty")));
            Assert.False(httpContext.HasSignatureComponent(new HttpHeaderComponent("x-missing")));
            Assert.True(httpContext.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-dict", "a")));
            Assert.True(httpContext.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-dict", "c")));
            Assert.False(httpContext.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-dict", "x")));
            Assert.False(httpContext.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-missing-dict", "x")));
            Assert.False(httpContext.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-dict-malformed", "x")));

            Assert.True(httpContext.HasSignatureComponent(new QueryParamsComponent("a")));
            Assert.True(httpContext.HasSignatureComponent(new QueryParamsComponent("c")));
            Assert.True(httpContext.HasSignatureComponent(new QueryParamsComponent("e")));
            Assert.False(httpContext.HasSignatureComponent(new QueryParamsComponent("x")));

            Assert.True(httpContext.HasSignatureComponent(SignatureComponent.Query));
            Assert.False(httpContext.HasSignatureComponent(new DerivedComponent("@foo")));

            Assert.False(httpContext.HasSignatureComponent(new HttpHeaderComponent("content-length")));
            response.Headers.ContentLength = 123;
            Assert.True(httpContext.HasSignatureComponent(new HttpHeaderComponent("content-length")));

            Assert.False(httpContext.HasSignatureComponent(new RequestResponseComponent("test")));
            request.Headers.Add("signature", "test=:dGVzdA==:");
            Assert.False(httpContext.HasSignatureComponent(new RequestResponseComponent("missing")));
            Assert.True(httpContext.HasSignatureComponent(new RequestResponseComponent("test")));
        }

        [Theory]
        [InlineData("@signature-params")]
        [InlineData("@query-params")]
        [InlineData("@request-response")]
        public void HasSignatureComponentThrowsNotSupportedExceptionForUnsupportedDerivedComponents(string name)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(
                () => httpContext.HasSignatureComponent(new DerivedComponent(name)));
            Assert.Equal($"Derived component '{name}' must be added through the corresponding class.", ex.Message);
        }
    }
}
