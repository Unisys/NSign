using NSign.Signatures;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NSign
{
    /// <summary>
    /// An interface to help with verifying signatures on HTTP messages.
    /// </summary>
    public interface IVerifier
    {
        /// <summary>
        /// Verifies a signature defined by its inputSpec and input against the given expectedSignature asynchronously.
        /// </summary>
        /// <param name="signatureParams">
        /// A SignatureParamsComponent object defining the parameters of the signature.
        /// </param>
        /// <param name="input">
        /// A <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/> that represents the raw input used for verification
        /// of the signature.
        /// </param>
        /// <param name="expectedSignature">
        /// A <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/> that represents the expected signature, i.e. the
        /// signature as provided by the signer.
        /// </param>
        /// <param name="cancellationToken">
        /// A CancellationToken value that tracks cancellation of the operation.
        /// </param>
        /// <returns>
        /// A Task which results in a VerificationResult value that defines the outcome of the verification on completion.
        /// </returns>
        Task<VerificationResult> VerifyAsync(
            SignatureParamsComponent signatureParams,
            ReadOnlyMemory<byte> input,
            ReadOnlyMemory<byte> expectedSignature,
            CancellationToken cancellationToken);
    }
}
