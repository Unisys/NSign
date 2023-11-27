using System;

namespace NSign.Signatures
{
    /// <summary>
    /// Represents a signature component based on a HTTP message header field.
    /// </summary>
    public class HttpHeaderComponent : SignatureComponent, IEquatable<HttpHeaderComponent>
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
            : this(name, bindRequest, useByteSequence: false, fromTrailers: false) { }

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
        /// <param name="useByteSequence">
        /// Whether the component should be encoded as a byte sequence. This represents the <c>bs</c> flag from the
        /// standard.
        /// </param>
        /// <param name="fromTrailers">
        /// Whether the component should be taken from the trailers. This represents the <c>tr</c> flag from the
        /// standard.
        /// </param>
        public HttpHeaderComponent(string name, bool bindRequest, bool useByteSequence, bool fromTrailers)
            : base(SignatureComponentType.HttpHeader, name, bindRequest)
        {
            UseByteSequence = useByteSequence;
            FromTrailers = fromTrailers;
        }

        /// <summary>
        /// Gets a flag indicating whether the component should be encoded as a byte sequence. This represents the
        /// <c>bs</c> flag from the standard.
        /// </summary>
        public bool UseByteSequence { get; }

        /// <summary>
        /// Gets a flag indicating whether the  component should be taken from the trailers. This represents the
        /// <c>tr</c> flag from the standard.
        /// </summary>
        public bool FromTrailers { get; }

        #region IEquatable<HttpHeaderComponent> Implementation

        /// <inheritdoc/>
        public bool Equals(HttpHeaderComponent? other)
        {
            if (null == other)
            {
                return false;
            }
            else if (ReferenceEquals(this, other))
            {
                return true;
            }

            return
                base.Equals(other) &&
                UseByteSequence == other.UseByteSequence &&
                FromTrailers == other.FromTrailers;
        }

        #endregion

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is HttpHeaderComponent comp)
            {
                return Equals(comp);
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode() ^ UseByteSequence.GetHashCode() ^ FromTrailers.GetHashCode();
        }

        /// <inheritdoc/>
        public override void Accept(ISignatureComponentVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
