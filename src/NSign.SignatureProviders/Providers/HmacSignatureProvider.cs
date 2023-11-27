using NSign.Signatures;
using System;
using System.Runtime.CompilerServices;
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
        protected HmacSignatureProvider(string algorithmName, string? keyId) : base(keyId)
        {
            if (String.IsNullOrWhiteSpace(algorithmName))
            {
                throw new ArgumentNullException(nameof(algorithmName));
            }

            this.algorithmName = algorithmName;
        }

        /// <inheritdoc/>
        public override Task<ReadOnlyMemory<byte>> SignAsync(ReadOnlyMemory<byte> input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using HMAC hmac = GetAlgorithm();
            return Task.FromResult(new ReadOnlyMemory<byte>(hmac.ComputeHash(input.ToArray())));
        }

        /// <inheritdoc/>
        public override Task<VerificationResult> VerifyAsync(
            SignatureParamsComponent signatureParams,
            ReadOnlyMemory<byte> input,
            ReadOnlyMemory<byte> expectedSignature,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // If the signature parameters has the 'alg' parameter set, it must match the algorithm provided with this
            // instance. The same goes for the 'keyid' parameter, provided that it is set with this instance.
            if (!signatureParams.HasMatchingAlgorithm(algorithmName) ||
                (!String.IsNullOrEmpty(KeyId) && !signatureParams.HasMatchingKeyId(KeyId!)))
            {
                return Task.FromResult(VerificationResult.NoMatchingVerifierFound);
            }

            using HMAC hmac = GetAlgorithm();
            byte[] actualSignature = hmac.ComputeHash(input.ToArray());

            VerificationResult result = VerificationResult.SignatureMismatch;
#if NETSTANDARD2_0
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
            {
                // NOTE: This is taken from the runtime source of newer versions.
                //       https://github.com/dotnet/runtime/blob/release/8.0/src/libraries/System.Security.Cryptography/src/System/Security/Cryptography/CryptographicOperations.cs

                // NoOptimization because we want this method to be exactly as non-short-circuiting
                // as written.
                //
                // NoInlining because the NoOptimization would get lost if the method got inlined.

                if (left.Length != right.Length)
                {
                    return false;
                }

                int length = left.Length;
                int accum = 0;

                for (int i = 0; i < length; i++)
                {
                    accum |= left[i] - right[i];
                }

                return accum == 0;
            }

            if (FixedTimeEquals(expectedSignature.Span, actualSignature))
#elif NETSTANDARD2_1_OR_GREATER || NET
            if (CryptographicOperations.FixedTimeEquals(expectedSignature.Span, actualSignature))
#endif
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
