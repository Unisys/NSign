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
        /// Represents the '@method' derived component.
        /// </summary>
        public static readonly DerivedComponent Method = new DerivedComponent(Constants.DerivedComponents.Method);

        /// <summary>
        /// Represents the '@target-uri' derived component.
        /// </summary>
        public static readonly DerivedComponent RequestTargetUri = new DerivedComponent(Constants.DerivedComponents.TargetUri);

        /// <summary>
        /// Represents the '@authority' derived component.
        /// </summary>
        public static readonly DerivedComponent Authority = new DerivedComponent(Constants.DerivedComponents.Authority);

        /// <summary>
        /// Represents the '@scheme' derived component.
        /// </summary>
        public static readonly DerivedComponent Scheme = new DerivedComponent(Constants.DerivedComponents.Scheme);

        /// <summary>
        /// Represents the '@request-target' derived component.
        /// </summary>
        public static readonly DerivedComponent RequestTarget = new DerivedComponent(Constants.DerivedComponents.RequestTarget);

        /// <summary>
        /// Represents the '@path' derived component.
        /// </summary>
        public static readonly DerivedComponent Path = new DerivedComponent(Constants.DerivedComponents.Path);

        /// <summary>
        /// Represents the '@query' derived component.
        /// </summary>
        public static readonly DerivedComponent Query = new DerivedComponent(Constants.DerivedComponents.Query);

        /// <summary>
        /// Represents the '@status' derived component.
        /// </summary>
        public static readonly DerivedComponent Status = new DerivedComponent(Constants.DerivedComponents.Status);

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

        #region Derived Components with Request-Response binding

        /// <summary>
        /// Represents the '@method' derived component.
        /// </summary>
        internal static readonly DerivedComponent RequestBoundMethod =
            new DerivedComponent(Constants.DerivedComponents.Method, bindRequest: true);

        /// <summary>
        /// Represents the '@target-uri' derived component.
        /// </summary>
        internal static readonly DerivedComponent RequestBoundRequestTargetUri =
            new DerivedComponent(Constants.DerivedComponents.TargetUri, bindRequest: true);

        /// <summary>
        /// Represents the '@authority' derived component.
        /// </summary>
        internal static readonly DerivedComponent RequestBoundAuthority =
            new DerivedComponent(Constants.DerivedComponents.Authority, bindRequest: true);

        /// <summary>
        /// Represents the '@scheme' derived component.
        /// </summary>
        internal static readonly DerivedComponent RequestBoundScheme =
            new DerivedComponent(Constants.DerivedComponents.Scheme, bindRequest: true);

        /// <summary>
        /// Represents the '@request-target' derived component.
        /// </summary>
        internal static readonly DerivedComponent RequestBoundRequestTarget =
            new DerivedComponent(Constants.DerivedComponents.RequestTarget, bindRequest: true);

        /// <summary>
        /// Represents the '@path' derived component.
        /// </summary>
        internal static readonly DerivedComponent RequestBoundPath =
            new DerivedComponent(Constants.DerivedComponents.Path, bindRequest: true);

        /// <summary>
        /// Represents the '@query' derived component.
        /// </summary>
        internal static readonly DerivedComponent RequestBoundQuery =
            new DerivedComponent(Constants.DerivedComponents.Query, bindRequest: true);

        #endregion

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
            : this(type, componentName, bindRequest: false) { }

        /// <summary>
        /// Initializes a new instance of SignatureComponent.
        /// </summary>
        /// <param name="type">
        /// A SignatureComponentType value defining the type of the signature component.
        /// </param>
        /// <param name="componentName">
        /// A string representing the signature component's name.
        /// </param>
        /// <param name="bindRequest">
        /// Whether or not the component should be bound to the request. This represents the <c>req</c> flag from the
        /// standard.
        /// </param>
        protected SignatureComponent(SignatureComponentType type, string componentName, bool bindRequest)
        {
            if (SignatureComponentType.HttpHeader != type && SignatureComponentType.Derived != type)
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }
            if (String.IsNullOrWhiteSpace(componentName))
            {
                throw new ArgumentNullException(nameof(componentName));
            }

            Type = type;
            ComponentName = componentName.ToLower();
            BindRequest = bindRequest;
        }

        #endregion

        #region ISignatureComponent Implementation

        /// <inheritdoc/>
        public SignatureComponentType Type { get; }

        /// <inheritdoc/>
        public string ComponentName { get; }

        /// <inheritdoc/>
        public bool BindRequest { get; }

        /// <inheritdoc/>
        public string? OriginalIdentifier { get; init; }

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
                BindRequest == other.BindRequest &&
                StringComparer.Ordinal.Equals(ComponentName, other.ComponentName);
        }

        #endregion

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is SignatureComponent comp)
            {
                return Equals(comp);
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Type.GetHashCode() ^ ComponentName.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string suffix = BindRequest ? ";req" : String.Empty;
            return $"{ComponentName}{suffix}";
        }
    }
}
