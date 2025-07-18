using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NSign.Providers
{
    /// <summary>
    /// Provides signatures using ECDSA with P-521 and SHA-512 signature hashes.
    /// </summary>
    public class ECDsaP521Sha512SignatureProvider : ECDsaSignatureProvider
    {
        /// <summary>
        /// Initializes a new instance of ECDsaP521Sha512SignatureProvider.
        /// </summary>
        /// <param name="certificate">
        /// The <see cref="X509Certificate2"/> to use to get the public key and the private key (only needed if the
        /// provider is used to create signatures).
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if the value should not
        /// be set / is not important.
        /// </param>
        public ECDsaP521Sha512SignatureProvider(X509Certificate2 certificate, string keyId) :
            base(certificate,
                ECCurve.NamedCurves.nistP521.Oid.Value!,
                "P-521",
                Constants.SignatureAlgorithms.EcdsaP521Sha512,
                keyId)
        { }

        /// <summary>
        /// Initializes a new instance of ECDsaP521Sha512SignatureProvider.
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
        public ECDsaP521Sha512SignatureProvider(ECDsa? privateKey, ECDsa publicKey, string? keyId) :
            base(privateKey,
                publicKey,
                ECCurve.NamedCurves.nistP521.Oid.Value!,
                "P-521",
                Constants.SignatureAlgorithms.EcdsaP521Sha512,
                keyId)
        { }

        /// <inheritdoc/>
        protected override HashAlgorithmName SignatureHash => HashAlgorithmName.SHA512;
    }
}
