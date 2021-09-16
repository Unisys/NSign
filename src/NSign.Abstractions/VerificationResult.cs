namespace NSign
{
    /// <summary>
    /// Represents the possible outcomes of signature verification.
    /// </summary>
    public enum VerificationResult
    {
        /// <summary>
        /// The outcome is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The signature input for a signature was not found.
        /// </summary>
        SignatureInputNotFound,

        /// <summary>
        /// The signature input for a signature was malformed.
        /// </summary>
        SignatureInputMalformed,

        /// <summary>
        /// The signature input is referring to components that do not exist on the HTTP message.
        /// </summary>
        SignatureInputComponentMissing,

        /// <summary>
        /// A verifier matching the algorithm and / or key was not found.
        /// </summary>
        NoMatchingVerifierFound,

        /// <summary>
        /// The signature has already expired.
        /// </summary>
        SignatureExpired,

        /// <summary>
        /// The signature did not match / verification failed.
        /// </summary>
        SignatureMismatch,

        /// <summary>
        /// Verification of the signature succeeded.
        /// </summary>
        SuccessfullyVerified,
    }
}
