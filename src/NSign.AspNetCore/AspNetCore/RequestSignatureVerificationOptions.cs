using NSign.Signatures;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NSign.AspNetCore
{
    /// <summary>
    /// Options class to control signature validation on HTTP request messages through the
    /// <see cref="SignatureVerificationMiddleware"/>.
    /// </summary>
    public sealed class RequestSignatureVerificationOptions : SignatureVerificationOptions
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RequestSignatureVerificationOptions"/>.
        /// </summary>
        public RequestSignatureVerificationOptions()
        {
            OnMissingSignatures = ServerOnMissingSignatures;
            OnSignatureInputError = ServerOnSignatureInputError;
            OnSignatureVerificationFailed = ServerOnSignatureVerificationFailed;
            OnSignatureVerificationSucceeded = ServerOnSignatureVerificationSucceeded;
        }

        /// <summary>
        /// Gets or sets the status code to use when signature verification failed. Defaults to <c>401 (Unauthenticated)</c>
        /// </summary>
        public int VerificationErrorResponseStatus { get; set; } = 401;

        /// <summary>
        /// Gets or sets the status code to use when no signature to be verified was found on the request. Defaults to
        /// <c>400 (Bad Request)</c>.
        /// </summary>
        public int MissingSignatureResponseStatus { get; set; } = 400;

        /// <summary>
        /// Gest or sets the status code to use when errors/issues with signature inputs were encountered. Defaults to
        /// <c>400 (Bad Request)</c>.
        /// </summary>
        public int SignatureInputErrorResponseStatus { get; set; } = 400;

        #region Private Methods

        /// <summary>
        /// Provides the default implementation for the OnMissingSignatures handler. Always sets the response message's
        /// status code to the value configured with <see cref="MissingSignatureResponseStatus"/>.
        /// </summary>
        /// <param name="context">
        /// The <see cref="MessageContext"/> that defines the context which is missing signatures.
        /// </param>
        private Task ServerOnMissingSignatures(MessageContext context)
        {
            RequestMessageContext messageContext = (RequestMessageContext)context;

            messageContext.HttpContext.Response.StatusCode = MissingSignatureResponseStatus;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Provides the default implementation for the OnSignatureInputError handler. Always sets the response message's
        /// status code to the value configured with <see cref="SignatureInputErrorResponseStatus"/>.
        /// </summary>
        /// <param name="context">
        /// The <see cref="MessageContext"/> that defines the context which has signature input errors.
        /// </param>
        /// <param name="results">
        /// A <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="String"/> and <see cref="VerificationResult"/>
        /// that maps the names of the signatures with input errors to <see cref="VerificationResult"/> values.
        /// </param>
        private Task ServerOnSignatureInputError(
            MessageContext context,
            IReadOnlyDictionary<string, VerificationResult> results)
        {
            RequestMessageContext messageContext = (RequestMessageContext)context;

            messageContext.HttpContext.Response.StatusCode = SignatureInputErrorResponseStatus;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Provides the default implementation for the OnSignatureVerificationFailed handler. Always sets the response
        /// message's status code to the value configured with <see cref="VerificationErrorResponseStatus"/>.
        /// </summary>
        /// <param name="context">
        /// The <see cref="MessageContext"/> that defines the context which has signature verification failures.
        /// </param>
        /// <param name="verificationResults">
        /// A <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="String"/> and <see cref="VerificationResult"/>
        /// that maps the names of the signatures with verification failures to <see cref="VerificationResult"/> values.
        /// </param>
        private Task ServerOnSignatureVerificationFailed(
            MessageContext context,
            IReadOnlyDictionary<string, VerificationResult> verificationResults)
        {
            RequestMessageContext messageContext = (RequestMessageContext)context;

            messageContext.HttpContext.Response.StatusCode = VerificationErrorResponseStatus;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Provides the default implementation for the OnSignatureVerificationSucceeded handler. Always invokes the
        /// next middle
        /// </summary>
        /// <param name="context">
        /// The <see cref="MessageContext"/> that defines the context for which signature verification succeeded.
        /// </param>
        /// <returns>
        /// A Task that tracks completion of the operation.
        /// </returns>
        private Task ServerOnSignatureVerificationSucceeded(MessageContext context)
        {
            RequestMessageContext messageContext = (RequestMessageContext)context;

            return messageContext.NextMiddleware!(messageContext.HttpContext);
        }

        #endregion
    }
}
