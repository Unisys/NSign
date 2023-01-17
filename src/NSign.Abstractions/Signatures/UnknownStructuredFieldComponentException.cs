using System;
using System.Runtime.Serialization;

namespace NSign.Signatures
{
    /// <summary>
    /// Exception tracking the use of unknow
    /// </summary>
    public sealed class UnknownStructuredFieldComponentException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UnknownStructuredFieldComponentException"/>.
        /// </summary>
        /// <param name="httpHeaderStructuredField">
        /// The <see cref="HttpHeaderStructuredFieldComponent"/> that caused the exception.
        /// </param>
        public UnknownStructuredFieldComponentException(HttpHeaderStructuredFieldComponent httpHeaderStructuredField)
            : base(GetMessage(httpHeaderStructuredField))
        {
            HttpHeaderStructuredField = httpHeaderStructuredField;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="UnknownStructuredFieldComponentException"/>.
        /// </summary>
        /// <param name="info">
        /// The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="StreamingContext"/> that contains contextual information about the source or destination.
        /// </param>
        public UnknownStructuredFieldComponentException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }

        /// <summary>
        /// Gets the <see cref="HttpHeaderStructuredFieldComponent"/> that caused the exception.
        /// </summary>
        public HttpHeaderStructuredFieldComponent? HttpHeaderStructuredField { get; }

        /// <summary>
        /// Gets the message for the exception.
        /// </summary>
        /// <param name="httpHeaderStructuredField">
        ///  The <see cref="HttpHeaderStructuredFieldComponent"/> that caused the exception.
        /// </param>
        /// <returns>
        /// A string that serves as the exception message.
        /// </returns>
        private static string GetMessage(HttpHeaderStructuredFieldComponent httpHeaderStructuredField)
        {
            return $"The HTTP field '{httpHeaderStructuredField.ComponentName}' is not registered as a structured field. " +
                "Did you forget to register this field in HttpFieldOptions.StructuredFieldsMap?";
        }
    }
}
