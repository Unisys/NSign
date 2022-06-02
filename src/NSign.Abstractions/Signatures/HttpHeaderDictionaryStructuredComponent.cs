using System;
using System.Diagnostics;

namespace NSign.Signatures
{
    /// <summary>
    /// Represents a signature component based on a HTTP message header field that has dictionary structured values.
    /// </summary>
    [DebuggerDisplay("Type={Type}, Component={ComponentName}, Key={Key}")]
    public class HttpHeaderDictionaryStructuredComponent :
        HttpHeaderComponent, ISignatureComponentWithKey, IEquatable<HttpHeaderDictionaryStructuredComponent>
    {
        /// <summary>
        /// Initializes a new instance of HttpHeaderDictionaryStructuredComponent.
        /// </summary>
        /// <param name="name">
        /// The name of the HTTP message header this component represents.
        /// </param>
        /// <param name="key">
        /// The key of the dictionary-structured value to use.
        /// </param>
        public HttpHeaderDictionaryStructuredComponent(string name, string key) : this(name, key, bindRequest: false) { }

        /// <summary>
        /// Initializes a new instance of HttpHeaderDictionaryStructuredComponent.
        /// </summary>
        /// <param name="name">
        /// The name of the HTTP message header this component represents.
        /// </param>
        /// <param name="key">
        /// The key of the dictionary-structured value to use.
        /// </param>
        /// <param name="bindRequest">
        /// Whether or not the component should be bound to the request. This represents the <c>req</c> flag from the
        /// standard.
        /// </param>
        public HttpHeaderDictionaryStructuredComponent(string name, string key, bool bindRequest) : base(name, bindRequest)
        {
            if (String.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            Key = key;
        }

        #region ISignatureComponentWithKey Implementation

        /// <inheritdoc/>
        public string Key { get; }

        #endregion

        /// <inheritdoc/>
        public override void Accept(ISignatureComponentVisitor visitor)
        {
            visitor.Visit(this);
        }

        #region IEquatable<HttpHeaderDictionaryStructuredComponent> Implementation

        /// <inheritdoc/>
        public bool Equals(HttpHeaderDictionaryStructuredComponent other)
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
                GetType() == other.GetType() &&
                BindRequest == other.BindRequest &&
                StringComparer.Ordinal.Equals(ComponentName, other.ComponentName) &&
                StringComparer.Ordinal.Equals(Key, other.Key);
        }

        #endregion

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is HttpHeaderDictionaryStructuredComponent httpHeaderDict)
            {
                return base.Equals(httpHeaderDict);
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Key.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{ComponentName};key={Key}";
        }
    }
}
