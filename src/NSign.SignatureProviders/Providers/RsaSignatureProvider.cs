using NSign.Signatures;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace NSign.Providers
{
    /// <summary>
    /// Base class for RSA (asymmetric) signature providers.
    /// </summary>
    public abstract class RsaSignatureProvider : SignatureProvider, IDisposable
    {
        /// <summary>
        /// The private key used to sign.
        /// </summary>
        private readonly RSA privateKey;

        /// <summary>
        /// The public key used to verify signatures.
        /// </summary>
        private readonly RSA publicKey;

        /// <summary>
        /// The name of the symmetric signature algorithm provided by this instance.
        /// </summary>
        private readonly string algorithmName;

        /// <summary>
        /// Initializes a new instance of RsaSignatureProvider.
        /// </summary>
        /// <param name="certificate">
        /// The <see cref="X509Certificate2"/> to use to get the public key and the private key (only needed if the
        /// provider is used to create signatures).
        /// </param>
        /// <param name="algorithmName">
        /// The name of the asymmetric signature algorithm provided by this instance.
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if the value should not
        /// be set / is not important.
        /// </param>
        public RsaSignatureProvider(X509Certificate2 certificate, string algorithmName, string keyId) : base(keyId)
        {
            Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));

            privateKey = (RSA)Certificate.PrivateKey;
            publicKey = (RSA)Certificate.PublicKey.Key;

            if (String.IsNullOrWhiteSpace(algorithmName))
            {
                throw new ArgumentNullException(nameof(algorithmName));
            }
            this.algorithmName = algorithmName;
        }

        /// <summary>
        /// Gets the <see cref="X509Certificate2"/> to use to get the public key and the private key.
        /// </summary>
        /// <remarks>
        /// A certificate with access to the private key is needed only when signatures must be created.
        /// </remarks>
        public X509Certificate2 Certificate { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (null != privateKey)
            {
                privateKey.Dispose();
            }

            if (null != publicKey)
            {
                publicKey.Dispose();
            }

            if (null != Certificate)
            {
                Certificate.Dispose();
            }
        }

        /// <inheritdoc/>
        public override Task<byte[]> SignAsync(byte[] input, CancellationToken cancellationToken)
        {
            if (null == privateKey)
            {
                throw new InvalidOperationException("Cannot sign using a certificate without a private key.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(privateKey.SignData(input, SignatureHash, SignaturePadding));
        }

        /// <inheritdoc/>
        public override Task<VerificationResult> VerifyAsync(
            SignatureParamsComponent signatureParams,
            byte[] input,
            byte[] expectedSignature,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // If the signature parameters has the 'alg' parameter set, it must match the algorithm provided with this
            // instance. The same goes for the 'keyid' paramter, provided that it is set with this instance.
            if (!signatureParams.HasMatchingAlgorithm(algorithmName) ||
                (!String.IsNullOrEmpty(KeyId) && !signatureParams.HasMatchingKeyId(KeyId)))
            {
                return Task.FromResult(VerificationResult.NoMatchingVerifierFound);
            }

            VerificationResult result = VerificationResult.SignatureMismatch;
            if (publicKey.VerifyData(input, expectedSignature, SignatureHash, SignaturePadding))
            {
                result = VerificationResult.SuccessfullyVerified;
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Gets the <see cref="HashAlgorithmName"/> struct identifiying the hash to use for the signatures.
        /// </summary>
        protected abstract HashAlgorithmName SignatureHash { get; }

        /// <summary>
        /// Gets the <see cref="RSASignaturePadding"/> object identifying the signature padding mode to use for the signatures.
        /// </summary>
        protected abstract RSASignaturePadding SignaturePadding { get; }
    }
}
