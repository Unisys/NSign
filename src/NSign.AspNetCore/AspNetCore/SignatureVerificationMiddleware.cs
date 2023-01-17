﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSign.Http;
using NSign.Signatures;
using System.Threading.Tasks;

namespace NSign.AspNetCore
{
    /// <summary>
    /// Implements an AspNetCore middleware that verifies signatures on request messages passed in 'signature' headers
    /// in combination with signature input specs from 'signature-input' headers.
    /// </summary>
    public sealed class SignatureVerificationMiddleware : IMiddleware
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
        /// The <see cref="IOptions{TOptions}"/> of <see cref="HttpFieldOptions"/> that should be used for both signing
        /// messages and verifying message signatures.
        /// </summary>
        private readonly IOptions<HttpFieldOptions> httpFieldOptions;

        /// <summary>
        /// An <see cref="IOptions{TOptions}"/> of <see cref="RequestSignatureVerificationOptions"></see> object holding
        /// the options to use for signature verification.
        /// </summary>
        private readonly IOptions<RequestSignatureVerificationOptions> signatureVerificationOptions;

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
        /// <param name="httpFieldOptions">
        /// The <see cref="IOptions{TOptions}"/> of <see cref="HttpFieldOptions"/> that should be used for both signing
        /// messages and verifying message signatures.
        /// </param>
        /// <param name="signatureVerificationOptions">
        /// An <see cref="IOptions{TOptions}"/> of <see cref="RequestSignatureVerificationOptions"></see> object holding
        /// the options to use for signature verification.
        /// </param>
        public SignatureVerificationMiddleware(
            ILogger<SignatureVerificationMiddleware> logger,
            IMessageVerifier verifier,
            IOptions<HttpFieldOptions> httpFieldOptions,
            IOptions<RequestSignatureVerificationOptions> signatureVerificationOptions)
        {
            this.logger = logger;
            this.verifier = verifier;
            this.httpFieldOptions = httpFieldOptions;
            this.signatureVerificationOptions = signatureVerificationOptions;
        }

        /// <inheritdoc/>
        public Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
        {
            RequestMessageContext messageContext = new RequestMessageContext(httpContext,
                                                                             httpFieldOptions.Value,
                                                                             signatureVerificationOptions.Value,
                                                                             next,
                                                                             logger);

            // The middleware is relying on the verification options to use the next delegate when verification passed,
            // so it is not invoked here.
            return verifier.VerifyMessageAsync(messageContext);
        }
    }
}
