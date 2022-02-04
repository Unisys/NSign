using System;
using System.Diagnostics;

namespace NSign.Signatures
{
    /// <summary>
    /// Represents the '@request-response' derived signature component for a specific signature identified by its key.
    /// </summary>
    [DebuggerDisplay("Type={Type}, Component={ComponentName}, Key={Key}")]
    public sealed class RequestResponseComponent :
        DerivedComponent, ISignatureComponentWithKey, IEquatable<RequestResponseComponent>
    {
        /// <summary>
        /// Initializes a new instance RequestResponseComponent.
        /// </summary>
        /// <param name="key">
        /// The key of the signature and signature input to correlate.
        /// </param>
        public RequestResponseComponent(string key) : base(Constants.DerivedComponents.RequestResponse)
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

        #region IEquatable<RequestResponseComponent>

        /// <inheritdoc/>
        public bool Equals(RequestResponseComponent other)
        {
            if (null == other)
            {
                return false;
            }
            else if (ReferenceEquals(this, other))
            {
                return true;
            }

            return StringComparer.Ordinal.Equals(Key, other.Key);
        }

        #endregion

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return base.Equals(obj as RequestResponseComponent);
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
