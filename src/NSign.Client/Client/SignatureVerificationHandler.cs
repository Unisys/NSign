using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSign.Signatures;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NSign.Client
{
    /// <summary>
    /// Implements a <see cref="DelegatingHandler"/> that verifies signatures passed in 'signature' headers in combination
    /// with signature input specs from 'signature-input' headers for <see cref="HttpClient"/> request/response pipelines.
    /// </summary>
    public sealed class SignatureVerificationHandler : DelegatingHandler
    {
        #region Fields

        /// <summary>
        /// The ILogger to use.
        /// </summary>
        private readonly ILogger<SignatureVerificationHandler> logger;

        /// <summary>
        /// The <see cref="IMessageVerifier"/> to use to verify incoming response messages.
        /// </summary>
        private readonly IMessageVerifier verifier;

        /// <summary>
        /// An IOptions of SignatureVerificationOptions object holding the current options to use for signature verification.
        /// </summary>
        private readonly IOptions<SignatureVerificationOptions> options;

        #endregion

        /// <summary>
        /// Initializes a new instance of SignatureVerificationHandler.
        /// </summary>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        /// <param name="verifier">
        /// The <see cref="IMessageVerifier"/> to use to verify incoming response messages.
        /// </param>
        /// <param name="options">
        /// An IOptions of SignatureVerificationOptions object holding the current options to use for signature verification.
        /// </param>
        public SignatureVerificationHandler(
            ILogger<SignatureVerificationHandler> logger,
            IMessageVerifier verifier,
            IOptions<SignatureVerificationOptions> options)
        {
            this.logger = logger;
            this.verifier = verifier;
            this.options = options;
        }

        #region DelegatingHandler Implementation

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Send the request as usual. We're not interested in modifying it or anything, we merely record it so we can
            // use it later on for verification of signatures in the response, if any.
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            HttpResponseMessageContext context = new HttpResponseMessageContext(
                logger, request, response, cancellationToken, options.Value);

            await verifier.VerifyMessageAsync(context);

            return response;
        }

        #endregion
    }
}
