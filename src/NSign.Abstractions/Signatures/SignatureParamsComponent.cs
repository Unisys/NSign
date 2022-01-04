using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NSign.Signatures
{
    /// <summary>
    /// Represents the '@signature-params' specialty signature component.
    /// </summary>
    public sealed class SignatureParamsComponent : SpecialtyComponent
    {
        /// <summary>
        /// The Collection of SignatureComponent objects that should be included in the signature.
        /// </summary>
        private readonly Collection<SignatureComponent> components;

        #region C'tors

        /// <summary>
        /// Initializes a new instance of SignatureParamsComponent.
        /// </summary>
        public SignatureParamsComponent() : base(Constants.SpecialtyComponents.SignatureParams)
        {
            components = new Collection<SignatureComponent>();
        }

        /// <summary>
        /// Initializes a new instance of SignatureParamsComponent.
        /// </summary>
        /// <param name="value">
        /// The value to parse the component data from.
        /// </param>
        public SignatureParamsComponent(string value) : this()
        {
            OriginalValue = value;
            SignatureInputParser.ParseAndUpdate(value, this);
        }

        #endregion

        /// <inheritdoc/>
        public override void Accept(ISignatureComponentVisitor visitor)
        {
            visitor.Visit(this);
        }

        /// <summary>
        /// Gets a read-only collection of SignatureComponent objects that are included in a signature.
        /// </summary>
        public IReadOnlyCollection<SignatureComponent> Components => components;

        /// <summary>
        /// Gets the original (unparsed) value of the component; this is null exept when the component was created through
        /// the parser.
        /// </summary>
        public string OriginalValue { get; }

        /// <summary>
        /// Gets or sets a nullable DateTimeOffset value that defines when the signature was created.
        /// </summary>
        public DateTimeOffset? Created { get; set; }

        /// <summary>
        /// Gets or sets a nullable DateTimeOffset value that defines when the signature expires.
        /// </summary>
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        /// Gets or sets a string value that serves as the signature's nonce.
        /// </summary>
        public string Nonce { get; set; }

        /// <summary>
        /// Gets or sets the string that defines the signing algorithm used.
        /// </summary>
        public string Algorithm { get; set; }

        /// <summary>
        /// Gets or sets the ID of the key used for signing.
        /// </summary>
        public string KeyId { get; set; }

        #region Fluent interface

        /// <summary>
        /// Adds a new SignatureComponent to this signature parameters.
        /// </summary>
        /// <param name="component">
        /// The SignatureComponent object to add.
        /// </param>
        /// <returns>
        /// The SignatureParamsComponent on which the method was called.
        /// </returns>
        public SignatureParamsComponent AddComponent(SignatureComponent component)
        {
            if (component is SignatureParamsComponent ||
                component.ComponentName.Equals(Constants.SpecialtyComponents.SignatureParams))
            {
                throw new InvalidOperationException("Cannot add a '@signature-params' component to a SignatureParamsComponent.");
            }

            components.Add(component);

            return this;
        }

        /// <summary>
        /// Sets the 'created' parameter to the current timestamp.
        /// </summary>
        /// <returns>
        /// The SignatureParamsComponent on which the method was called.
        /// </returns>
        public SignatureParamsComponent WithCreatedNow()
        {
            return WithCreated(DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Sets the 'created' parameter to the given timestamp.
        /// </summary>
        /// <param name="created">
        /// The nullable DateTimeOffset value to set.
        /// </param>
        /// <returns>
        /// The SignatureParamsComponent on which the method was called.
        /// </returns>
        public SignatureParamsComponent WithCreated(DateTimeOffset? created)
        {
            Created = created;

            return this;
        }

        /// <summary>
        /// Sets the 'expires' parameter to the given timestamp.
        /// </summary>
        /// <param name="expires">
        /// The nullable DateTimeOffset value to set.
        /// </param>
        /// <returns>
        /// The SignatureParamsComponent on which the method was called.
        /// </returns>
        public SignatureParamsComponent WithExpires(DateTimeOffset? expires)
        {
            Expires = expires;

            return this;
        }

        /// <summary>
        /// Sets the 'expires' parameter to the timestamp with the given relativeExpirationFromNow offset from now.
        /// </summary>
        /// <param name="relativeExpirationFromNow">
        /// A TimeSpan value that defines the relative expiration (from now).
        /// </param>
        /// <returns>
        /// The SignatureParamsComponent on which the method was called.
        /// </returns>
        public SignatureParamsComponent WithExpires(TimeSpan relativeExpirationFromNow)
        {
            Expires = DateTimeOffset.Now.Add(relativeExpirationFromNow);

            return this;
        }

        /// <summary>
        /// Sets the 'nonce' parameter to the given value.
        /// </summary>
        /// <param name="nonce">
        /// The nonce value to set.
        /// </param>
        /// <returns>
        /// The SignatureParamsComponent on which the method was called.
        /// </returns>
        public SignatureParamsComponent WithNonce(string nonce)
        {
            Nonce = nonce;

            return this;
        }

        /// <summary>
        /// Sets the 'alg' parameter to the given SignatureAlgorithm value.
        /// </summary>
        /// <param name="algorithm">
        /// The nullable SignatureAlgorithm value to set.
        /// </param>
        /// <returns>
        /// The SignatureParamsComponent on which the method was called.
        /// </returns>
        public SignatureParamsComponent WithAlgorithm(SignatureAlgorithm? algorithm)
        {
            Algorithm = algorithm.HasValue ? algorithm.Value.GetName() : null;

            return this;
        }

        /// <summary>
        /// Sets the 'keyid' parameter to the given value.
        /// </summary>
        /// <param name="keyId">
        /// The string to set.
        /// </param>
        /// <returns>
        /// The SignatureParamsComponent on which the method was called.
        /// </returns>
        public SignatureParamsComponent WithKeyId(string keyId)
        {
            KeyId = keyId;

            return this;
        }

        #endregion
    }
}
