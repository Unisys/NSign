using Moq;
using NSign.Signatures;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using Xunit;

namespace NSign.Client
{
    partial class HttpRequestMessageExtensionsTests
    {
        [Fact]
        public void HasSignatureComponentDoesNotSupportCustomClassesDerivedFromSignatureComponent()
        {
            Mock<SignatureComponent> mockComp = new Mock<SignatureComponent>(
                MockBehavior.Loose, SignatureComponentType.HttpHeader, "test-component");
            mockComp.Setup(c => c.Accept(It.IsAny<ISignatureComponentVisitor>()))
                .Callback((ISignatureComponentVisitor visitor) => visitor.Visit(mockComp.Object));

            Assert.Throws<NotSupportedException>(() => request.HasSignatureComponent(mockComp.Object));
        }

        [Fact]
        public void HasSignatureComponentChecksRequestAndContentHeaders()
        {
            request.RequestUri = new Uri("http://localhost:8080/UnitTests/?a=b&c=d&c=cc&e=&non-param");

            StringContent content = new StringContent("hello world");
            content.Headers.Add("x-content-header", "exists");
            content.Headers.Add("x-content-header-empty", "");
            content.Headers.Add("x-content-dict", "a=b, c");
            content.Headers.Add("x-content-dict-malformed", "#");

            request.Content = content;
            request.Headers.Add("x-header", "exists");
            request.Headers.Add("x-header-empty", "");
            content.Headers.Add("x-dict", "a=b, c");
            content.Headers.Add("x-dict-malformed", "#");

            Assert.True(request.HasSignatureComponent(new HttpHeaderComponent("x-content-header")));
            Assert.True(request.HasSignatureComponent(new HttpHeaderComponent("x-content-header-empty")));
            Assert.False(request.HasSignatureComponent(new HttpHeaderComponent("x-content-missing")));
            Assert.True(request.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-content-dict", "a")));
            Assert.True(request.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-content-dict", "c")));
            Assert.False(request.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-content-dict", "x")));
            Assert.False(request.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-content-missing-dict", "x")));
            Assert.False(request.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-content-dict-malformed", "x")));

            Assert.True(request.HasSignatureComponent(new HttpHeaderComponent("x-header")));
            Assert.True(request.HasSignatureComponent(new HttpHeaderComponent("x-header-empty")));
            Assert.False(request.HasSignatureComponent(new HttpHeaderComponent("x-missing")));
            Assert.True(request.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-dict", "a")));
            Assert.True(request.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-dict", "c")));
            Assert.False(request.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-dict", "x")));
            Assert.False(request.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-missing-dict", "x")));
            Assert.False(request.HasSignatureComponent(new HttpHeaderDictionaryStructuredComponent("x-dict-malformed", "x")));

            Assert.True(request.HasSignatureComponent(new QueryParamsComponent("a")));
            Assert.True(request.HasSignatureComponent(new QueryParamsComponent("c")));
            Assert.True(request.HasSignatureComponent(new QueryParamsComponent("e")));
            Assert.False(request.HasSignatureComponent(new QueryParamsComponent("x")));
            Assert.False(request.HasSignatureComponent(new QueryParamsComponent("non-param")));

            Assert.True(request.HasSignatureComponent(SignatureComponent.Query));
            Assert.False(request.HasSignatureComponent(new SpecialtyComponent("@foo")));

            Assert.True(request.HasSignatureComponent(new HttpHeaderComponent("content-length")));
            request.Content = null;
            Assert.False(request.HasSignatureComponent(new HttpHeaderComponent("content-length")));

            request.Content = JsonContent.Create("blah"); // Does not provide a content length because it applies chunked encoding.
            Assert.False(request.HasSignatureComponent(new HttpHeaderComponent("content-length")));
        }

        [Theory]
        [InlineData("@signature-params")]
        [InlineData("@query-params")]
        [InlineData("@status")]
        [InlineData("@request-response")]
        public void HasSignatureComponentThrowsNotSupportedExceptionForUnsupportedSpecialtyComponents(string name)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(
                () => request.HasSignatureComponent(new SpecialtyComponent(name)));
            Assert.Equal($"Speciality component '{name}' must be added through the corresponding class.", ex.Message);
        }

        [Fact]
        public void HasSignatureComponentThrowsForRequestResponseComponent()
        {
            Assert.Throws<NotSupportedException>(
                () => request.HasSignatureComponent(new RequestResponseComponent("another")));
        }
    }
}
