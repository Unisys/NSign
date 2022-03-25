using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSign.Signatures;
using System.Threading.Tasks;

namespace NSign.AspNetCore
{
    /// <summary>
    /// Implements an AspNetCore middleware that verifies signatures on request messages passed in 'signature' headers
    /// in combination with signature input specs from 'signature-input' headers.
    /// </summary>
    public sealed partial class SignatureVerificationMiddleware : IMiddleware
    {
        #region Fields

        /// <summary>
        /// The ILogger to use.
        /// </summary>
        private readonly ILogger<SignatureVerificationMiddleware> logger;

        /// <summary>
        /// The <see cref="IMessageVerifier"/> to use for request message signature verification.
        /// </summary>
        private readonly IMessageVerifier verifier;

        /// <summary>
        /// An <see cref="IOptions{TOptions}"/> of <see cref="RequestSignatureVerificationOptions"></see> object holding
        /// the options to use for signature verification.
        /// </summary>
        private readonly IOptions<RequestSignatureVerificationOptions> options;

        #endregion

        /// <summary>
        /// Initializes a new instance of SignatureVerificationMiddleware.
        /// </summary>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        /// <param name="verifier">
        /// The <see cref="IMessageVerifier"/> to use for request message signature verification.
        /// </param>
        /// <param name="options">
        /// An <see cref="IOptions{TOptions}"/> of <see cref="RequestSignatureVerificationOptions"></see> object holding
        /// the options to use for signature verification.
        /// </param>
        public SignatureVerificationMiddleware(
            ILogger<SignatureVerificationMiddleware> logger,
            IMessageVerifier verifier,
            IOptions<RequestSignatureVerificationOptions> options)
        {
            this.logger = logger;
            this.verifier = verifier;
            this.options = options;
        }

        /// <inheritdoc/>
        public Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
        {
            RequestMessageContext messageContext = new RequestMessageContext(httpContext, options.Value, next, logger);

            // The middleware is relying on the verification options to use the next delegate when verification passed,
            // so it is not invoked here.
            return verifier.VerifyMessageAsync(messageContext);
        }
    }
}
