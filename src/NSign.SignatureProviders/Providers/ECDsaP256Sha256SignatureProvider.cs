using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NSign.Providers
{
    /// <summary>
    /// Provides signatures using ECDSA with P-256 and SHA-256 signature hashes.
    /// </summary>
    public sealed class ECDsaP256Sha256SignatureProvider : ECDsaSignatureProvider
    {
        /// <summary>
        /// Initializes a new instance of ECDsaP256Sha256SignatureProvider.
        /// </summary>
        /// <param name="certificate">
        /// The <see cref="X509Certificate2"/> to use to get the public key and the private key (only needed if the
        /// provider is used to create signatures).
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if the value should not
        /// be set / is not important.
        /// </param>
        public ECDsaP256Sha256SignatureProvider(X509Certificate2 certificate, string keyId) :
            base(certificate,
                 ECCurve.NamedCurves.nistP256.Oid.Value!,
                 "P-256",
                 Constants.SignatureAlgorithms.EcdsaP256Sha256,
                 keyId)
        { }

        /// <summary>
        /// Initializes a new instance of ECDsaP256Sha256SignatureProvider.
        /// </summary>
        /// <param name="privateKey">
        /// The <see cref="ECDsa"/> object that represents the private key or null if signing with this provider is not
        /// needed.
        /// </param>
        /// <param name="publicKey">
        /// The <see cref="ECDsa"/> object that represents the public key to use for signature verification.
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if the value should not
        /// be set / is not important.
        /// </param>
        public ECDsaP256Sha256SignatureProvider(ECDsa? privateKey, ECDsa publicKey, string? keyId) :
            base(privateKey,
                 publicKey,
                 ECCurve.NamedCurves.nistP256.Oid.Value!,
                 "P-256",
                 Constants.SignatureAlgorithms.EcdsaP256Sha256,
                 keyId)
        { }

        /// <inheritdoc/>
        protected override HashAlgorithmName SignatureHash => HashAlgorithmName.SHA256;
    }
}
