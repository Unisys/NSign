using NSign.Signatures;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NSign.Providers
{
    /// <summary>
    /// Base class for signature providers, implementing both <see cref="ISigner"/> and <see cref="IVerifier"/>.
    /// </summary>
    public abstract class SignatureProvider : ISigner, IVerifier
    {
        /// <summary>
        /// Initializes a new instance of SignatureProvider.
        /// </summary>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if the value should not
        /// be set / is not important.
        /// </param>
        public SignatureProvider(string? keyId)
        {
            KeyId = keyId;
        }

        /// <summary>
        /// Gets the value for the KeyId parameter of signatures produced with this provider or null if the value should
        /// not be set / is not important.
        /// </summary>
        public string? KeyId { get; }

        /// <inheritdoc/>
        public virtual Task UpdateSignatureParamsAsync(SignatureParamsComponent signatureParams, MessageContext messageContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            signatureParams.KeyId = KeyId;

            return Task.CompletedTask;
        }

        public Task<ReadOnlyMemory<byte>> SignAsync(
            string? keyId,
            ReadOnlyMemory<byte> input,
            CancellationToken cancellationToken)
        {
            if (this.KeyId != keyId)
            {
                throw new InvalidOperationException("The specified keyId does not match the keyId of this provider.");
            }

            return SignAsync(input, cancellationToken);
        }

        /// <inheritdoc/>
        public abstract Task<ReadOnlyMemory<byte>> SignAsync(
            ReadOnlyMemory<byte> input,
            CancellationToken cancellationToken);

        /// <inheritdoc/>
        public abstract Task<VerificationResult> VerifyAsync(
            SignatureParamsComponent signatureParams,
            ReadOnlyMemory<byte> input,
            ReadOnlyMemory<byte> expectedSignature,
            CancellationToken cancellationToken);
    }
}
