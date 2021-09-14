using NSign.Signatures;
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
        /// A byte array that represents the raw input used for verification of the signature.
        /// </param>
        /// <param name="expectedSignature">
        /// A byte array that represents the expected signature, i.e. the signature as provided by the signer.
        /// </param>
        /// <param name="cancellationToken">
        /// A CancellationToken value that tracks cancellation of the operation.
        /// </param>
        /// <returns>
        /// A Task which results in a boolean that defines whether or not signature verification succeeded on completion.
        /// </returns>
        Task<bool> VerifyAsync(
            SignatureParamsComponent signatureParams,
            byte[] input,
            byte[] expectedSignature,
            CancellationToken cancellationToken);
    }
}
