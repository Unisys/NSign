using Xunit;

namespace NSign.Http
{
    public sealed class PercentCodecTests
    {
        [Theory]
        [InlineData("\x23", "%23")]
        [InlineData("\x7f", "%7F")]
        [InlineData("≡", "%E2%89%A1")]
        [InlineData("‽", "%E2%80%BD")]
        [InlineData("Say what‽", "Say%20what%E2%80%BD")]
        [InlineData("abcABC0123*-._", "abcABC0123*-._")]
        [InlineData("abc+def", "abc%2Bdef")]
        public void EncodeWorks(string input, string expectedOutput)
        {
            Assert.Equal(expectedOutput, PercentCodec.Encode(input));
        }

        [Theory]
        [InlineData("\x23", "%23")]
        [InlineData("\x7f", "%7F")]
        [InlineData("≡", "%E2%89%A1")]
        [InlineData("‽", "%E2%80%BD")]
        [InlineData("Say what‽", "Say%20what%E2%80%BD")]
        [InlineData("abcABC0123*-._", "abcABC0123*-._")]
        [InlineData("abc+def", "abc%2Bdef")]
        public void EncodeWithDecodeFirstWorks(string input, string expectedOutput)
        {
            string encoded = PercentCodec.Encode(input, decodeFirst: true);
            Assert.Equal(expectedOutput, encoded);

            // Also check that encode first decodes percent-encoded values.
            Assert.Equal(expectedOutput, PercentCodec.Encode(encoded, decodeFirst: true));
        }
    }
}
