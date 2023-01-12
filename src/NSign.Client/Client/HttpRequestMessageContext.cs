using Microsoft.Extensions.Logging;
using NSign.Signatures;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Web;

namespace NSign.Client
{
    /// <summary>
    /// Implements the <see cref="MessageContext"/> for <see cref="HttpRequestMessage"/> signing.
    /// </summary>
    internal class HttpRequestMessageContext : MessageContext
    {
        /// <summary>
        /// A lazily initialized <see cref="NameValueCollection"/> object that represents the parsed query parameters
        /// from the request.
        /// </summary>
        private readonly Lazy<NameValueCollection> queryParams;

        /// <summary>
        /// Initializes a new instance of <see cref="HttpRequestMessageContext"/>.
        /// </summary>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        /// <param name="request">
        /// The <see cref="HttpRequestMessage"/> this context is for.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> value that tracks cancellation of the request/response pipeline.
        /// </param>
        /// <param name="signingOptions">
        /// A <see cref="MessageSigningOptions"/> object that defines how to sign the request message, if signing is
        /// needed.
        /// </param>
        internal HttpRequestMessageContext(
            ILogger logger,
            HttpRequestMessage request,
            CancellationToken cancellationToken,
            MessageSigningOptions? signingOptions)
            : base(logger)
        {
            Debug.Assert(null != request, "The request must not be null.");

            Request = request;
            Aborted = cancellationToken;
            SigningOptions = signingOptions;

            queryParams = new Lazy<NameValueCollection>(LoadQueryParams, LazyThreadSafetyMode.None);
        }

        /// <summary>
        /// Gets the <see cref="HttpRequestMessage"/> this context is for.
        /// </summary>
        public HttpRequestMessage Request { get; }

        #region MessageContext Implementation

        /// <inheritdoc/>
        public override bool HasResponse => false;

        /// <inheritdoc/>
        public override sealed CancellationToken Aborted { get; }

        /// <inheritdoc/>
        public override MessageSigningOptions? SigningOptions { get; }

        /// <inheritdoc/>
        public override void AddHeader(string headerName, string value)
        {
            Request.Headers.Add(headerName, value);
        }

        /// <inheritdoc/>
        public override string? GetDerivedComponentValue(DerivedComponent component)
        {
            return Request.GetDerivedComponentValue(component);
        }

        /// <inheritdoc/>
        public override sealed IEnumerable<string> GetHeaderValues(string headerName)
        {
            if (TryGetHeaderValues(MessageHeaders, MessageContent, headerName, out IEnumerable<string> values))
            {
                return values;
            }

            return Array.Empty<string>();
        }

        /// <inheritdoc/>
        public override sealed IEnumerable<string> GetRequestHeaderValues(string headerName)
        {
            if (TryGetHeaderValues(Request.Headers, Request.Content, headerName, out IEnumerable<string> values))
            {
                return values;
            }

            return Array.Empty<string>();
        }

        /// <inheritdoc/>
        public override IEnumerable<string> GetTrailerValues(string fieldName)
        {
            throw new NotSupportedException("Trailers in signatures are not supported for request messages.");
        }

        /// <inheritdoc/>
        public override IEnumerable<string> GetRequestTrailerValues(string fieldName)
        {
            throw new NotSupportedException("Request-based trailers in signatures are not supported.");
        }

        /// <inheritdoc/>
        public override sealed IEnumerable<string> GetQueryParamValues(string paramName)
        {
            string[] values = queryParams.Value.GetValues(paramName);

            if (null != values)
            {
                return values;
            }
            else
            {
                return Array.Empty<string>();
            }
        }

        /// <inheritdoc/>
        public override sealed bool HasHeader(bool bindRequest, string headerName)
        {
            Debug.Assert(false == bindRequest, "Binding to the request message is not supported for this context.");
            return TryGetHeaderValues(MessageHeaders, MessageContent, headerName, out _);
        }

        /// <inheritdoc/>
        public override sealed bool HasQueryParam(string paramName)
        {
            return null != queryParams.Value.GetValues(paramName);
        }

        #endregion

        #region Protected Interface

        /// <summary>
        /// Gets an <see cref="HttpHeaders"/> object representing the headers of the message the context is for.
        /// </summary>
        protected virtual HttpHeaders MessageHeaders => Request.Headers;

        /// <summary>
        /// Gets an <see cref="HttpContent"/> object respresenting the content (if any) of the message the context is for.
        /// </summary>
        protected virtual HttpContent? MessageContent => Request.Content;

        /// <summary>
        /// Tries to get the message header values for the header with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="headers">
        /// The <see cref="HttpHeaders"/> in which to look for the header.
        /// </param>
        /// <param name="content">
        /// The <see cref="HttpContent"/> of the message in which to look for more headers.
        /// </param>
        /// <param name="name">
        /// The name of the header to get the values for.
        /// </param>
        /// <param name="values">
        /// If the header exists, this is updated with the values of the header.
        /// </param>
        /// <returns>
        /// True if the header exists, or false otherwise.
        /// </returns>
        /// <remarks>
        /// This method also checks the headers on the content of the message, provided it is set. It also emulates
        /// synthetic headers like 'content-length' which are in some cases calculated based on the content.
        /// </remarks>
        protected bool TryGetHeaderValues(HttpHeaders headers, HttpContent? content, string name, out IEnumerable<string> values)
        {
            if (headers.TryGetValues(name, out values))
            {
                return true;
            }
            else if (null == content)
            {
                return false;
            }

            // Try the content headers too.
            if (TryGetHeaderValues(content.Headers, null, name, out values))
            {
                return true;
            }
            else
            {
                switch (name)
                {
                    case "content-length":
                        if (content.Headers.ContentLength.HasValue)
                        {
                            values = new string[] { content.Headers.ContentLength.Value.ToString(), };
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                }
            }

            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads the request message's query parameters into a <see cref="NameValueCollection"/> and returns it. If
        /// there are no query parameters, an empty collection is returned.
        /// </summary>
        /// <returns>
        /// An instance of <see cref="NameValueCollection"/>.
        /// </returns>
        private NameValueCollection LoadQueryParams()
        {
            if (String.IsNullOrWhiteSpace(Request.RequestUri.Query))
            {
                return new NameValueCollection();
            }

            return HttpUtility.ParseQueryString(Request.RequestUri.Query);
        }

        #endregion
    }
}
