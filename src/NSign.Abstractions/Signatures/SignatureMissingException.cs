using System;
using System.Runtime.Serialization;

namespace NSign.Signatures
{
    /// <summary>
    /// Represents an exception that is thrown when an HTTP message is expected to have signatures but does not have any.
    /// </summary>
    public sealed class SignatureMissingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of SignatureMissingException.
        /// </summary>
        public SignatureMissingException() :
            base("The message does not contain any signatures for verification. " +
                 "Consider removing signature verification or make sure messages are signed.")
        { }

#if NET8_0_OR_GREATER
#else
        /// <summary>
        /// Initializes a new instance of SignatureMissingException.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized object data about the exception
        /// being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information about the source or
        /// destination.
        /// </param>
        public SignatureMissingException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
#endif
    }
}
