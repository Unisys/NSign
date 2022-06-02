using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSign.Signatures;
using System;
using System.Threading.Tasks;

namespace NSign.AspNetCore
{
    /// <summary>
    /// Implements the MessageContext for HTTP response messages in ASP.NET Core. It offers additional functionality
    /// to initiate the signing when the <see cref="HttpResponse.OnStarting(Func{Task})"/> event is raisedin a response.
    /// </summary>
    internal sealed class RequestResponseMessageContext : RequestMessageContext
    {
        private readonly MessageSigningOptions signingOptions;

        /// <summary>
        /// Initializes a new instance of ResponseMessageContext.
        /// </summary>
        /// <param name="signer">
        /// The
        /// </param>
        /// <param name="httpContext">
        /// The <see cref="HttpContext"/> defining the request/response pipeline.
        /// </param>
        /// <param name="signingOptions">
        /// The <see cref="MessageSigningOptions"/> object that describes the details of how messages should be signed.
        /// </param>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        internal RequestResponseMessageContext(
            IMessageSigner signer,
            HttpContext httpContext,
            MessageSigningOptions signingOptions,
            ILogger logger)
            : base(httpContext, null, null, logger)
        {
            Signer = signer;
            this.signingOptions = signingOptions;
        }

        /// <summary>
        /// The <see cref="IMessageSigner"/> to use to sign outgoing response messages.
        /// </summary>
        public IMessageSigner Signer { get; }

        /// <summary>
        /// Handles the 'Starting' event of the response to be signed. This let's us add response headers after the
        /// response status has been defined and before the response body is written.
        /// </summary>
        /// <returns>
        /// A Task which tracks completion of the operation.
        /// </returns>
        public Task OnResponseStartingAsync()
        {
            return Signer.SignMessageAsync(this);
        }

        #region MessageContext Implementation

        /// <inheritdoc/>
        /// <remarks>
        /// This is always null because there's no support for verifying signatures on HTTP response messages in the
        /// ASP.NET Core middleware.
        /// </remarks>
        public override SignatureVerificationOptions? VerificationOptions => null;

        /// <inheritdoc/>
        public override MessageSigningOptions? SigningOptions => signingOptions;

        /// <inheritdoc/>
        public override bool HasResponse => true;

        /// <inheritdoc/>
        public override void AddHeader(string headerName, string value)
        {
            HttpContext.Response.Headers.Add(headerName, value);
        }

        /// <inheritdoc/>
        public override string? GetDerivedComponentValue(DerivedComponent component)
        {
            return component.ComponentName switch
            {
                Constants.DerivedComponents.Status => HttpContext.Response.StatusCode.ToString(),

                _ => base.GetDerivedComponentValue(component),
            };
        }

        #endregion

        /// <inheritdoc/>
        protected override IHeaderDictionary MessageHeaders => HttpContext.Response.Headers;
    }
}
