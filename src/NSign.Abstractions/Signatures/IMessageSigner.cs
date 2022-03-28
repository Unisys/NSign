using System.Threading.Tasks;

namespace NSign.Signatures
{
    /// <summary>
    /// Defines the contract for signing HTTP messages.
    /// </summary>
    public interface IMessageSigner
    {
        /// <summary>
        /// Asynchronously signs the message in the given <paramref name="context"/>.
        /// </summary>
        /// <param name="context">
        /// A <see cref="MessageContext"/> object that describes the message (or message pipeline) to sign.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> which tracks completion of the operation.
        /// </returns>
        Task SignMessageAsync(MessageContext context);
    }
}
