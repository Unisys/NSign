using NSign.Signatures;
using System;
using Xunit;
using static NSign.MessageSigningOptions;

namespace NSign
{
    public sealed class MessageSigningOptionsTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ComponentSpecCtorDisallowsSignatureParamsComponent(bool mandatory)
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(
                () => new ComponentSpec(new SignatureParamsComponent(), mandatory));
            Assert.Equal("A SignatureParamsComponent cannot be added explicitly; it is always added automatically.", ex.Message);
        }

        [Fact]
        public void WithMandatoryComponentsAddsMandatoryComponent()
        {
            MessageSigningOptions options = new MessageSigningOptions()
                .WithMandatoryComponent(SignatureComponent.Method);

            Assert.Collection(options.ComponentsToInclude,
                (comp) =>
                {
                    Assert.Same(SignatureComponent.Method, comp.Component);
                    Assert.True(comp.Mandatory);
                });
        }

        [Fact]
        public void WithOptionalComponentAddsMandatoryComponent()
        {
            MessageSigningOptions options = new MessageSigningOptions()
                .WithOptionalComponent(SignatureComponent.Method);

            Assert.Collection(options.ComponentsToInclude,
                (comp) =>
                {
                    Assert.Same(SignatureComponent.Method, comp.Component);
                    Assert.False(comp.Mandatory);
                });
        }
    }
}
