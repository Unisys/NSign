using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSign.Signatures;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NSign.Client
{
    /// <summary>
    /// Implements a <see cref="DelegatingHandler"/> that adds <c>Signature-Input</c> and <c>Signature</c> headers with
    /// signatures as per configured options to outgoing HTTP request messages.
    /// </summary>
    public sealed class SigningHandler : DelegatingHandler
    {
        /// <summary>
        /// The ILogger to use.
        /// </summary>
        private readonly ILogger<SigningHandler> logger;

        /// <summary>
        /// The <see cref="IMessageSigner"/> to use to sign outgoing request messages.
        /// </summary>
        private readonly IMessageSigner signer;

        /// <summary>
        /// The IOptions of <see cref="MessageSigningOptions"/> to define how to sign requests.
        /// </summary>
        private readonly IOptions<MessageSigningOptions> options;

        /// <summary>
        /// Initializes a new instance of SigningHandler.
        /// </summary>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        /// <param name="signer">
        /// The <see cref="IMessageSigner"/> to use to sign outgoing request messages.
        /// </param>
        /// <param name="options">
        /// The IOptions of <see cref="MessageSigningOptions"/> to define how to sign requests.
        /// </param>
        public SigningHandler(
            ILogger<SigningHandler> logger,
            IMessageSigner signer,
            IOptions<MessageSigningOptions> options)
        {
            this.logger = logger;
            this.signer = signer;
            this.options = options;
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpRequestMessageContext context = new HttpRequestMessageContext(
                logger, request, cancellationToken, options.Value);

            await signer.SignMessageAsync(context);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
