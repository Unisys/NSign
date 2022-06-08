using StructuredFieldValues;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace NSign.Http
{
    public sealed class StructuredFieldValueTests
    {
        [Fact]
        public void ListValueWorks()
        {
            ParsedItem item = new ParsedItem(
                "abc",
                new Dictionary<string, object>()
                {
                    { "abc", 123 },
                    { "def", "ghi" },
                    { "jkl", true },
                });
            ParsedItem item2 = new ParsedItem(
                "abc2",
                new Dictionary<string, object>()
                {
                    { "abc", 234 },
                    { "def", "xyz" },
                    { "jkl", false },
                });
            StructuredFieldValue value = new StructuredFieldValue(ImmutableList<ParsedItem>.Empty.Add(item).Add(item2));

            Assert.Equal(StructuredFieldType.List, value.Type);
            Assert.NotNull(value.List);
            Assert.Null(value.Dictionary);
            Assert.Null(value.Item);

            Assert.Collection(value.List,
                (actual) => Assert.Equal(item, actual),
                (actual) => Assert.Equal(item2, actual));
            Assert.Equal(@"""abc"";abc=123;def=""ghi"";jkl, ""abc2"";abc=234;def=""xyz"";jkl=?0", value.Serialize());
        }

        [Fact]
        public void DictionaryValueWorks()
        {
            ParsedItem item = new ParsedItem(
                "abc",
                new Dictionary<string, object>()
                {
                    { "abc", 123 },
                    { "def", "ghi" },
                    { "jkl", true },
                });
            ParsedItem item2 = new ParsedItem(
                "abc2",
                new Dictionary<string, object>()
                {
                    { "abc", 234 },
                    { "def", "xyz" },
                    { "jkl", false },
                });
            StructuredFieldValue value = new StructuredFieldValue(new Dictionary<string, ParsedItem>()
            {
                { "first", item },
                { "second", item2 },
            });

            Assert.Equal(StructuredFieldType.Dictionary, value.Type);
            Assert.Null(value.List);
            Assert.NotNull(value.Dictionary);
            Assert.Null(value.Item);

            Assert.Collection(value.Dictionary,
                (actual) =>
                {
                    Assert.Equal("first", actual.Key);
                    Assert.Equal(item, actual.Value);
                },
                (actual) =>
                {
                    Assert.Equal("second", actual.Key);
                    Assert.Equal(item2, actual.Value);
                });
            Assert.Equal(@"first=""abc"";abc=123;def=""ghi"";jkl, second=""abc2"";abc=234;def=""xyz"";jkl=?0",
                value.Serialize());
        }

        [Fact]
        public void ItemValueWorks()
        {
            ParsedItem item = new ParsedItem(
                "abc",
                new Dictionary<string, object>()
                {
                    { "abc", 123 },
                    { "def", "ghi" },
                    { "jkl", true },
                });
            StructuredFieldValue value = new StructuredFieldValue(item);

            Assert.Equal(StructuredFieldType.Item, value.Type);
            Assert.Null(value.List);
            Assert.Null(value.Dictionary);
            Assert.NotNull(value.Item);

            Assert.Equal(item, value.Item);
            Assert.Equal(@"""abc"";abc=123;def=""ghi"";jkl", value.Serialize());
        }

        [Fact]
        public void DefaultCtorCreatesUnknownValue()
        {
            StructuredFieldValue value = new StructuredFieldValue();

            Assert.Equal(StructuredFieldType.Unknown, value.Type);
            Assert.Null(value.List);
            Assert.Null(value.Dictionary);
            Assert.Null(value.Item);

            try
            {
                value.Serialize();
            }
            catch (NotSupportedException ex)
            {
                Assert.Equal("Cannot serialize a field of type Unknown type.", ex.Message);
            }
        }
    }
}
