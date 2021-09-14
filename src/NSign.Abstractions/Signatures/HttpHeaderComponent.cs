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
        public HttpHeaderComponent(string name) : base(SignatureComponentType.HttpHeader, name) { }

        /// <inheritdoc/>
        public override void Accept(ISignatureComponentVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
