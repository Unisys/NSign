using System.Diagnostics;
using static NSign.Signatures.SignatureInputParser;

namespace NSign.Signatures
{
    /// <summary>
    /// Extensions for signature input parsing.
    /// </summary>
    internal static class SignatureInputParserExtensions
    {
        /// <summary>
        /// Checks if the given tokenType is one of the token types from the mask.
        /// </summary>
        /// <param name="tokenType">
        /// The TokenType to test.
        /// </param>
        /// <param name="expectedTypes">
        /// The TokenType mask to test against.
        /// </param>
        /// <returns>
        /// True if one token type from the mask matches, or false otherwise.
        /// </returns>
        public static bool IsOneOf(this TokenType tokenType, TokenType expectedTypes)
        {
#if DEBUG
            uint actualType = (uint)tokenType;
            Debug.Assert(0 == (actualType & (actualType - 1)), "The token type must be a single token type flag.");
#endif
            return expectedTypes.HasFlag(tokenType);
        }

        /// <summary>
        /// Checks if the given token has a TokenType matching one of the token types from the mask.
        /// </summary>
        /// <param name="token">
        /// The Token to test.
        /// </param>
        /// <param name="expectedTypes">
        /// The TokenType mask to test against.
        /// </param>
        /// <returns>
        /// True if one token type from the mask matches, or false otherwise.
        /// </returns>
        public static bool IsTypeOneOf(this Token token, TokenType expectedTypes)
        {
            return token.Type.IsOneOf(expectedTypes);
        }

        /// <summary>
        /// Ensures that the given Tokenizer's current token is one of the expected / allowed types or throws if not.
        /// </summary>
        /// <param name="tokenizer">
        /// The Tokenizer to check.
        /// </param>
        /// <param name="expectedTypes">
        /// The TokenType mask to test against.
        /// </param>
        /// <exception cref="SignatureInputParserException">
        /// A SignatureInputParserException is thrown if the tokenizer's current token has an unexpected token type.
        /// </exception>
        public static void EnsureTokenOneOfOrThrow(this Tokenizer tokenizer, TokenType expectedTypes)
        {
            if (!tokenizer.Token.Type.IsOneOf(expectedTypes))
            {
                throw new SignatureInputParserException(expectedTypes, tokenizer);
            }
        }
    }
}
