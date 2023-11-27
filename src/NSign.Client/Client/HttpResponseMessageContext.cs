using Microsoft.Extensions.Logging;
using NSign.Http;
using NSign.Signatures;
using System;
using System.Collections.Generic;
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
        /// <summary>
        /// Initializes a new instance of <see cref="HttpResponseMessageContext"/>.
        /// </summary>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        /// <param name="httpFieldOptions">
        /// The <see cref="HttpFieldOptions"/> that should be used for both signing messages and verifying message
        /// signatures.
        /// </param>
        /// <param name="request">
        /// The <see cref="HttpRequestMessage"/> this context is for.
        /// </param>
        /// <param name="response">
        /// The <see cref="HttpResponseMessage"/> this context is for.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> value that tracks cancellation of the request/response pipeline.
        /// </param>
        /// <param name="verificationOptions">
        /// The <see cref="SignatureVerificationOptions"/> to use for signature verification.
        /// </param>
        internal HttpResponseMessageContext(
            ILogger logger,
            HttpFieldOptions httpFieldOptions,
            HttpRequestMessage request,
            HttpResponseMessage response,
            CancellationToken cancellationToken,
            SignatureVerificationOptions verificationOptions)
            : base(logger, httpFieldOptions, request, cancellationToken, null)
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

        /// <inheritdoc/>
        public override IEnumerable<string> GetTrailerValues(string fieldName)
        {
#if NETSTANDARD2_0
            throw new NotSupportedException("Trailers are not supported with .netstandard 2.0. Please update to a more modern version.");
#elif NETSTANDARD2_1_OR_GREATER || NET
            if (Response.TrailingHeaders.TryGetValues(fieldName, out IEnumerable<string>? values) &&
                null != values)
            {
                return values;
            }

            return Array.Empty<string>();
#endif
        }

        /// <inheritdoc/>
        public override bool HasTrailer(bool bindRequest, string fieldName)
        {
#if NETSTANDARD2_0
            throw new NotSupportedException("Trailers are not supported with .netstandard 2.0. Please update to a more modern version.");
#elif NETSTANDARD2_1_OR_GREATER || NET
            if (bindRequest)
            {
                // The HttpRequestMessage for HttpClient does not support trailers.
                return false;
            }

            return Response.TrailingHeaders.TryGetValues(fieldName, out _);
#endif
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
