namespace NSign.Signatures
{
    /// <summary>
    /// Represents a signature component based on a HTTP message header field.
    /// </summary>
    public class HttpHeaderComponent : SignatureComponent
    {
        /// <summary>
        /// Initializes a new instance of HttpHeaderComponent.
        /// </summary>
        /// <param name="name">
        /// The name of the HTTP message header this component represents.
        /// </param>
        public HttpHeaderComponent(string name) : this(name, bindRequest: false) { }

        /// <summary>
        /// Initializes a new instance of HttpHeaderComponent.
        /// </summary>
        /// <param name="name">
        /// The name of the HTTP message header this component represents.
        /// </param>
        /// <param name="bindRequest">
        /// Whether or not the component should be bound to the request. This represents the <c>req</c> flag from the
        /// standard.
        /// </param>
        public HttpHeaderComponent(string name, bool bindRequest)
            : base(SignatureComponentType.HttpHeader, name, bindRequest) { }

        /// <inheritdoc/>
        public override void Accept(ISignatureComponentVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
