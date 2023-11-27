using System;
using System.Runtime.Serialization;

using static NSign.Signatures.SignatureInputParser;

namespace NSign.Signatures
{
    /// <summary>
    /// Exception tracking the discovery of an unexpected token in the input.
    /// </summary>
    public sealed class SignatureInputParserException : Exception
    {
        /// <summary>
        /// Initializes a new instance of SignatureInputParserException.
        /// </summary>
        /// <param name="expected">
        /// A TokenType value representing all the types of tokens that were expected / allowed at the given position.
        /// </param>
        /// <param name="tokenizer">
        /// The Tokenizer that tracks the found token as well as its position.
        /// </param>
        internal SignatureInputParserException(TokenType expected, Tokenizer tokenizer) : base(GetMessage(expected, tokenizer))
        {
            Expected = expected;
            FoundType = tokenizer.Token.Type;
#if NETSTANDARD2_0
            FoundValue = new String(tokenizer.Token.Value.ToArray());
#elif NETSTANDARD2_1_OR_GREATER || NET
            FoundValue = new String(tokenizer.Token.Value);
#endif
            Position = tokenizer.LastPosition;
        }

#if NET8_0_OR_GREATER
#else
        /// <summary>
        /// Initializes a new instance of SignatureInputParserException.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized object data about the exception
        /// being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information about the source or
        /// destination.
        /// </param>
        public SignatureInputParserException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Expected = (TokenType)info.GetValue(nameof(Expected), typeof(TokenType))!;
            FoundType = (TokenType)info.GetValue(nameof(FoundType), typeof(TokenType))!;
            FoundValue = info.GetString(nameof(FoundValue))!;
            Position = info.GetInt32(nameof(Position));
        }
#endif

        /// <summary>
        /// Gets a TokenType value representing all the types of tokens that were expected / allowed at the given position.
        /// </summary>
        internal TokenType Expected { get; }

        /// <summary>
        /// Gets a TokenType value representing the type of token found at the given position.
        /// </summary>
        internal TokenType FoundType { get; }

        /// <summary>
        /// Gets a string representing the value of the token (if any) that was found at the given position.
        /// </summary>
        internal string FoundValue { get; }

        /// <summary>
        /// Gets the position of the unexpected token in the input.
        /// </summary>
        public int Position { get; }

#if NET8_0_OR_GREATER
#else
        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(Expected), Expected);
            info.AddValue(nameof(FoundType), FoundType);
#if NETSTANDARD2_0
            info.AddValue(nameof(FoundValue), new String(FoundValue.ToCharArray()));
#elif NETSTANDARD2_1_OR_GREATER || NET
            info.AddValue(nameof(FoundValue), new String(FoundValue));
#endif
            info.AddValue(nameof(Position), Position);
        }
#endif

        /// <summary>
        /// Gets a friendly message for this exception.
        /// </summary>
        /// <param name="expected">
        /// A TokenType value representing all the types of tokens that were expected / allowed at the given position.
        /// </param>
        /// <param name="tokenizer">
        /// The Tokenizer that tracks the found token as well as its position.
        /// </param>
        /// <returns>
        /// A string representing the exception's message.
        /// </returns>
        private static string GetMessage(TokenType expected, Tokenizer tokenizer)
        {
            uint rawExpected = (uint)expected;

            if ((rawExpected & (rawExpected - 1)) == 0)
            {
                // A single token type is expected / allowed.
#if NETSTANDARD2_0
                return
                    $"Expected token of type {expected}, but found token '{new String(tokenizer.Token.Value.ToArray())}' of type " +
                    $"{tokenizer.Token.Type} at position {tokenizer.LastPosition}.";
#elif NETSTANDARD2_1_OR_GREATER || NET
                return
                    $"Expected token of type {expected}, but found token '{new String(tokenizer.Token.Value)}' of type " +
                    $"{tokenizer.Token.Type} at position {tokenizer.LastPosition}.";
#endif
            }
            else
            {
                // Multiple token types were expected / allowed.
#if NETSTANDARD2_0
                return
                    $"Expected token type to be one of {{{expected}}}, but found token '{new String(tokenizer.Token.Value.ToArray())}' " +
                    $"of type {tokenizer.Token.Type} at position {tokenizer.LastPosition}.";
#elif NETSTANDARD2_1_OR_GREATER || NET
                return
                    $"Expected token type to be one of {{{expected}}}, but found token '{new String(tokenizer.Token.Value)}' " +
                    $"of type {tokenizer.Token.Type} at position {tokenizer.LastPosition}.";
#endif
            }
        }
    }
}
