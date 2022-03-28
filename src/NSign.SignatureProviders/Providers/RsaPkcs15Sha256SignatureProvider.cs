using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NSign.Providers
{
    /// <summary>
    /// Provides signatures using PKCS #1 v1.5 signature padding and SHA-256 signature hashes.
    /// </summary>
    public sealed class RsaPkcs15Sha256SignatureProvider : RsaSignatureProvider
    {
        /// <summary>
        /// Initializes a new instance of RsaPkcs15Sha256SignatureProvider.
        /// </summary>
        /// <param name="certificate">
        /// The <see cref="X509Certificate2"/> to use to get the public key and the private key (only needed if the
        /// provider is used to create signatures).
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if the value should not
        /// be set / is not important.
        /// </param>
        public RsaPkcs15Sha256SignatureProvider(X509Certificate2 certificate, string? keyId)
            : base(certificate, Constants.SignatureAlgorithms.RsaPkcs15Sha256, keyId)
        { }

        /// <summary>
        /// Initializes a new instance of RsaPkcs15Sha256SignatureProvider.
        /// </summary>
        /// <param name="privateKey">
        /// The <see cref="RSA"/> object that represents the private key or null if signing with this provider is not
        /// needed.
        /// </param>
        /// <param name="publicKey">
        /// The <see cref="RSA"/> object that represents the public key to use for signature verification.
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if the value should not
        /// be set / is not important.
        /// </param>
        public RsaPkcs15Sha256SignatureProvider(RSA? privateKey, RSA publicKey, string? keyId)
            : base(privateKey, publicKey, Constants.SignatureAlgorithms.RsaPkcs15Sha256, keyId)
        { }

        /// <inheritdoc/>
        protected override HashAlgorithmName SignatureHash => HashAlgorithmName.SHA256;

        /// <inheritdoc/>
        protected override RSASignaturePadding SignaturePadding => RSASignaturePadding.Pkcs1;
    }
}
