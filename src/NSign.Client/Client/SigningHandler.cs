using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSign.Http;
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
        /// The <see cref="IOptions{TOptions}"/> of <see cref="HttpFieldOptions"/> that should be used for both signing
        /// messages and verifying message signatures.
        /// </summary>
        private readonly IOptions<HttpFieldOptions> httpFieldOptions;

        /// <summary>
        /// The IOptions of <see cref="MessageSigningOptions"/> to define how to sign requests.
        /// </summary>
        private readonly IOptions<MessageSigningOptions> signingOptions;

        /// <summary>
        /// Initializes a new instance of SigningHandler.
        /// </summary>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        /// <param name="signer">
        /// The <see cref="IMessageSigner"/> to use to sign outgoing request messages.
        /// </param>
        /// <param name="httpFieldOptions">
        /// The <see cref="IOptions{TOptions}"/> of <see cref="HttpFieldOptions"/> that should be used for both signing
        /// messages and verifying message signatures.
        /// </param>
        /// <param name="signingOptions">
        /// The IOptions of <see cref="MessageSigningOptions"/> to define how to sign requests.
        /// </param>
        public SigningHandler(
            ILogger<SigningHandler> logger,
            IMessageSigner signer,
            IOptions<HttpFieldOptions> httpFieldOptions,
            IOptions<MessageSigningOptions> signingOptions)
        {
            this.logger = logger;
            this.signer = signer;
            this.httpFieldOptions = httpFieldOptions;
            this.signingOptions = signingOptions;
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpRequestMessageContext context = new HttpRequestMessageContext(logger,
                                                                              httpFieldOptions.Value,
                                                                              request,
                                                                              cancellationToken,
                                                                              signingOptions.Value);

            await signer.SignMessageAsync(context);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
