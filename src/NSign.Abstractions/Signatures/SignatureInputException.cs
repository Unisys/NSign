using System;
using System.Runtime.Serialization;

namespace NSign.Signatures
{
    /// <summary>
    /// Represents an exception in signature input.
    /// </summary>
    public sealed class SignatureInputException : Exception
    {
        /// <summary>
        /// Initializes a new SignatureInputException.
        /// </summary>
        /// <param name="message">
        /// The exception message.
        /// </param>
        public SignatureInputException(string message) : base(message)
        { }

        /// <summary>
        /// Initializes a new instance of SignatureInputException.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized object data about the exception
        /// being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information about the source or
        /// destination.
        /// </param>
        public SignatureInputException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }
}
