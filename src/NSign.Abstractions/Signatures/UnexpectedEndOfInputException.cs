using System;
using System.Runtime.Serialization;

namespace NSign.Signatures
{
    /// <summary>
    /// Exception tracking the unexpected end of input during parsing.
    /// </summary>
    public sealed class UnexpectedEndOfInputException : Exception
    {
        /// <summary>
        /// Initializes a new instance of UnexpectedEndOfInputException.
        /// </summary>
        /// <param name="expectedCharacter">
        /// The character that was expected.
        /// </param>
        public UnexpectedEndOfInputException(char expectedCharacter) : base(GetMessage(expectedCharacter))
        {
            ExpectedCharacter = expectedCharacter;
        }

        /// <summary>
        /// Initializes a new instance of UnexpectedEndOfInputException.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized object data about the exception
        /// being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information about the source or
        /// destination.
        /// </param>
        public UnexpectedEndOfInputException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ExpectedCharacter = info.GetChar(nameof(ExpectedCharacter));
        }

        /// <summary>
        /// Gets the character that was expected.
        /// </summary>
        public char ExpectedCharacter { get; }

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(ExpectedCharacter), ExpectedCharacter);
        }

        /// <summary>
        /// Gets a friendly message for this exception.
        /// </summary>
        /// <param name="expectedCharacter">
        /// The character that was expected.
        /// </param>
        /// <returns>
        /// A string representing the exception's message.
        /// </returns>
        private static string GetMessage(char expectedCharacter)
        {
            return $"Expected character '{expectedCharacter}' but found end of input.";
        }
    }
}
