using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NSign.Signatures
{
    /// <summary>
    /// Represents an exception that is thrown when an HTTP message has one or multiple signatures that failed verification.
    /// </summary>
    public sealed class SignatureVerificationFailedException : Exception
    {
        /// <summary>
        /// Initializes a new SignatureVerificationFailedException.
        /// </summary>
        /// <param name="signatureNames">
        /// The names of the signatures for which verification failed.
        /// </param>
        public SignatureVerificationFailedException(IEnumerable<string> signatureNames) :
            base($"Some signatures have failed verification: {String.Join(", ", signatureNames)}.")
        { }

        /// <summary>
        /// Initializes a new instance of SignatureVerificationFailedException.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized object data about the exception
        /// being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information about the source or
        /// destination.
        /// </param>
        public SignatureVerificationFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }
}
