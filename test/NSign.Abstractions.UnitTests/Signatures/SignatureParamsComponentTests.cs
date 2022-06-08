using Moq;
using System;
using Xunit;

namespace NSign.Signatures
{
    public sealed class SignatureParamsComponentTests
    {
        private readonly SignatureParamsComponent signatureParams = new SignatureParamsComponent();

        [Fact]
        public void CtorWithValueParses()
        {
            string input = "(\"@method\" \"@query-param\";name=\"foo\" \"my-header\";key=\"blah\" \"content-type\");created=0";
            SignatureParamsComponent signatureParams = new SignatureParamsComponent(input);

            Assert.Collection(signatureParams.Components,
                (c) => Assert.Equal(SignatureComponent.Method, c),
                (c) => Assert.Equal(new QueryParamComponent("Foo"), c),
                (c) => Assert.Equal(new HttpHeaderDictionaryStructuredComponent("My-Header", "blah"), c),
                (c) => Assert.Equal(SignatureComponent.ContentType, c));

            Assert.True(signatureParams.Created.HasValue);
            Assert.Equal(DateTimeOffset.UnixEpoch, signatureParams.Created!.Value);
            Assert.Equal(input, signatureParams.OriginalValue);
        }

        [Fact]
        public void ComponentTypeIsDerived()
        {
            Assert.Equal(SignatureComponentType.Derived, signatureParams.Type);
        }

        [Fact]
        public void ComponentNameIsNormalized()
        {
            Assert.Equal("@signature-params", signatureParams.ComponentName);
        }

        [Fact]
        public void AcceptCallsVisitor()
        {
            Mock<ISignatureComponentVisitor> mockVisitor = new Mock<ISignatureComponentVisitor>(MockBehavior.Strict);

            mockVisitor.Setup(v => v.Visit(It.Is<SignatureParamsComponent>(c => c == signatureParams)));

            signatureParams.Accept(mockVisitor.Object);

            mockVisitor.Verify(v => v.Visit(It.IsAny<SignatureParamsComponent>()), Times.Once);
        }

        [Fact]
        public void FluentInterfaceWorks()
        {
            signatureParams
                .AddComponent(SignatureComponent.Authority)
                .AddComponent(SignatureComponent.Digest)
                .AddComponent(SignatureComponent.Path)
                .WithAlgorithm(SignatureAlgorithm.HmacSha256)
                .WithCreatedNow()
                .WithExpires(DateTimeOffset.UtcNow.AddSeconds(30))
                .WithKeyId("MyKeyId")
                .WithNonce("my-nonce");

            Assert.Collection(signatureParams.Components,
              (c) => Assert.Equal(SignatureComponent.Authority, c),
              (c) => Assert.Equal(SignatureComponent.Digest, c),
              (c) => Assert.Equal(SignatureComponent.Path, c));

            Assert.Equal("hmac-sha256", signatureParams.Algorithm);
            Assert.True(signatureParams.Created.HasValue);
            Assert.InRange(signatureParams.Created!.Value, DateTimeOffset.UtcNow.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
            Assert.True(signatureParams.Expires.HasValue);
            Assert.InRange(signatureParams.Expires!.Value, DateTimeOffset.UtcNow.AddSeconds(29), DateTimeOffset.UtcNow.AddSeconds(31));
            Assert.Equal("MyKeyId", signatureParams.KeyId);
            Assert.Equal("my-nonce", signatureParams.Nonce);

            // More tests.
            signatureParams
                .WithExpires(TimeSpan.FromSeconds(10))
                .WithAlgorithm(null);
            Assert.InRange(signatureParams.Expires.Value, DateTimeOffset.UtcNow.AddSeconds(9), DateTimeOffset.UtcNow.AddSeconds(11));
            Assert.Null(signatureParams.Algorithm);
        }

        [Fact]
        public void AddComponentThrowsForSignatureParamsComponent()
        {
            InvalidOperationException ex;

            ex = Assert.Throws<InvalidOperationException>(() => signatureParams.AddComponent(new SignatureParamsComponent()));
            Assert.Equal("Cannot add a '@signature-params' component to a SignatureParamsComponent.", ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() => signatureParams.AddComponent(new DerivedComponent("@signature-params")));
            Assert.Equal("Cannot add a '@signature-params' component to a SignatureParamsComponent.", ex.Message);
        }
    }
}
