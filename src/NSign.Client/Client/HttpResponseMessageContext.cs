using Microsoft.Extensions.Logging;
using NSign.Signatures;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace NSign.Client
{
    /// <summary>
    /// Implements a <see cref="HttpResponseMessageContext"/> for signature verification on <see cref="HttpResponseMessage"/>.
    /// </summary>
    internal sealed class HttpResponseMessageContext : HttpRequestMessageContext
    {
        internal HttpResponseMessageContext(
            ILogger logger,
            HttpRequestMessage request,
            HttpResponseMessage response,
            CancellationToken cancellationToken,
            SignatureVerificationOptions verificationOptions)
            : base(logger, request, cancellationToken, null)
        {
            Response = response;
            VerificationOptions = verificationOptions;
        }

        /// <summary>
        /// Gets the <see cref="HttpResponseMessage"/> for signature verification.
        /// </summary>
        public HttpResponseMessage Response { get; }

        #region MessageContext Implementation

        /// <inheritdoc/>
        public override bool HasResponse => true;

        /// <inheritdoc/>
        public override MessageSigningOptions? SigningOptions => null;

        /// <inheritdoc/>
        public override SignatureVerificationOptions? VerificationOptions { get; }

        public override void AddHeader(string headerName, string value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override string? GetDerivedComponentValue(DerivedComponent component)
        {
            return component.ComponentName switch
            {
                Constants.DerivedComponents.Status => ((int)Response.StatusCode).ToString(),

                _ => base.GetDerivedComponentValue(component),
            };
        }

        #endregion

        #region Protected Interface

        /// <inheritdoc/>
        protected override HttpHeaders MessageHeaders => Response.Headers;

        /// <inheritdoc/>
        protected override HttpContent? MessageContent => Response.Content;

        #endregion
    }
}
