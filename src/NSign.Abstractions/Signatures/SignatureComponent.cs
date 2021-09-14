using System;
using System.Diagnostics;

namespace NSign.Signatures
{
    /// <summary>
    /// Represents a single component that serves as input for a HTTP message signature.
    /// </summary>
    [DebuggerDisplay("Type={Type}, Component={ComponentName}")]
    public abstract class SignatureComponent : ISignatureComponent, IEquatable<SignatureComponent>
    {
        #region Consts

        /// <summary>
        /// Represents the '@method' specialty component.
        /// </summary>
        public static readonly SpecialtyComponent Method = new SpecialtyComponent(Constants.SpecialtyComponents.Method);

        /// <summary>
        /// Represents the '@target-uri' specialty component.
        /// </summary>
        public static readonly SpecialtyComponent RequestTargetUri = new SpecialtyComponent(Constants.SpecialtyComponents.TargetUri);

        /// <summary>
        /// Represents the '@authority' specialty component.
        /// </summary>
        public static readonly SpecialtyComponent Authority = new SpecialtyComponent(Constants.SpecialtyComponents.Authority);

        /// <summary>
        /// Represents the '@scheme' specialty component.
        /// </summary>
        public static readonly SpecialtyComponent Scheme = new SpecialtyComponent(Constants.SpecialtyComponents.Scheme);

        /// <summary>
        /// Represents the '@request-target' specialty component.
        /// </summary>
        public static readonly SpecialtyComponent RequestTarget = new SpecialtyComponent(Constants.SpecialtyComponents.RequestTarget);

        /// <summary>
        /// Represents the '@path' specialty component.
        /// </summary>
        public static readonly SpecialtyComponent Path = new SpecialtyComponent(Constants.SpecialtyComponents.Path);

        /// <summary>
        /// Represents the '@query' specialty component.
        /// </summary>
        public static readonly SpecialtyComponent Query = new SpecialtyComponent(Constants.SpecialtyComponents.Query);

        /// <summary>
        /// Represents the '@status' specialty component.
        /// </summary>
        public static readonly SpecialtyComponent Status = new SpecialtyComponent(Constants.SpecialtyComponents.Status);

        /// <summary>
        /// Represents the 'Digest' HTTP header component.
        /// </summary>
        public static readonly HttpHeaderComponent Digest = new HttpHeaderComponent(Constants.Headers.Digest);

        /// <summary>
        /// Represents the 'Content-Type' HTTP header component.
        /// </summary>
        public static readonly HttpHeaderComponent ContentType = new HttpHeaderComponent(Constants.Headers.ContentType);

        /// <summary>
        /// Represents the 'Content-Length' HTTP header component.
        /// </summary>
        public static readonly HttpHeaderComponent ContentLength = new HttpHeaderComponent(Constants.Headers.ContentLength);

        #endregion

        #region C'tors

        /// <summary>
        /// Initializes a new instance of SignatureComponent.
        /// </summary>
        /// <param name="type">
        /// A SignatureComponentType value defining the type of the signature component.
        /// </param>
        /// <param name="componentName">
        /// A string representing the signature component's name.
        /// </param>
        protected SignatureComponent(SignatureComponentType type, string componentName)
        {
            if (SignatureComponentType.HttpHeader != type && SignatureComponentType.Specialty != type)
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }
            if (String.IsNullOrWhiteSpace(componentName))
            {
                throw new ArgumentNullException(nameof(componentName));
            }

            Type = type;
            ComponentName = componentName.ToLower();
        }

        #endregion

        #region ISignatureComponent Implementation

        /// <inheritdoc/>
        public SignatureComponentType Type { get; }

        /// <inheritdoc/>
        public string ComponentName { get; }

        #endregion

        /// <summary>
        /// Accepts an instance of ISignatureComponentVisitor.
        /// </summary>
        /// <param name="visitor">
        /// An instance of ISignatureComponentVisitor that should be accepted to visit this component.
        /// </param>
        public virtual void Accept(ISignatureComponentVisitor visitor)
        {
            visitor.Visit(this);
        }

        #region IEquatable<SignatureComponent> Implementation

        /// <inheritdoc/>
        public bool Equals(SignatureComponent other)
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
                Type == other.Type &&
                StringComparer.Ordinal.Equals(ComponentName, other.ComponentName);
        }

        #endregion

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as SignatureComponent);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Type.GetHashCode() ^ ComponentName.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ComponentName;
        }
    }
}
