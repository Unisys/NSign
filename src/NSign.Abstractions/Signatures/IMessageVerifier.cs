using System.Threading.Tasks;

namespace NSign.Signatures
{
    /// <summary>
    /// Defines the contract for verifying signatures on HTTP messages.
    /// </summary>
    public interface IMessageVerifier
    {
        /// <summary>
        /// Asynchronously verifies signatures on the message in the given <paramref name="context"/>.
        /// </summary>
        /// <param name="context">
        /// A <see cref="MessageContext"/> object that describes the message (or message pipeline) to verify
        /// signatures on.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> which tracks completion of the operation.
        /// </returns>
        Task VerifyMessageAsync(MessageContext context);
    }
}
