using System.Diagnostics;

namespace NSign.Signatures
{
    /// <summary>
    /// Represents a signature component based on a HTTP message header field that is known to be a structured field
    /// according to RFC 8941.
    /// </summary>
    [DebuggerDisplay("Type={Type}, Component={ComponentName}, StructuredField")]
    public sealed class HttpHeaderStructuredFieldComponent : HttpHeaderComponent
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
            : base(name, bindRequest) { }

        /// <inheritdoc/>
        public override void Accept(ISignatureComponentVisitor visitor)
        {
            visitor.Visit(this);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{ComponentName};sf";
        }
    }
}
