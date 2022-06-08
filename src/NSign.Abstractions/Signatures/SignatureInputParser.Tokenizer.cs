using System;

namespace NSign.Signatures
{
    ref partial struct SignatureInputParser
    {
        /// <summary>
        /// Tokenizer for signature input strings.
        /// </summary>
        internal ref struct Tokenizer
        {
            /// <summary>
            /// The position of the next token.
            /// </summary>
            private int position;

            /// <summary>
            /// Initializes a new instance of Tokenizer.
            /// </summary>
            /// <param name="input">
            /// The input to parse.
            /// </param>
            public Tokenizer(ReadOnlySpan<char> input)
            {
                Input = input;
                position = 0;
                LastPosition = -1;
                Token = Token.Empty;
            }

            /// <summary>
            /// The input to parse.
            /// </summary>
            public ReadOnlySpan<char> Input { get; }

            /// <summary>
            /// Gets or sets the last read token. Before Next() is called for the first time, this is always the empty token.
            /// </summary>
            public Token Token { get; private set; }

            /// <summary>
            /// Gets or sets the start position of the last token, or -1 if no token has been read yet.
            /// </summary>
            public int LastPosition { get; private set; }

            /// <summary>
            /// Moves to the next token, if possible.
            /// </summary>
            /// <returns>
            /// True if a token was read, or false if the end of input was reached.
            /// </returns>
            public bool Next()
            {
                LastPosition = position;
                int chr = Read();

                switch (chr)
                {
                    case -1:
                        Token = Token.EndOfInput;
                        return false;

                    case '\t':
                    case '\r':
                    case '\n':
                    case ' ':
                        ConsumeWhitespace();
                        return true;

                    case '(':
                        Token = Token.OpenParenthesis;
                        return true;

                    case ')':
                        Token = Token.CloseParenthesis;
                        return true;

                    case ';':
                        Token = Token.Semicolon;
                        return true;

                    case '=':
                        Token = Token.Equal;
                        return true;

                    case '\"':
                        ConsumeQuotedString();
                        return true;

                    default:
                        if (IsIdentifierChar(chr, isFirst: true))
                        {
                            ConsumeIdentifier();
                            return true;
                        }
                        else if (IsIntegerChar(chr, isFirst: true))
                        {
                            ConsumeInteger();
                            return true;
                        }
                        else
                        {
                            throw new UnexpectedInputException((char)chr, LastPosition);
                        }
                }
            }

            /// <summary>
            /// Reads the next character from the input.
            /// </summary>
            /// <returns>
            /// An int value that represents the read character or -1 to indicate the end of input.
            /// </returns>
            int Read()
            {
                int result = Peek();
                ++position;

                return result;
            }

            /// <summary>
            /// Peeks at the next character from the input without actually consuming it.
            /// </summary>
            /// <returns>
            /// An int value that represents the read character or -1 to indicate the end of input.
            /// </returns>
            int Peek()
            {
                if (Input.Length <= position)
                {
                    return -1;
                }

                return Input[position];
            }

            /// <summary>
            /// Consumes a sequence of continuous whitespace in the input and updates the current Token with a whitespace token.
            /// </summary>
            private void ConsumeWhitespace()
            {
                int chr = Peek();
                while (IsWhitespace(chr))
                {
                    // Consume the current whitespace character and peek into the next character to see if it's a whitespace too.
                    Read();
                    chr = Peek();
                }

                Token = Token.Whitespace;
            }

            /// <summary>
            /// Consumes a quoted string and updates the current Token with a quoted string token.
            /// </summary>
            /// <remarks>
            /// The opening double quote must have already been read, thus the position is set to the first character of the
            /// quoted string.
            /// </remarks>
            private void ConsumeQuotedString()
            {
                int chr;
                // The current reader position is at the first char of the string after the opening double quote.
                int startPos = position;

                while ('\"' != (chr = Read()))
                {
                    if (-1 == chr)
                    {
                        throw new UnexpectedEndOfInputException('\"');
                    }
                }

                // We have already read the closing double quote, so the actual quoted string ends one character before.
                Token = new Token(TokenType.QuotedString, Input[startPos..(position - 1)]);
            }

            /// <summary>
            /// Consumes an identifier and updates the current Token with an identifier token.
            /// </summary>
            /// <remarks>
            /// The first character of the identifier was already read, so the identifier's start position is one character
            /// before the current position.
            /// </remarks>
            private void ConsumeIdentifier()
            {
                int startPos = position - 1;

                // Consume identifier characters starting at the second character until we find a non-identifier character.
                while (IsIdentifierChar(Peek(), isFirst: false))
                {
                    Read();
                }

                Token = new Token(TokenType.Identifier, Input[startPos..position]);
            }

            /// <summary>
            /// Consumes an integer and updates the current Token with an integer token.
            /// </summary>
            /// <remarks>
            /// The first digit of the integer was already read, so the integers's start position is one character before the
            /// current position.
            /// </remarks>
            private void ConsumeInteger()
            {
                int startPos = position - 1;

                while (IsIntegerChar(Peek()))
                {
                    Read();
                }

                Token = new Token(TokenType.Integer, Input[startPos..position]);
            }

            /// <summary>
            /// Checks if the given character is valid for an identifier.
            /// </summary>
            /// <param name="chr">
            /// The character to check.
            /// </param>
            /// <param name="isFirst">
            /// A flag which indicates whether or not this is the first char for a new identifier.
            /// </param>
            /// <returns>
            /// True if the character is a valid identifier character.
            /// </returns>
            /// <remarks>
            /// Valid names are per section 3.1.2 of RFC 8941.
            /// See also <see href="https://httpwg.org/specs/rfc8941.html#rfc.section.3.1.2"/>.
            /// </remarks>
            private static bool IsIdentifierChar(int chr, bool isFirst)
            {
                if (isFirst)
                {
                    return (chr >= 'a' && chr <= 'z') || chr == '*';
                }

                return (chr >= 'a' && chr <= 'z') || (chr >= '0' && chr <= '9') ||
                    chr == '_' || chr == '-' || chr == '.' || chr == '*';
            }

            /// <summary>
            /// Checks if the given character is valid for an integer.
            /// </summary>
            /// <param name="chr">
            /// The character to check.
            /// </param>
            /// <param name="isFirst">
            /// A flag which indicates whether or not this is the first integer character to be tested in sequence.
            /// </param>
            /// <returns>
            /// True if the character is a valid integer character.
            /// </returns>
            private static bool IsIntegerChar(int chr, bool isFirst = false)
            {
                if (isFirst)
                {
                    return chr == '-' || (chr >= '0' && chr <= '9');
                }
                else
                {
                    return chr >= '0' && chr <= '9';
                }
            }

            /// <summary>
            /// Checks if the given character is a whitespace character.
            /// </summary>
            /// <param name="chr">
            /// The character to check.
            /// </param>
            /// <returns>
            /// True if the character is a whitespace character, or false otherwise.
            /// </returns>
            private static bool IsWhitespace(int chr)
            {
                return chr == ' ';
            }
        }
    }
}
