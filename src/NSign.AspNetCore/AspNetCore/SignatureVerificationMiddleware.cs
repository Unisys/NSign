using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NSign.Signatures;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NSign.AspNetCore
{
    /// <summary>
    /// Implements an AspNetCore middleware that verifies signatures passed in 'signature' headers in combination with
    /// signature input specs from 'signature-input' headers.
    /// </summary>
    public sealed partial class SignatureVerificationMiddleware : IMiddleware
    {
        #region Fields

        /// <summary>
        /// The ILogger to use.
        /// </summary>
        private readonly ILogger<SignatureVerificationMiddleware> logger;

        /// <summary>
        /// The IVerifier to use for signature verification.
        /// </summary>
        private readonly IVerifier signatureVerifier;

        /// <summary>
        /// An IOptions of RequestSignatureVerificationOptions object holding the current options to use for signature
        /// verification.
        /// </summary>
        private readonly IOptions<RequestSignatureVerificationOptions> options;

        #endregion

        /// <summary>
        /// Initializes a new instance of SignatureVerificationMiddleware.
        /// </summary>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        /// <param name="signatureVerifier">
        /// The IVerifier to use for signature verification.
        /// </param>
        /// <param name="options">
        /// An IOptions of RequestSignatureVerificationOptions object holding the current options to use for signature
        /// verification.
        /// </param>
        public SignatureVerificationMiddleware(
            ILogger<SignatureVerificationMiddleware> logger,
            IVerifier signatureVerifier,
            IOptions<RequestSignatureVerificationOptions> options)
        {
            this.logger = logger;
            this.signatureVerifier = signatureVerifier;
            this.options = options;
        }

        /// <inheritdoc/>
        public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
        {
            Context context = CreateContext(httpContext.Request);

            if (!context.HasSignatures)
            {
                httpContext.Response.StatusCode = context.Options.MissingSignatureResponseStatus;
                return;
            }

            // Go through all signatures that have been found, verify them and track results.
            Dictionary<string, VerificationResult> results = new Dictionary<string, VerificationResult>();

            foreach (SignatureContext signatureContext in context.Signatures)
            {
                if (!context.Options.ShouldVerify(signatureContext.Name))
                {
                    logger.LogDebug("Signature '{name}' does not need to be verified.", signatureContext.Name);
                    continue;
                }

                VerificationResult result = await VerifySignaturesAsync(context, signatureContext);
                results.Add(signatureContext.Name, result);
            }

            if (results.Count <= 0)
            {
                logger.LogDebug("No signature was verified.");
                httpContext.Response.StatusCode = context.Options.MissingSignatureResponseStatus;
            }
            else if (results.Any(SignatureInputError))
            {
                logger.LogDebug("Signatures {signatures} had input errors.", String.Join("|", results.Where(SignatureInputError)));
                httpContext.Response.StatusCode = context.Options.SignatureInputErrorResponseStatus;
            }
            else if (results.Any(VerificationFailed))
            {
                logger.LogDebug("Signatures {signatures} failed verification.", String.Join("|", results.Where(VerificationFailed)));
                httpContext.Response.StatusCode = context.Options.VerificationErrorResponseStatus;
            }
            else
            {
                await next(httpContext);
            }
        }

        /// <summary>
        /// Creates a new context for signature verification of the given request.
        /// </summary>
        /// <param name="request">
        /// The HttpRequest for which the context is to be created.
        /// </param>
        /// <returns>
        /// A Context value.
        /// </returns>
        private Context CreateContext(HttpRequest request)
        {
            if (request.Headers.TryGetValue(Constants.Headers.Signature, out StringValues signatureHeaders) &&
                request.Headers.TryGetValue(Constants.Headers.SignatureInput, out StringValues inputHeaders))
            {
                return new Context(signatureHeaders, inputHeaders, request, options.Value);
            }
            else
            {
                return new Context(StringValues.Empty, StringValues.Empty, request, options.Value);
            }
        }

        /// <summary>
        /// Verifies the signature from the given context asynchronously.
        /// </summary>
        /// <param name="context">
        /// A Context value describing context for verification.
        /// </param>
        /// <param name="signatureContext">
        /// A SignatureContext value with details on the signature to verify.
        /// </param>
        /// <returns>
        /// A Task which results in a VerificationResult value describing the outcome of the verification when it completes.
        /// </returns>
        private Task<VerificationResult> VerifySignaturesAsync(Context context, SignatureContext signatureContext)
        {
            if (!signatureContext.HasInputSpec)
            {
                logger.LogDebug("Missing Signature-Input for signature '{name}'.", signatureContext.Name);
                return Task.FromResult(VerificationResult.SignatureInputNotFound);
            }

            string rawInputSpec = signatureContext.InputSpec;
            SignatureInputSpec inputSpec;

            try
            {
                inputSpec = new SignatureInputSpec(signatureContext.Name, rawInputSpec);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to parse input spec '{input}' for signature '{name}'.",
                    rawInputSpec, signatureContext.Name);
                return Task.FromResult(VerificationResult.SignatureInputMalformed);
            }

            if (IsExpired(context, inputSpec))
            {
                logger.LogDebug("Signature '{name}' with input '{input}' has already expired.",
                    signatureContext.Name, signatureContext.InputSpec);
                return Task.FromResult(VerificationResult.SignatureExpired);
            }

            if (!VerifyMandatoryComponentsPresent(context, inputSpec))
            {
                return Task.FromResult(VerificationResult.SignatureInputComponentMissing);
            }

            return VerifySignatureAsync(context, signatureContext, inputSpec);
        }

        /// <summary>
        /// Checks if the signature on the given input spec has expired.
        /// </summary>
        /// <param name="context">
        /// A Context value describing context for verification.
        /// </param>
        /// <param name="inputSpec">
        /// The SignatureInputSpec to check.
        /// </param>
        /// <returns>
        /// True if the signature has expired, or false otherwise.
        /// </returns>
        private bool IsExpired(Context context, SignatureInputSpec inputSpec)
        {
            DateTimeOffset? created = inputSpec.SignatureParameters.Created;
            DateTimeOffset? expires = inputSpec.SignatureParameters.Expires;

            if (context.Options.MaxSignatureAge.HasValue && created.HasValue &&
                created.Value + context.Options.MaxSignatureAge.Value < DateTimeOffset.UtcNow)
            {
                logger.LogDebug("The signature is considered expired because the MaxSignatureAge policy was violated.");
                return true;
            }

            if (!expires.HasValue)
            {
                return false;
            }

            return expires.Value <= DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Verifies that mandatory components and parameters are present in the signature input.
        /// </summary>
        /// <param name="context">
        /// A Context value describing context for verification.
        /// </param>
        /// <param name="inputSpec">
        /// The SignatureInputSpec value to verify.
        /// </param>
        /// <returns>
        /// True if the verification passed, or false otherwise.
        /// </returns>
        private bool VerifyMandatoryComponentsPresent(Context context, SignatureInputSpec inputSpec)
        {
            SignatureParamsComponent sigParams = inputSpec.SignatureParameters;

            if (context.Options.CreatedRequired && !sigParams.Created.HasValue)
            {
                logger.LogDebug("Verification failed ('created' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    inputSpec.Name, sigParams.OriginalValue);
                return false;
            }
            else if (context.Options.ExpiresRequired && !sigParams.Expires.HasValue)
            {
                logger.LogDebug("Verification failed ('expires' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    inputSpec.Name, sigParams.OriginalValue);
                return false;
            }
            else if (context.Options.NonceRequired && !sigParams.Nonce.HasValue)
            {
                logger.LogDebug("Verification failed ('nonce' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    inputSpec.Name, sigParams.OriginalValue);
                return false;
            }
            else if (context.Options.AlgorithmRequired && String.IsNullOrWhiteSpace(sigParams.Algorithm))
            {
                logger.LogDebug("Verification failed ('alg' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    inputSpec.Name, sigParams.OriginalValue);
                return false;
            }
            else if (context.Options.KeyIdRequired && String.IsNullOrWhiteSpace(sigParams.KeyId))
            {
                logger.LogDebug("Verification failed ('keyid' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    inputSpec.Name, sigParams.OriginalValue);
                return false;
            }
            else if (sigParams.Nonce.HasValue && null != context.Options.VerifyNonce &&
                !context.Options.VerifyNonce(inputSpec))
            {
                logger.LogDebug("Nonce verification failed for signature '{name}' with spec '{inputSpec}'.",
                    inputSpec.Name, sigParams.OriginalValue);
                return false;
            }

            ImmutableHashSet<SignatureComponent> presentComponents = sigParams.Components.ToImmutableHashSet();
            bool allComponentsPresent = true;

            foreach (SignatureComponent component in context.Options.RequiredSignatureComponents)
            {
                if (!presentComponents.Contains(component))
                {
                    logger.LogDebug("Required component '{component}' is not present in signature input spec.", component);
                    allComponentsPresent = false;
                }
            }

            return allComponentsPresent;
        }

        /// <summary>
        /// Asynchronously verifies the signature using the current <see cref="IVerifier"/>.
        /// </summary>
        /// <param name="context">
        /// A Context value describing context for verification.
        /// </param>
        /// <param name="signatureContext">
        /// A SignatureContext value with details on the signature to verify.
        /// </param>
        /// <param name="inputSpec">
        /// The SignatureInputSpec value to verify with.
        /// </param>
        /// <returns>
        /// A Task which results in a VerificationResult value describing the outcome of the verification when it completes.
        /// </returns>
        private async Task<VerificationResult> VerifySignatureAsync(
            Context context,
            SignatureContext signatureContext,
            SignatureInputSpec inputSpec)
        {
            byte[] expectedSignature = signatureContext.Signature;
            byte[] input = context.Request.GetSignatureInput(inputSpec);

            try
            {
                VerificationResult result = await signatureVerifier.VerifyAsync(
                    inputSpec.SignatureParameters,
                    input,
                    expectedSignature,
                    CancellationToken.None);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Verification failed with an unhandled exception.");
                throw;
            }
        }

        /// <summary>
        /// Checks if a verification has a failing result.
        /// </summary>
        /// <param name="result">
        /// The KeyValuePair of string and VerificationResult to check.
        /// </param>
        /// <returns>
        /// True if the verification has failed, or false otherwise.
        /// </returns>
        private static bool VerificationFailed(KeyValuePair<string, VerificationResult> result)
        {
            return result.Value != VerificationResult.SuccessfullyVerified;
        }

        /// <summary>
        /// Checks if a verification has a result indicating issues with signature input.
        /// </summary>
        /// <param name="result">
        /// The KeyValuePair of string and VerificationResult to check.
        /// </param>
        /// <returns>
        /// True if there was an issue with signature input, or false otherwise.
        /// </returns>
        private static bool SignatureInputError(KeyValuePair<string, VerificationResult> result)
        {
            return result.Value == VerificationResult.SignatureInputMalformed ||
                result.Value == VerificationResult.SignatureInputNotFound ||
                result.Value == VerificationResult.SignatureInputComponentMissing;
        }
    }
}
