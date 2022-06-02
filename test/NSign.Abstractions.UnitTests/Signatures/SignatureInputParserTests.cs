using System;
using Xunit;

namespace NSign.Signatures
{
    public sealed class SignatureInputParserTests
    {
        [Fact]
        public void ParseAndUpdateValidatesInput()
        {
            ArgumentNullException ex;

            ex = Assert.Throws<ArgumentNullException>(() => SignatureInputParser.ParseAndUpdate(null, null));
            Assert.Equal("input", ex.ParamName);

            ex = Assert.Throws<ArgumentNullException>(() => SignatureInputParser.ParseAndUpdate("", null));
            Assert.Equal("input", ex.ParamName);

            ex = Assert.Throws<ArgumentNullException>(() => SignatureInputParser.ParseAndUpdate(" ", null));
            Assert.Equal("input", ex.ParamName);

            ex = Assert.Throws<ArgumentNullException>(() => SignatureInputParser.ParseAndUpdate("blah", null));
            Assert.Equal("signatureParams", ex.ParamName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ParsingSucceedsForValidInput(bool bindRequest)
        {
            string suffix = bindRequest ? ";req" : String.Empty;

            string input =
                $@"(""@method""{suffix} ""@target-uri""{suffix} ""@authority""{suffix} ""@scheme""{suffix} " +
                $@"""@request-target""{suffix} ""@path""{suffix} ""@query""{suffix} ""@query-param""{suffix};name=""some-param"" " +
                $@"""@status"" ""my-header""{suffix} ""my-dict-header""{suffix};key=""blah"" ""@extension""{suffix})" +
                @";expires=-1534;created=1234;keyid=""key-id"";nonce=""the-nonce"";alg=""signature-alg""";
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            SignatureInputParser.ParseAndUpdate(input, signatureParams);

            Assert.Collection(signatureParams.Components,
                (c) => Assert.Equal(bindRequest ? SignatureComponent.RequestBoundMethod : SignatureComponent.Method, c),
                (c) => Assert.Equal(bindRequest ? SignatureComponent.RequestBoundRequestTargetUri : SignatureComponent.RequestTargetUri, c),
                (c) => Assert.Equal(bindRequest ? SignatureComponent.RequestBoundAuthority : SignatureComponent.Authority, c),
                (c) => Assert.Equal(bindRequest ? SignatureComponent.RequestBoundScheme : SignatureComponent.Scheme, c),
                (c) => Assert.Equal(bindRequest ? SignatureComponent.RequestBoundRequestTarget : SignatureComponent.RequestTarget, c),
                (c) => Assert.Equal(bindRequest ? SignatureComponent.RequestBoundPath : SignatureComponent.Path, c),
                (c) => Assert.Equal(bindRequest ? SignatureComponent.RequestBoundQuery : SignatureComponent.Query, c),
                (c) => Assert.Equal(new QueryParamComponent("Some-Param", bindRequest), c),
                (c) => Assert.Equal(SignatureComponent.Status, c),
                (c) => Assert.Equal(new HttpHeaderComponent("My-Header", bindRequest), c),
                (c) => Assert.Equal(new HttpHeaderDictionaryStructuredComponent("My-Dict-Header", "blah", bindRequest), c),
                (c) => Assert.Equal(new DerivedComponent("@extension", bindRequest), c));

            Assert.True(signatureParams.Created.HasValue);
            Assert.Equal(1234L, signatureParams.Created!.Value.ToUnixTimeSeconds());
            Assert.True(signatureParams.Expires.HasValue);
            Assert.Equal(-1534L, signatureParams.Expires!.Value.ToUnixTimeSeconds());
            Assert.Equal("the-nonce", signatureParams.Nonce);
            Assert.Equal("signature-alg", signatureParams.Algorithm);
            Assert.Equal("key-id", signatureParams.KeyId);
        }

        [Theory]
        [InlineData("blah",
            "Expected token of type OpenParenthesis, but found token 'blah' of type Identifier at position 0.")]
        [InlineData("\"blah\"",
            "Expected token of type OpenParenthesis, but found token 'blah' of type QuotedString at position 0.")]
        [InlineData("1234",
            "Expected token of type OpenParenthesis, but found token '1234' of type Integer at position 0.")]
        [InlineData("(987",
            "Expected token type to be one of {QuotedString, CloseParenthesis}, but found token '987' of type Integer at position 1.")]
        [InlineData(@"(""a"" b)",
            "Expected token of type QuotedString, but found token 'b' of type Identifier at position 5.")]
        [InlineData(@"(""a""""b"")",
            "Expected token type to be one of {Semicolon, CloseParenthesis, Whitespace}, but found token 'b' of type QuotedString at position 4.")]
        [InlineData("()created=1234",
            "Expected token of type Semicolon, but found token 'created' of type Identifier at position 2.")]
        [InlineData(@"();created=""1234""",
            "Expected token of type Integer, but found token '1234' of type QuotedString at position 11.")]
        [InlineData(@"();expires=""1234""",
            "Expected token of type Integer, but found token '1234' of type QuotedString at position 11.")]
        [InlineData(@"();nonce=1234",
            "Expected token of type QuotedString, but found token '1234' of type Integer at position 9.")]
        [InlineData(@"();alg=1234",
            "Expected token of type QuotedString, but found token '1234' of type Integer at position 7.")]
        [InlineData(@"();keyid=1234",
            "Expected token of type QuotedString, but found token '1234' of type Integer at position 9.")]
        [InlineData(@"(""header"";555",
            "Expected token of type Identifier, but found token '555' of type Integer at position 10.")]
        [InlineData(@"(""header"";foo;",
            "Expected token of type Identifier, but found token '' of type EndOfInput at position 14.")]
        [InlineData(@"(""header"";foo=bar",
            "Expected token of type QuotedString, but found token 'bar' of type Identifier at position 14.")]
        [InlineData(@"(""header"";foo=""bar""",
            "Expected token type to be one of {Semicolon, CloseParenthesis, Whitespace}, but found token '' of type EndOfInput at position 19.")]
        [InlineData(@"(""header"";foo=""bar"";",
            "Expected token of type Identifier, but found token '' of type EndOfInput at position 20.")]
        [InlineData(@"(""header"";foo=123",
            "Expected token of type QuotedString, but found token '123' of type Integer at position 14.")]
        [InlineData(@"();333",
            "Expected token of type Identifier, but found token '333' of type Integer at position 3.")]
        [InlineData(@"();blah",
            "Expected token of type Equal, but found token '' of type EndOfInput at position 7.")]
        [InlineData(@"();blah;",
            "Expected token of type Equal, but found token '' of type Semicolon at position 7.")]
        [InlineData(@"();blah=;",
            "Expected token type to be one of {QuotedString, Integer}, but found token '' of type Semicolon at position 8.")]
        public void ParsingThrowsSignatureInputParserException(string input, string expectedMessage)
        {
            SignatureInputParserException ex;
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            ex = Assert.Throws<SignatureInputParserException>(
                () => SignatureInputParser.ParseAndUpdate(input, signatureParams));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData(".", "Unexpected character '.' found at position 0.")]
        [InlineData("(&", "Unexpected character '&' found at position 1.")]
        [InlineData("(#", "Unexpected character '#' found at position 1.")]
        public void ParsingThrowsUnexpectedInputException(string input, string expectedMessage)
        {
            UnexpectedInputException ex;
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            ex = Assert.Throws<UnexpectedInputException>(() => SignatureInputParser.ParseAndUpdate(input, signatureParams));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData(@"(""header-name", "Expected character '\"' but found end of input.")]
        [InlineData(@"(""header-name"";key=""blah", "Expected character '\"' but found end of input.")]
        [InlineData(@"();alg=""algorithm", "Expected character '\"' but found end of input.")]
        public void ParsingThrowsUnexpectedEndOfInputException(string input, string expectedMessage)
        {
            UnexpectedEndOfInputException ex;
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            ex = Assert.Throws<UnexpectedEndOfInputException>(
                () => SignatureInputParser.ParseAndUpdate(input, signatureParams));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData(@"("""")",
            "Component names must not be empty.")]
        [InlineData(@"(""blah"" """")",
            "Component names must not be empty.")]
        [InlineData(@"(""@query-param"")",
            "The @query-param component requires the 'name' parameter.")]
        [InlineData(@"(""@query-param""  ""header"")",
            "The @query-param component requires the 'name' parameter.")]
        [InlineData(@"(""@query-param"";key=""foo"" ""header"")",
            @"The component '""@query-param"";key=""foo""' has unsupported parameter 'key'.")]
        [InlineData(@"(""blah"" ""@signature-params"")",
            "The @signature-params component is not allowed.")]
        [InlineData(@"(""dict-header"";name=""blah"")",
            @"The component '""dict-header"";name=""blah""' has unsupported parameter 'name'.")]
        [InlineData(@"();Foo=1234",
            "Unsupported signature input parameter: Foo with value '1234'.")]
        public void ParsingThrowsSignatureInputException(string input, string expectedMessage)
        {
            SignatureInputException ex;
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            ex = Assert.Throws<SignatureInputException>(() => SignatureInputParser.ParseAndUpdate(input, signatureParams));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData(@"(""header"" ""@query-param"";blah=""foo"")",
            @"The component '""@query-param"";blah=""foo""' has unsupported parameter 'blah'.")]
        [InlineData(@"(""header"";blotz=""blimp"" ""@status"")",
            @"The component '""header"";blotz=""blimp""' has unsupported parameter 'blotz'.")]
        [InlineData(@"(""header"";req ""@query-param"";req;blah=""foo"")",
            @"The component '""@query-param"";req;blah=""foo""' has unsupported parameter 'blah'.")]
        [InlineData(@"(""header"";req;blotz=""blimp"" ""@status"")",
            @"The component '""header"";req;blotz=""blimp""' has unsupported parameter 'blotz'.")]
        [InlineData(@"(""header"" ""@status"";req)",
            @"The component '""@status"";req' has unsupported parameter 'req'.")]
        public void ParsingThrowsSignatureInputExceptionForUnsupportedParameters(string input, string expectedMessage)
        {
            SignatureParamsComponent signatureParams = new SignatureParamsComponent();

            SignatureInputException ex = Assert.Throws<SignatureInputException>(
                () => SignatureInputParser.ParseAndUpdate(input, signatureParams));
            Assert.Equal(expectedMessage, ex.Message);
        }
    }
}
