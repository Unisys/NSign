using NSign.Signatures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NSign
{
    /// <summary>
    /// Options class to control signature validation.
    /// </summary>
    public class SignatureVerificationOptions
    {
        /// <summary>
        /// Initializes a new instance of SignatureVerificationOptions.
        /// </summary>
        public SignatureVerificationOptions()
        {
            ShouldVerify = DefaultShouldVerify;
        }

        /// <summary>
        /// Gets an ICollection of string values representing the names of signatures to verify.
        /// </summary>
        public ICollection<string> SignaturesToVerify { get; } = new Collection<string>();

        /// <summary>
        /// Gets or sets a function which takes a string respresenting the name of a signature as input and returns a
        /// boolean indicating whether or not the signature should be verified.
        /// </summary>
        /// <remarks>
        /// This defaults to return true for all signatures registered in the SignaturesToVerify collection.
        /// </remarks>
        public Func<string, bool> ShouldVerify { get; set; }

        /// <summary>
        /// Gets or sets a function which takes a SignatureInputSpec value as input, validates the nonce from the signature
        /// parameters and returns a boolean indicating whether or not the nonce was accepted. If unset, does not verify
        /// the nonce.
        /// </summary>
        public Func<SignatureInputSpec, bool> VerifyNonce { get; set; }

        /// <summary>
        /// Gets an ICollection of SignatureComponent objects representing all the components a signature must present in
        /// order to be accepted.
        /// </summary>
        public ICollection<SignatureComponent> RequiredSignatureComponents { get; } = new Collection<SignatureComponent>();

        /// <summary>
        /// Gets or sets a flag which indicates whether or not the 'created' parameter of signature input must be set in
        /// order for the signature to be accepted. Defaults to <c>true</c>.
        /// </summary>
        public bool CreatedRequired { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag which indicates whether or not the 'expires' parameter of signature input must be set in
        /// order for the signature to be accepted. Defaults to <c>true</c>.
        /// </summary>
        public bool ExpiresRequired { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag which indicates whether or not the 'nonce' parameter of signature input must be set in
        /// order for the signature to be accepted. Defaults to <c>false</c>.
        /// </summary>
        public bool NonceRequired { get; set; } = false;

        /// <summary>
        /// Gets or sets a flag which indicates whether or not the 'alg' parameter of signature input must be set in
        /// order for the signature to be accepted. Defaults to <c>false</c>.
        /// </summary>
        public bool AlgorithmRequired { get; set; } = false;

        /// <summary>
        /// Gets or sets a flag which indicates whether or not the 'keyid' parameter of signature input must be set in
        /// order for the signature to be accepted. Defaults to <c>false</c>.
        /// </summary>
        public bool KeyIdRequired { get; set; } = false;

        /// <summary>
        /// Gets or sets a nullable TimeSpan value that indicates the maximum age of a signature (based on the 'created'
        /// parameter) in order for the signature to accepted. If a value is set, it is applied even if <c>ExpiresRequired == true</c>
        /// and an expiration is set on the signature, provided that the 'created' parameter is set too.
        /// Defaults to 5 minutes.
        /// </summary>
        public TimeSpan? MaxSignatureAge { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Provides the default implementation for the ShouldVerify predicate: all signatures in SignaturesToVerify
        /// should be verified.
        /// </summary>
        /// <param name="signatureName">
        /// The name of the signature to check.
        /// </param>
        /// <returns>
        /// True if the signature with the given name should be verified, or false otherwise.
        /// </returns>
        public bool DefaultShouldVerify(string signatureName)
        {
            return SignaturesToVerify.Contains(signatureName);
        }
    }
}
