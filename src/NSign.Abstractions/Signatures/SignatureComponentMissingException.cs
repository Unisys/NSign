using System;
using System.Runtime.Serialization;

namespace NSign.Signatures
{
    /// <summary>
    /// Exception to track missing signature components from the input HTTP message.
    /// </summary>
    public sealed class SignatureComponentMissingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of SignatureComponentMissingException.
        /// </summary>
        /// <param name="component">
        /// The ISignatureComponent which caused the exception.
        /// </param>
        public SignatureComponentMissingException(ISignatureComponent component) : base(GetMessage(component))
        {
            Component = component;
        }

        /// <summary>
        /// Initializes a new instance of SignatureComponentMissingException.
        /// </summary>
        /// <param name="componentWithKey">
        /// The ISignatureComponentWithKey which caused the exception.
        /// </param>
        public SignatureComponentMissingException(ISignatureComponentWithKey componentWithKey)
            : base(GetMessage(componentWithKey))
        {
            Component = componentWithKey;
        }

        /// <summary>
        /// Initializes a new instance of SignatureComponentMissingException.
        /// </summary>
        /// <param name="componentWithName">
        /// The ISignatureComponentWithName which caused the exception.
        /// </param>
        public SignatureComponentMissingException(ISignatureComponentWithName componentWithName)
            : base(GetMessage(componentWithName))
        {
            Component = componentWithName;
        }

        /// <summary>
        /// Initializes a new instance of SignatureComponentMissingException.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized object data about the exception
        /// being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information about the source or
        /// destination.
        /// </param>
        public SignatureComponentMissingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Gets the ISignatureComponent which caused the exception.
        /// </summary>
        public ISignatureComponent Component { get; }

        /// <summary>
        /// Gets the exception message for the given component.
        /// </summary>
        /// <param name="component">
        /// The ISignatureComponent that caused the exception.
        /// </param>
        /// <returns>
        /// A string that represents the exception's message.
        /// </returns>
        private static string GetMessage(ISignatureComponent component)
        {
            return $"The signature component '{component.ComponentName}' of does not exist but is required.";
        }

        /// <summary>
        /// Gets the exception message for the given component.
        /// </summary>
        /// <param name="component">
        /// The ISignatureComponentWithKey that caused the exception.
        /// </param>
        /// <returns>
        /// A string that represents the exception's message.
        /// </returns>
        private static string GetMessage(ISignatureComponentWithKey component)
        {
            return $"The signature component '{component.ComponentName};key=\"{component.Key}\"' does not exist but is required.";
        }

        /// <summary>
        /// Gets the exception message for the given component.
        /// </summary>
        /// <param name="component">
        /// The ISignatureComponentWithName that caused the exception.
        /// </param>
        /// <returns>
        /// A string that represents the exception's message.
        /// </returns>
        private static string GetMessage(ISignatureComponentWithName component)
        {
            return $"The signature component '{component.ComponentName};key=\"{component.Name}\"' does not exist but is required.";
        }
    }
}
