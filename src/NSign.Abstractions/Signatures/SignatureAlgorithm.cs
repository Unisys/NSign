namespace NSign.Signatures
{
    /// <summary>
    /// Defines signature algorithms for HTTP messages that are supported by NSign.
    /// </summary>
    public enum SignatureAlgorithm
    {
        /// <summary>
        /// Unknown signature algorithm.
        /// </summary>
        Unknown,

        /// <summary>
        /// Asymmetric signature with RSA with PSS signature padding and SHA-512 signature hashes.
        /// </summary>
        /// <remarks>
        /// The string representation is "rsa-pss-sha512".
        /// </remarks>
        RsaPssSha512,

        /// <summary>
        /// Asymmetric signature with RSA with PKCS #1 v1.5 signature padding and SHA-256 signature hashes.
        /// </summary>
        /// <remarks>
        /// The string representation is "rsa-v1_5-sha256".
        /// </remarks>
        RsaPkcs15Sha256,

        /// <summary>
        /// Symmetric signature with HMAC SHA-256.
        /// </summary>
        /// <remarks>
        /// The string representation is "hmac-sha256".
        /// </remarks>
        HmacSha256,

        /// <summary>
        /// Asymmetric signature with ECDSA with curve P-256 and SHA-256.
        /// </summary>
        /// <remarks>
        /// The string representation is "ecdsa-p256-sha256".
        /// </remarks>
        EcdsaP256Sha256,

        /// <summary>
        /// Asymmetric signature with ECDSA with curve P-384 and SHA-384.
        /// </summary>
        /// <remarks>
        /// The string representation is "ecdsa-p384-sha384".
        /// </remarks>
        EcdsaP384Sha384,
    }
}
