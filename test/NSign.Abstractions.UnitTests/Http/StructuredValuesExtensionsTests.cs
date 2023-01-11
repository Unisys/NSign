using StructuredFieldValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public void SerializeAsStringWorksForByteSequenceAsReadOnlyMemory(byte[] input, string expectedOutput)
        {
            Assert.Equal(expectedOutput, StructuredValuesExtensions.SerializeAsString((object)input));

            ReadOnlyMemory<byte> memory = input;
            Assert.Equal(expectedOutput, StructuredValuesExtensions.SerializeAsString(memory));
        }

        [Theory]
        [InlineData(new byte[] { }, @"::")]
        [InlineData(new byte[] { 0x00, }, @":AA==:")]
        [InlineData(new byte[] { 0x0d, 0xe6, 0x9d, 0x05, 0xe7, 0x9f, }, @":DeadBeef:")]
        public void SerializeAsStringWorksForByteSequenceAsReadOnlySpan(byte[] input, string expectedOutput)
        {
            Assert.Equal(expectedOutput, StructuredValuesExtensions.SerializeAsString((object)input));

            ReadOnlySpan<byte> span = input;
            Assert.Equal(expectedOutput, StructuredValuesExtensions.SerializeAsString(span));
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
            Assert.Equal("(\"elem1\";a=\"x\" \"elem2\")", ((object)item).SerializeAsString());
        }

        [Fact]
        public void SerializeAsParametersReturnsEmptyStringForNullOrEmptyParameters()
        {
            string actual;

            actual = StructuredValuesExtensions.SerializeAsParameters(null);
            Assert.Equal("", actual);

            actual = StructuredValuesExtensions.SerializeAsParameters(Array.Empty<KeyValuePair<string, object>>());
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

        [Theory]
        [InlineData(@"""abc")]
        public void TryParseStructuredFieldValueFailsForInvalidInput(string input)
        {
            Assert.False(new string[] { input }.TryParseStructuredFieldValue(out StructuredFieldValue value));
            Assert.Equal(StructuredFieldType.Unknown, value.Type);

            try
            {
                value.Serialize();
            }
            catch (NotSupportedException ex)
            {
                Assert.Equal("Cannot serialize a field of type Unknown type.", ex.Message);
            }
        }

        [Theory]
        [InlineData(@"2; foourl=""https://foo.example.com/""", StructuredFieldType.List,
            @"2;foourl=""https://foo.example.com/""")]
        [InlineData(@"sugar, tea,  rum", StructuredFieldType.List, @"sugar, tea, rum")]
        [InlineData(@"(""foo""  ""bar""),  (""baz""), ( ""bat"" ""one""), ( )", StructuredFieldType.List,
            @"(""foo"" ""bar""), (""baz""), (""bat"" ""one""), ()")]
        [InlineData(@"(""foo""; a=1; b=2); lvl=5, (""bar"" ""baz"");lvl=1", StructuredFieldType.List
            , @"(""foo"";a=1;b=2);lvl=5, (""bar"" ""baz"");lvl=1")]
        [InlineData(@"abc;a=1;b=2; cde_456, (ghi;jk=4  l);q=""9"";r=w", StructuredFieldType.List,
            @"abc;a=1;b=2;cde_456, (ghi;jk=4 l);q=""9"";r=w")]
        [InlineData(@"1;  a;  b=?0", StructuredFieldType.List, @"1;a;b=?0")]
        [InlineData(@"en=""Applepie"" ,  da=:w4ZibGV0w6ZydGU=:", StructuredFieldType.Dictionary,
            @"en=""Applepie"", da=:w4ZibGV0w6ZydGU=:")]
        [InlineData(@"a=?0 ,  b, c; foo=bar ", StructuredFieldType.Dictionary, @"a=?0, b, c;foo=bar")]
        [InlineData(@"rating=1.5 , feelings=(joy  sadness)", StructuredFieldType.Dictionary,
            @"rating=1.5, feelings=(joy sadness)")]
        [InlineData(@"a=(1  2), b=3,  c=4; aa=bb, d=(5  6); valid", StructuredFieldType.Dictionary,
            @"a=(1 2), b=3, c=4;aa=bb, d=(5 6);valid")]
        [InlineData(@"5; foo=bar", StructuredFieldType.List, @"5;foo=bar")]
        public void TryParseStructuredFieldValueWithSubsequentSerializationNormalizes(
            string input,
            StructuredFieldType expectedType,
            string expectedOutout)
        {
            Assert.True(new string[] { input, }.TryParseStructuredFieldValue(out StructuredFieldValue value));
            Assert.Equal(expectedType, value.Type);
            Assert.Equal(expectedOutout, value.Serialize());
        }
    }
}
