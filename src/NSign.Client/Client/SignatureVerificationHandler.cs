using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSign.Signatures;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NSign.Client
{
    /// <summary>
    /// Implements a <see cref="DelegatingHandler"/> that verifies signatures passed in 'signature' headers in combination
    /// with signature input specs from 'signature-input' headers for <see cref="HttpClient"/> request/response pipelines.
    /// </summary>
    public sealed partial class SignatureVerificationHandler : DelegatingHandler
    {
        #region Fields

        /// <summary>
        /// The ILogger to use.
        /// </summary>
        private readonly ILogger<SignatureVerificationHandler> logger;

        /// <summary>
        /// The <see cref="IVerifier"/> to use to verify incoming response messages.
        /// </summary>
        private readonly IVerifier verifier;

        /// <summary>
        /// An IOptions of SignatureVerificationOptions object holding the current options to use for signature verification.
        /// </summary>
        private readonly IOptions<SignatureVerificationOptions> options;

        #endregion

        /// <summary>
        /// Initializes a new instance of SignatureVerificationHandler.
        /// </summary>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        /// <param name="verifier">
        /// The <see cref="IVerifier"/> to use to verify incoming response messages.
        /// </param>
        /// <param name="options">
        /// An IOptions of SignatureVerificationOptions object holding the current options to use for signature verification.
        /// </param>
        public SignatureVerificationHandler(
            ILogger<SignatureVerificationHandler> logger,
            IVerifier verifier,
            IOptions<SignatureVerificationOptions> options)
        {
            this.logger = logger;
            this.verifier = verifier;
            this.options = options;
        }

        #region DelegatingHandler Implementation

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Send the request as usual. We're not interested in modifying it or anything, we merely record it so we can
            // use it later on for verification of signatures in the response, if any.
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            Context context = CreateContext(request, response);

            if (!context.HasSignatures)
            {
                throw new SignatureMissingException();
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
                throw new SignatureMissingException();
            }
            else if (results.Any(VerificationResultPredicates.SignatureInputError))
            {
                List<string> sigNames = results
                    .Where(VerificationResultPredicates.SignatureInputError)
                    .Select(_ => _.Key)
                    .ToList();
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Signatures [{signatures}] have input errors.", String.Join("|", sigNames));
                }

                throw new SignatureInputException(sigNames);
            }
            else if (results.Any(VerificationResultPredicates.VerificationFailed))
            {
                List<string> sigNames = results
                    .Where(VerificationResultPredicates.VerificationFailed)
                    .Select(_ => _.Key)
                    .ToList();
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Signatures [{signatures}] failed verification.", String.Join("|", sigNames));
                }

                throw new SignatureVerificationFailedException(sigNames);
            }

            return response;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates a new verification context for the given request and response messages.
        /// </summary>
        /// <param name="request">
        /// The HttpRequest that caused the response the context is for.
        /// </param>
        /// <param name="response">
        /// The HttpResponse the context is for.
        /// </param>
        /// <returns>
        /// A new instance of <see cref="Context"/> for signature verification.
        /// </returns>
        private Context CreateContext(HttpRequestMessage request, HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues(Constants.Headers.Signature, out IEnumerable<string> signatureHeaders) &&
                response.Headers.TryGetValues(Constants.Headers.SignatureInput, out IEnumerable<string> inputHeaders))
            {
                return new Context(signatureHeaders, inputHeaders, request, response, options.Value);
            }
            else
            {
                return new Context(Enumerable.Empty<string>(), Enumerable.Empty<string>(), request, response, options.Value);
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
            else if (context.Options.NonceRequired && String.IsNullOrWhiteSpace(sigParams.Nonce))
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
            else if (null != sigParams.Nonce && null != context.Options.VerifyNonce &&
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
            byte[] input = context.GetSignatureInput(inputSpec);
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Verifying signature '{sigName}' input spec '{inputSpec}' against signature '{sig}'.",
                    signatureContext.Name, signatureContext.InputSpec, Convert.ToBase64String(signatureContext.Signature));
                logger.LogDebug("Signature input generated from request/response: '{input}'.", System.Text.Encoding.UTF8.GetString(input));
            }

            try
            {
                VerificationResult result = await verifier.VerifyAsync(
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

        #endregion
    }
}
