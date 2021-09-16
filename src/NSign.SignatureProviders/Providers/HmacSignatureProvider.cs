using NSign.Signatures;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace NSign.Providers
{
    /// <summary>
    /// Base class for HMAC (symmetric) signature providers.
    /// </summary>
    public abstract class HmacSignatureProvider : SignatureProvider
    {
        /// <summary>
        /// The name of the symmetric signature algorithm provided by this instance.
        /// </summary>
        private readonly string algorithmName;

        /// <summary>
        /// Initializes a new instance of HmacSignatureProvider.
        /// </summary>
        /// <param name="algorithmName">
        /// The name of the symmetric signature algorithm provided by this instance.
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if the value should not
        /// be set / is not important.
        /// </param>
        protected HmacSignatureProvider(string algorithmName, string keyId) : base(keyId)
        {
            if (String.IsNullOrWhiteSpace(algorithmName))
            {
                throw new ArgumentNullException(nameof(algorithmName));
            }

            this.algorithmName = algorithmName;
        }

        /// <inheritdoc/>
        public override Task<byte[]> SignAsync(byte[] input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using HMAC hmac = GetAlgorithm();
            return Task.FromResult(hmac.ComputeHash(input));
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

            using HMAC hmac = GetAlgorithm();
            byte[] actualSignature = hmac.ComputeHash(input);

            VerificationResult result = VerificationResult.SignatureMismatch;
            if (CryptographicOperations.FixedTimeEquals(expectedSignature, actualSignature))
            {
                result = VerificationResult.SuccessfullyVerified;
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Gets a new instance of the HMAC algorithm used for signing and verification.
        /// </summary>
        /// <returns>
        /// A new instance of the HMAC algorithm used.
        /// </returns>
        protected abstract HMAC GetAlgorithm();
    }
}
