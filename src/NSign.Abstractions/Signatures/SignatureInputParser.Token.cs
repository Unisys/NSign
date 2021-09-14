using System;
using System.Diagnostics;

namespace NSign.Signatures
{
    ref partial struct SignatureInputParser
    {
        /// <summary>
        /// Token types produced by the Tokenizer.
        /// </summary>
        [Flags]
        internal enum TokenType : uint
        {
            /// <summary>
            /// Unknown token. This should only be used before the Tokenizer has read the first token.
            /// </summary>
            Unknown = 0x0000,

            /// <summary>
            /// Identifier token.
            /// </summary>
            Identifier = 0x0001,

            /// <summary>
            /// Quoted string token.
            /// </summary>
            QuotedString = 0x0002,

            /// <summary>
            /// Integer token.
            /// </summary>
            Integer = 0x0004,

            /// <summary>
            /// The equal sign '='.
            /// </summary>
            Equal = 0x0010,

            /// <summary>
            /// The semicolon ';'.
            /// </summary>
            Semicolon = 0x0020,

            /// <summary>
            /// The open parenthesis '('.
            /// </summary>
            OpenParenthesis = 0x0040,

            /// <summary>
            /// The closing parenthesis ')'.
            /// </summary>
            CloseParenthesis = 0x0080,

            /// <summary>
            /// Whitespace token to represent any number of consecutive whitespace.
            /// </summary>
            Whitespace = 0x0100,

            /// <summary>
            /// The End-of-Input token.
            /// </summary>
            EndOfInput = 0x1000,
        }

        /// <summary>
        /// Defines a token produced by the Tokenizer.
        /// </summary>
        [DebuggerDisplay("Type={Type}, Value={Value}")]
        internal readonly ref struct Token
        {
            /// <summary>
            /// Gets the Empty token.
            /// </summary>
            public static Token Empty => new Token(TokenType.Unknown, null);

            /// <summary>
            /// Gets an Equal token.
            /// </summary>
            public static Token Equal => new Token(TokenType.Equal, null);

            /// <summary>
            /// Gets a Semicolong token.
            /// </summary>
            public static Token Semicolon => new Token(TokenType.Semicolon, null);

            /// <summary>
            /// Gets an OpenParenthesis token.
            /// </summary>
            public static Token OpenParenthesis => new Token(TokenType.OpenParenthesis, null);

            /// <summary>
            /// Gets a CloseParenthesis token.
            /// </summary>
            public static Token CloseParenthesis => new Token(TokenType.CloseParenthesis, null);

            /// <summary>
            /// Gets a Whitespace token.
            /// </summary>
            public static Token Whitespace => new Token(TokenType.Whitespace, null);

            /// <summary>
            /// Gets an EndOfInput token.
            /// </summary>
            public static Token EndOfInput => new Token(TokenType.EndOfInput, null);

            /// <summary>
            /// Initializes a new instance of Token.
            /// </summary>
            /// <param name="type">
            /// The TokenType for the new token.
            /// </param>
            /// <param name="value">
            /// A ReadOnlySpan of char that represents the token's value, if any.
            /// </param>
            public Token(TokenType type, ReadOnlySpan<char> value)
            {
                Type = type;
                Value = value;
            }

            /// <summary>
            /// Gets the TokenType for the new token.
            /// </summary>
            public TokenType Type { get; }

            /// <summary>
            /// Gets a ReadOnlySpan of char that represents the token's value, if any.
            /// </summary>
            public ReadOnlySpan<char> Value { get; }
        }
    }
}
