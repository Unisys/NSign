using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NSign.Providers
{
    /// <summary>
    /// Provides signatures using ECDSA with P-384 and SHA-384 signature hashes.
    /// </summary>
    public sealed class ECDsaP382Sha384SignatureProvider : ECDsaSignatureProvider
    {
        /// <summary>
        /// Initializes a new instance of ECDsaP382Sha384SignatureProvider.
        /// </summary>
        /// <param name="certificate">
        /// The <see cref="X509Certificate2"/> to use to get the public key and the private key (only needed if the
        /// provider is used to create signatures).
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if the value should not
        /// be set / is not important.
        /// </param>
        public ECDsaP382Sha384SignatureProvider(X509Certificate2 certificate, string keyId) :
            base(certificate,
                 ECCurve.NamedCurves.nistP384.Oid.Value,
                 "P-384",
                 Constants.SignatureAlgorithms.EcdsaP384Sha384,
                 keyId)
        { }

        /// <inheritdoc/>
        protected override HashAlgorithmName SignatureHash => HashAlgorithmName.SHA384;
    }
}
