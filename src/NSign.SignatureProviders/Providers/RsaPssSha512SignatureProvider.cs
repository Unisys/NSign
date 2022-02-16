using NSign.Signatures;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NSign.Providers
{
    /// <summary>
    /// Provides signatures using PSS signature padding and SHA-512 signature hashes.
    /// </summary>
    public sealed class RsaPssSha512SignatureProvider : RsaSignatureProvider
    {
        /// <summary>
        /// Initializes a new instance of RsaPssSha512SignatureProvider.
        /// </summary>
        /// <param name="certificate">
        /// The <see cref="X509Certificate2"/> to use to get the public key and the private key (only needed if the
        /// provider is used to create signatures).
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if the value should not
        /// be set / is not important.
        /// </param>
        public RsaPssSha512SignatureProvider(X509Certificate2 certificate, string keyId)
            : base(certificate, Constants.SignatureAlgorithms.RsaPssSha512, keyId)
        { }

        /// <inheritdoc/>
        protected override HashAlgorithmName SignatureHash => HashAlgorithmName.SHA512;

        /// <inheritdoc/>
        protected override RSASignaturePadding SignaturePadding => RSASignaturePadding.Pss;
    }
}
