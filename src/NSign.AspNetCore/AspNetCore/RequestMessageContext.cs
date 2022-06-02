﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NSign.Signatures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace NSign.AspNetCore
{
    /// <summary>
    /// Implements the <see cref="MessageContext"/> abstract class for a single request/response pipeline.
    /// </summary>
    internal class RequestMessageContext : MessageContext
    {
        /// <summary>
        /// The <see cref="SignatureVerificationOptions"/> object that describes the details of signature verification.
        /// Can be null only when this context is not used for signature verification.
        /// </summary>
        private readonly SignatureVerificationOptions? verificationOptions;

        /// <summary>
        /// Initializes a new instance of MessageContext.
        /// </summary>
        /// <param name="httpContext">
        /// The <see cref="HttpContext"/> defining the request/response pipeline.
        /// </param>
        /// <param name="verificationOptions">
        /// The <see cref="SignatureVerificationOptions"/> object that describes the details of signature verification.
        /// Can be null only when this context is not used for signature verification.
        /// </param>
        /// <param name="nextMiddleware">
        /// A <see cref="RequestDelegate"/> that represents the next middleware to call, if any.
        /// </param>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        internal RequestMessageContext(
            HttpContext httpContext,
            SignatureVerificationOptions? verificationOptions,
            RequestDelegate? nextMiddleware,
            ILogger logger)
            : base(logger)
        {
            Debug.Assert(null != httpContext, "The httpContext must not be null.");

            HttpContext = httpContext;
            this.verificationOptions = verificationOptions;
            NextMiddleware = nextMiddleware;
        }

        /// <summary>
        /// The <see cref="HttpContext"/> defining the request/response pipeline for which to sign the response.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// The <see cref="RequestDelegate"/> that represents the next middleware to call, if any.
        /// </summary>
        public RequestDelegate? NextMiddleware { get; }

        #region MessageContext Implementation

        /// <inheritdoc/>
        public override SignatureVerificationOptions? VerificationOptions => verificationOptions;

        /// <inheritdoc/>
        public override bool HasResponse => false;

        /// <inheritdoc/>
        public override sealed CancellationToken Aborted => HttpContext.RequestAborted;

        /// <inheritdoc/>
        public override void AddHeader(string headerName, string value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override string? GetDerivedComponentValue(DerivedComponent component)
        {
            return HttpContext.Request.GetDerivedComponentValue(component);
        }

        /// <inheritdoc/>
        public override sealed IEnumerable<string> GetHeaderValues(string headerName)
        {
            if (TryGetHeaderValues(MessageHeaders, headerName, out StringValues values))
            {
                return values;
            }

            return Enumerable.Empty<string>();

        }

        /// <inheritdoc/>
        public override sealed IEnumerable<string> GetRequestHeaderValues(string headerName)
        {
            if (TryGetHeaderValues(HttpContext.Request.Headers, headerName, out StringValues values))
            {
                return values;
            }

            return Enumerable.Empty<string>();
        }

        /// <inheritdoc/>
        public override sealed IEnumerable<string> GetQueryParamValues(string paramName)
        {
            if (HttpContext.Request.Query.TryGetValue(paramName, out StringValues values))
            {
                return values;
            }

            return Enumerable.Empty<string>();
        }

        /// <inheritdoc/>
        public override sealed bool HasHeader(bool bindRequest, string headerName)
        {
            Debug.Assert(false == bindRequest, "Binding to the request message is not supported for this context.");
            return TryGetHeaderValues(MessageHeaders, headerName, out _);
        }

        /// <inheritdoc/>
        public override sealed bool HasQueryParam(string paramName)
        {
            return HttpContext.Request.Query.ContainsKey(paramName);
        }

        #endregion

        #region Protected Interface

        /// <summary>
        /// Gets an <see cref="IHeaderDictionary"/> representing the headers of the message the context is for.
        /// </summary>
        protected virtual IHeaderDictionary MessageHeaders => HttpContext.Request.Headers;

        /// <summary>
        /// Tries to get the message header values for the header with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="headers">
        /// The <see cref="IHeaderDictionary"/> in which to look for the header.
        /// </param>
        /// <param name="name">
        /// The name of the header to get the values for.
        /// </param>
        /// <param name="values">
        /// If the header exists, is updated with the values of the header.
        /// </param>
        /// <returns>
        /// True if the header exists, or false otherwise.
        /// </returns>
        /// <remarks>
        /// This method also emulates synthetic headers like 'content-length' which are in some cases calculated based
        /// on the content.
        /// </remarks>
        protected static bool TryGetHeaderValues(IHeaderDictionary headers, string name, out StringValues values)
        {
            if (headers.TryGetValue(name, out values))
            {
                return true;
            }

            switch (name)
            {
                case "content-length":
                    if (headers.ContentLength.HasValue)
                    {
                        values = new StringValues(headers.ContentLength.Value.ToString());
                        return true;
                    }
                    break;
            }

            return false;
        }

        #endregion
    }
}
