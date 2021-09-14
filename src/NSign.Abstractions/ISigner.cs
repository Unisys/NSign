using NSign.Signatures;
using System.Threading;
using System.Threading.Tasks;

namespace NSign
{
    /// <summary>
    /// An interface to help with signing HTTP messages.
    /// </summary>
    public interface ISigner
    {
        /// <summary>
        /// Let's the signer update the signature parameters (e.g. by specifying the algorith and key used) before the
        /// parameters are finalized for signing.
        /// </summary>
        /// <param name="signatureParams">
        /// The SignatureParamsComponent object holding the signature parameters to update, if necessary.
        /// </param>
        void UpdateSignatureParams(SignatureParamsComponent signatureParams);

        /// <summary>
        /// Signs the given input asynchronously.
        /// </summary>
        /// <param name="input">
        /// An array of byte values for which to create the signature.
        /// </param>
        /// <param name="cancellationToken">
        /// A CancellationToken value that tracks cancellation of the operation.
        /// </param>
        /// <returns>
        /// A Task which results in a byte array representing the signature when it completes.
        /// </returns>
        Task<byte[]> SignAsync(byte[] input, CancellationToken cancellationToken);
    }
}
