using System;
using System.Runtime.Serialization;

namespace NSign.Signatures
{
    /// <summary>
    /// Exception to describe a signature component that is not allowed in the given context.
    /// </summary>
    public sealed class SignatureComponentNotAllowedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of SignatureComponentNotAllowedException.
        /// </summary>
        /// <param name="message">
        /// The exception message.
        /// </param>
        /// <param name="component">
        /// The ISignatureComponent which caused the exception.
        /// </param>
        public SignatureComponentNotAllowedException(string message, ISignatureComponent component) : base(message)
        {
            Component = component;
        }

#if NET8_0_OR_GREATER
#else
        /// <summary>
        /// Initializes a new instance of SignatureComponentNotAllowedException.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized object data about the exception
        /// being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information about the source or
        /// destination.
        /// </param>
        public SignatureComponentNotAllowedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
#endif

        /// <summary>
        /// Gets the ISignatureComponent which caused the exception.
        /// </summary>
        public ISignatureComponent? Component { get; }
    }
}
