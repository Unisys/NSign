using System;
using Xunit;

namespace NSign.Signatures
{
    public sealed class SignatureInputSpecTests
    {
        private readonly SignatureInputSpec inputSpec = new SignatureInputSpec("my-sig");

        [Fact]
        public void CtorInitializesProperly()
        {
            Assert.Equal("my-sig", inputSpec.Name);
            Assert.NotNull(inputSpec.SignatureParameters);
        }

        [Fact]
        public void CtorWithValueParses()
        {
            DateTimeOffset created = new DateTimeOffset(2021, 9, 13, 13, 57, 23, TimeSpan.Zero);
            DateTimeOffset expires = new DateTimeOffset(2021, 9, 13, 14, 2, 23, TimeSpan.Zero);
            SignatureInputSpec inputSpec = new SignatureInputSpec("my-sig2", "();created=1631541443;expires=1631541743");

            Assert.Equal("my-sig2", inputSpec.Name);
            Assert.NotNull(inputSpec.SignatureParameters);

            Assert.True(inputSpec.SignatureParameters.Created.HasValue);
            Assert.Equal(created, inputSpec.SignatureParameters.Created!.Value);

            Assert.True(inputSpec.SignatureParameters.Expires.HasValue);
            Assert.Equal(expires, inputSpec.SignatureParameters.Expires!.Value);
        }
    }
}
