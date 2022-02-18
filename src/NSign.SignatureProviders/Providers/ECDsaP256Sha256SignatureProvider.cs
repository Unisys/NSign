using System;
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
        public ECDsaP256Sha256SignatureProvider(X509Certificate2 certificate, string keyId) :
            base(certificate, Constants.SignatureAlgorithms.EcdsaP256Sha256, keyId)
        { }

        /// <inheritdoc/>
        protected override HashAlgorithmName SignatureHash => HashAlgorithmName.SHA256;

        /// <inheritdoc/>
        protected override void CheckKeyAlgorithm(ECDsa publicKey, string parameterName)
        {
            base.CheckKeyAlgorithm(publicKey, parameterName);
            ECParameters parameters = publicKey.ExportParameters(false);

            if (parameters.Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
            {
                throw new ArgumentException(
                    $"A certificate with elliptic curve P-256 (oid: {ECCurve.NamedCurves.nistP256.Oid.Value}) is expected, " +
                    $"but curve '{parameters.Curve.Oid.FriendlyName}' (oid: {parameters.Curve.Oid.Value}) was provided.",
                    parameterName);
            }
        }
    }
}
