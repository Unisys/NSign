using System.Diagnostics;

namespace NSign.Signatures
{
    /// <summary>
    /// Represents a signature component based on a HTTP message header field that is known to be a structured field
    /// according to RFC 8941.
    /// </summary>
    [DebuggerDisplay("Type={Type}, Component={ComponentName}, StructuredField")]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode() -- Intentional: the hash algorithm is the same.
    public sealed class HttpHeaderStructuredFieldComponent : HttpHeaderComponent
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        /// <summary>
        /// Initializes a new instance of HttpHeaderStructuredFieldComponent.
        /// </summary>
        /// <param name="name">
        /// The name of the HTTP message header this component represents.
        /// </param>
        public HttpHeaderStructuredFieldComponent(string name)
            : this(name, bindRequest: false) { }

        /// <summary>
        /// Initializes a new instance of HttpHeaderStructuredFieldComponent.
        /// </summary>
        /// <param name="name">
        /// The name of the HTTP message header this component represents.
        /// </param>
        /// <param name="bindRequest">
        /// Whether or not the component should be bound to the request. This represents the <c>req</c> flag from the
        /// standard.
        /// </param>
        public HttpHeaderStructuredFieldComponent(string name, bool bindRequest)
            : this(name, bindRequest, fromTrailers: false) { }

        /// <summary>
        /// Initializes a new instance of HttpHeaderStructuredFieldComponent.
        /// </summary>
        /// <param name="name">
        /// The name of the HTTP message header this component represents.
        /// </param>
        /// <param name="bindRequest">
        /// Whether or not the component should be bound to the request. This represents the <c>req</c> flag from the
        /// standard.
        /// </param>
        /// <param name="fromTrailers">
        /// Whether the component should be taken from the trailers. This represents the <c>tr</c> flag from the
        /// standard.
        /// </param>
        public HttpHeaderStructuredFieldComponent(string name, bool bindRequest, bool fromTrailers)
            : base(name, bindRequest, useByteSequence: false, fromTrailers) { }

        /// <inheritdoc/>
        public override void Accept(ISignatureComponentVisitor visitor)
        {
            visitor.Visit(this);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is HttpHeaderStructuredFieldComponent httpHeaderDict)
            {
                return Equals(httpHeaderDict);
            }

            return false;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{ComponentName};sf";
        }
    }
}
