using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSign.Http;
using NSign.Signatures;
using System.Threading.Tasks;

namespace NSign.AspNetCore
{
    /// <summary>
    /// Implements an AspNetCore middleware that adds signatures to response messages.
    /// </summary>
    public sealed class ResponseSigningMiddleware : IMiddleware
    {
        /// <summary>
        /// The ILogger to use.
        /// </summary>
        private readonly ILogger<ResponseSigningMiddleware> logger;

        /// <summary>
        /// The <see cref="IOptions{TOptions}"/> of <see cref="HttpFieldOptions"/> that should be used for both signing
        /// messages and verifying message signatures.
        /// </summary>
        private readonly IOptions<HttpFieldOptions> httpFieldOptions;

        /// <summary>
        /// An <see cref="IOptions{TOptions}"/> of <see cref="MessageSigningOptions"></see> object holding the options
        /// to use for response message signing.
        /// </summary>
        private readonly IOptions<MessageSigningOptions> messageSigningOptions;

        /// <summary>
        /// The <see cref="IMessageSigner"/> to use to sign outgoing response messages.
        /// </summary>
        private readonly IMessageSigner signer;

        /// <summary>
        /// Initializes a new instance of ResponseSigningMiddleware.
        /// </summary>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        /// <param name="httpFieldOptions">
        /// The <see cref="IOptions{TOptions}"/> of <see cref="HttpFieldOptions"/> that should be used for both signing
        /// messages and verifying message signatures.
        /// </param>
        /// <param name="messageSigningOptions">
        /// An <see cref="IOptions{TOptions}"/> of <see cref="MessageSigningOptions"></see> object holding the options
        /// to use for response message signing.
        /// </param>
        /// <param name="signer">
        /// The <see cref="IMessageSigner"/> to use to sign outgoing response messages.
        /// </param>
        public ResponseSigningMiddleware(
            ILogger<ResponseSigningMiddleware> logger,
            IOptions<HttpFieldOptions> httpFieldOptions,
            IOptions<MessageSigningOptions> messageSigningOptions,
            IMessageSigner signer)
        {
            this.logger = logger;
            this.httpFieldOptions = httpFieldOptions;
            this.messageSigningOptions = messageSigningOptions;
            this.signer = signer;
        }

        /// <inheritdoc/>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            // Register a handler for the event of the response starting before we're done here.
            RequestResponseMessageContext messageContext = new RequestResponseMessageContext(
                signer, context, httpFieldOptions.Value, messageSigningOptions.Value, logger);
            context.Response.OnStarting(messageContext.OnResponseStartingAsync);

            await next(context);
        }
    }
}
