using System;
using System.Runtime.Serialization;

namespace NSign.Signatures
{
    /// <summary>
    /// Exception tracking the occurrence of an unexpected character in the input during parsing.
    /// </summary>
    public sealed class UnexpectedInputException : Exception
    {
        /// <summary>
        /// Initializes a new instance of UnexpectedInputException.
        /// </summary>
        /// <param name="unexpectedCharacter">
        /// The character that was not expected.
        /// </param>
        /// <param name="position">
        /// The zero-based position of the occurrence of the character.
        /// </param>
        public UnexpectedInputException(char unexpectedCharacter, int position) : base(GetMessage(unexpectedCharacter, position))
        {
            UnexpectedCharacter = unexpectedCharacter;
            Position = position;
        }

        /// <summary>
        /// Initializes a new instance of UnexpectedInputException.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized object data about the exception
        /// being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information about the source or
        /// destination.
        /// </param>
        public UnexpectedInputException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            UnexpectedCharacter = info.GetChar(nameof(UnexpectedCharacter));
            Position = info.GetInt32(nameof(Position));
        }

        /// <summary>
        /// Gets the character that was expected.
        /// </summary>
        public char UnexpectedCharacter { get; }

        /// <summary>
        /// Gets the zero-based position of the occurrence of the character.
        /// </summary>
        public int Position { get; }

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(UnexpectedCharacter), UnexpectedCharacter);
            info.AddValue(nameof(Position), Position);
        }

        /// <summary>
        /// Gets a friendly message for this exception.
        /// </summary>
        /// <param name="unexpectedCharacter">
        /// The character that was expected.
        /// </param>
        /// <param name="position">
        /// The zero-based position of the occurrence of the character.
        /// </param>
        /// <returns>
        /// A string representing the exception's message.
        /// </returns>
        private static string GetMessage(char unexpectedCharacter, int position)
        {
            return $"Unexpected character '{unexpectedCharacter}' found at position {position}.";
        }
    }
}
