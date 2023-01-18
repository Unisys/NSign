using NSign.Http;
using System;
using System.Runtime.Serialization;

namespace NSign.Signatures
{
    /// <summary>
    /// Exception to track parsing errors on structured HTTP fields.
    /// </summary>
    public sealed class StructuredFieldParsingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="StructuredFieldParsingException"/>.
        /// </summary>
        /// <param name="fieldName">
        /// The name of the HTTP field for which the exception is raised.
        /// </param>
        /// <param name="type">
        /// The expected structured field type of value.
        /// </param>
        public StructuredFieldParsingException(string fieldName, StructuredFieldType type)
            : base(GetMessage(fieldName, type))
        {
            FieldName = fieldName;
            Type = type;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StructuredFieldParsingException"/>.
        /// </summary>
        /// <param name="info">
        /// The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="StreamingContext"/> that contains contextual information about the source or destination.
        /// </param>
        public StructuredFieldParsingException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }

        /// <summary>
        /// Gets the name of the HTTP field for which the exception is raised.
        /// </summary>
        public string? FieldName { get; }

        /// <summary>
        /// Gets the expected structured field type of value.
        /// </summary>
        public StructuredFieldType? Type { get; }

        /// <summary>
        /// Gets the message for the exception.
        /// </summary>
        /// <param name="fieldName">
        /// The name of the HTTP field for which the exception is raised.
        /// </param>
        /// <param name="type">
        /// The expected structured field type of value.
        /// </param>
        /// <returns>
        /// A string that serves as the exception message.
        /// </returns>
        private static string GetMessage(string fieldName, StructuredFieldType type)
        {
            return $"The value of the structured HTTP field '{fieldName}' of type {type} could not be parsed.";
        }
    }
}
