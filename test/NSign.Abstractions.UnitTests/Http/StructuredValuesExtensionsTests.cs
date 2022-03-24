using StructuredFieldValues;
using System;
using System.Collections.Generic;
using Xunit;

namespace NSign.Http
{
    public sealed class StructuredValuesExtensionsTests
    {
        [Fact]
        public void SerializeAsStringValidatesInput()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
                () => StructuredValuesExtensions.SerializeAsString((object?)null));
            Assert.Equal("value", ex.ParamName);
        }

        [Theory]
        [InlineData(false, "?0")]
        [InlineData(true, "?1")]
        public void SerializeAsStringWorksForBoolean(bool input, string expectedOutput)
        {
            Assert.Equal(expectedOutput, StructuredValuesExtensions.SerializeAsString(input));
            Assert.Equal(expectedOutput, StructuredValuesExtensions.SerializeAsString((object)input));
        }

        [Theory]
        [InlineData("", @"""""")]
        [InlineData("abc", @"""abc""")]
        [InlineData("my\\value", @"""my\\value""")]
        [InlineData("dquote (\")", @"""dquote (\"")""")]
        public void SerializeAsStringWorksForString(string input, string expectedOutput)
        {
            Assert.Equal(expectedOutput, StructuredValuesExtensions.SerializeAsString(input));
            Assert.Equal(expectedOutput, StructuredValuesExtensions.SerializeAsString((object)input));
        }

        [Theory]
        [InlineData(new byte[] { }, @"::")]
        [InlineData(new byte[] { 0x00, }, @":AA==:")]
        [InlineData(new byte[] { 0x0d, 0xe6, 0x9d, 0x05, 0xe7, 0x9f, }, @":DeadBeef:")]
        public void SerializeAsStringWorksForByteSequence(byte[] input, string expectedOutput)
        {
            Assert.Equal(expectedOutput, StructuredValuesExtensions.SerializeAsString(input));

            ReadOnlyMemory<byte> memory = input;
            Assert.Equal(expectedOutput, StructuredValuesExtensions.SerializeAsString(memory));
        }

        [Fact]
        public void SerializeAsStringWorksForParsedItemList()
        {
            ParsedItem item = new ParsedItem(
                new ParsedItem[]
                {
                    new ParsedItem("elem1", new Dictionary<string, object>() { { "a", "x" }, }),
                    new ParsedItem("elem2", new Dictionary<string, object>()),
                },
                new Dictionary<string, object>());

            Assert.Equal("(\"elem1\";a=\"x\" \"elem2\")", item.SerializeAsString());
        }

        [Fact]
        public void SerializeAsParametersReturnsEmptyStringForNullOrEmptyParameters()
        {
            string actual;

            actual = StructuredValuesExtensions.SerializeAsParameters(null);
            Assert.Equal("", actual);

            actual = StructuredValuesExtensions.SerializeAsParameters(new KeyValuePair<string, object>[] { });
            Assert.Equal("", actual);
        }

        [Fact]
        public void SerializeAsParametersWorks()
        {
            string actual = StructuredValuesExtensions.SerializeAsParameters(
                new KeyValuePair<string, object>[]
                {
                    new KeyValuePair<string, object>("string", "def\\\""),
                    new KeyValuePair<string, object>("bool1", true),
                    new KeyValuePair<string, object>("bool2", false),
                    new KeyValuePair<string, object>("int", -1234),
                    new KeyValuePair<string, object>("bytes1", new byte[] { 0x09, 0xa7, 0xde, }),
                    new KeyValuePair<string, object>("bytes2", new ReadOnlyMemory<byte>(new byte[] { 0x45, 0xe6, 0xa5, })),
                });
            Assert.Equal(";string=\"def\\\\\\\"\";bool1;bool2=?0;int=-1234;bytes1=:Cafe:;bytes2=:Real:", actual);
        }

        [Theory]
        [InlineData((byte)1, "1")]
        [InlineData((sbyte)-1, "-1")]
        [InlineData((ushort)2, "2")]
        [InlineData((short)-2, "-2")]
        [InlineData((uint)3, "3")]
        [InlineData((int)-3, "-3")]
        [InlineData((ulong)4, "4")]
        [InlineData((long)-4, "-4")]
        [InlineData(5.1f, "5.1")]
        [InlineData(-5.1f, "-5.1")]
        [InlineData(6.1d, "6.1")]
        [InlineData(-6.1d, "-6.1")]
        public void SerializeAsStringWorksForNumbers(object input, string expectedOutput)
        {
            Assert.Equal(expectedOutput, StructuredValuesExtensions.SerializeAsString(input));
        }
    }
}
