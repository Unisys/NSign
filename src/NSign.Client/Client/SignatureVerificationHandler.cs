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
        /// The <see cref="IOptions{TOptions}"/> of <see cref="HttpFieldOptions"/> that should be used for both signing
        /// messages and verifying message signatures.
        /// </summary>
        private readonly IOptions<HttpFieldOptions> httpFieldOptions;

        /// <summary>
        /// An IOptions of SignatureVerificationOptions object holding the current options to use for signature verification.
        /// </summary>
        private readonly IOptions<SignatureVerificationOptions> signatureVerificationOptions;

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
        /// <param name="httpFieldOptions">
        /// The <see cref="IOptions{TOptions}"/> of <see cref="HttpFieldOptions"/> that should be used for both signing
        /// messages and verifying message signatures.
        /// </param>
        /// <param name="signatureVerificationOptions">
        /// An IOptions of SignatureVerificationOptions object holding the current options to use for signature verification.
        /// </param>
        public SignatureVerificationHandler(
            ILogger<SignatureVerificationHandler> logger,
            IMessageVerifier verifier,
            IOptions<HttpFieldOptions> httpFieldOptions,
            IOptions<SignatureVerificationOptions> signatureVerificationOptions)
        {
            this.logger = logger;
            this.verifier = verifier;
            this.httpFieldOptions = httpFieldOptions;
            this.signatureVerificationOptions = signatureVerificationOptions;
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
            HttpResponseMessageContext context = new HttpResponseMessageContext(logger,
                                                                                httpFieldOptions.Value,
                                                                                request,
                                                                                response,
                                                                                cancellationToken,
                                                                                signatureVerificationOptions.Value);

            await verifier.VerifyMessageAsync(context);

            return response;
        }

        #endregion
    }
}
