using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSign.Signatures
{
    /// <summary>
    /// Provides a default implementation of <see cref="IMessageVerifier"/> to verify signatures on HTTP messages.
    /// </summary>
    public sealed class DefaultMessageVerifier : IMessageVerifier
    {
        #region Fields

        /// <summary>
        /// The ILogger to use.
        /// </summary>
        private readonly ILogger<DefaultMessageVerifier> logger;

        /// <summary>
        /// The <see cref="IVerifier"/> to use to verify message signatures.
        /// </summary>
        private readonly IVerifier verifier;

        #endregion

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultMessageVerifier"/>.
        /// </summary>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        /// <param name="verifier">
        /// The <see cref="IVerifier"/> to use to verify message signatures.
        /// </param>
        public DefaultMessageVerifier(ILogger<DefaultMessageVerifier> logger, IVerifier verifier)
        {
            this.logger = logger;
            this.verifier = verifier;
        }

        /// <inheritdoc/>
        public async Task VerifyMessageAsync(MessageContext context)
        {
            if (null == context.VerificationOptions)
            {
                throw new InvalidOperationException("The message context does not have verification options.");
            }

            SignatureVerificationOptions options = context.VerificationOptions;

            if (!context.HasSignaturesForVerification)
            {
                await options.OnMissingSignatures(context);
                return;
            }

            // Go through all signatures that have been found, verify them and track results.
            Dictionary<string, VerificationResult> results = new Dictionary<string, VerificationResult>();

            foreach (SignatureContext signatureContext in context.SignaturesForVerification)
            {
                try
                {
                    // Evaluate the SignatureParams now so we get any parser errors here.
                    var _ = signatureContext.SignatureParams;
                }
                catch (Exception ex)
                {
                    string rawInputSpec = signatureContext.InputSpec;

                    logger.LogDebug(ex, "Failed to parse input spec '{input}' for signature '{name}'.",
                        rawInputSpec, signatureContext.Name);
                    results.Add(signatureContext.Name, VerificationResult.SignatureInputMalformed);
                    continue;
                }

                if (!options.ShouldVerify(signatureContext))
                {
                    logger.LogDebug("Signature '{name}' does not need to be verified.", signatureContext.Name);
                    continue;
                }

                VerificationResult result = await VerifySignatureWithPolicyAsync(context, signatureContext);
                logger.LogInformation("Verification of signature '{name}' resulted in '{result}'.",
                    signatureContext.Name, result);
                results.Add(signatureContext.Name, result);
            }

            await NotifyVerificationResultsAsync(context, results);
        }

        /// <summary>
        /// Verifies the signature from the given context asynchronously by looking both at the configured verification
        /// policy as well as the signature itself.
        /// </summary>
        /// <param name="context">
        /// A MessageContext value describing context for verification.
        /// </param>
        /// <param name="signatureContext">
        /// A SignatureContext value with details on the signature to verify.
        /// </param>
        /// <returns>
        /// A Task which results in a VerificationResult value describing the outcome of the verification when it completes.
        /// </returns>
        private Task<VerificationResult> VerifySignatureWithPolicyAsync(
            MessageContext context,
            SignatureContext signatureContext)
        {
            Debug.Assert(null != signatureContext.InputSpec, "The input spec must not be null.");

            SignatureParamsComponent signatureParams = signatureContext.SignatureParams;

            if (IsExpired(context, signatureParams))
            {
                logger.LogDebug("Signature '{name}' with input '{input}' has already expired.",
                    signatureContext.Name, signatureContext.InputSpec);
                return Task.FromResult(VerificationResult.SignatureExpired);
            }

            if (!VerifyMandatoryComponentsPresent(context, signatureContext))
            {
                return Task.FromResult(VerificationResult.SignatureInputComponentMissing);
            }

            return VerifySignatureAsync(context, signatureContext);
        }

        /// <summary>
        /// Notifies the options configured for the given <paramref name="context"/> of the verification results
        /// asynchronously.
        /// </summary>
        /// <param name="context">
        /// A MessageContext value describing context for notification.
        /// </param>
        /// <param name="results">
        /// A Dictionary of string and VerificationResult that holds all the results from verification attempts.
        /// </param>
        /// <returns>
        /// A Task that tracks completion of the operation.
        /// </returns>
        private async Task NotifyVerificationResultsAsync(
            MessageContext context,
            Dictionary<string, VerificationResult> results)
        {
            Debug.Assert(null != context.VerificationOptions, "The verification options of the context must not be null.");
            SignatureVerificationOptions options = context.VerificationOptions!;

            if (results.Count <= 0)
            {
                logger.LogDebug("No signature was verified.");
                await options.OnMissingSignatures(context);
            }
            else if (results.Any(VerificationResultPredicates.SignatureInputError))
            {
                Dictionary<string, VerificationResult> map = results
                    .Where(VerificationResultPredicates.SignatureInputError)
                    .ToDictionary(item => item.Key, item => item.Value);

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Signatures {signatures} had input errors.", String.Join("|", map.Keys));
                }

                await options.OnSignatureInputError(context, map);
            }
            else if (results.Any(VerificationResultPredicates.VerificationFailed))
            {
                Dictionary<string, VerificationResult> map = results
                    .Where(VerificationResultPredicates.VerificationFailed)
                    .ToDictionary(item => item.Key, item => item.Value);

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Signatures {signatures} failed verification.", String.Join("|", map.Keys));
                }

                await options.OnSignatureVerificationFailed(context, map);
            }
            else
            {
                await options.OnSignatureVerificationSucceeded(context);
            }
        }

        /// <summary>
        /// Checks if the signature on the given input spec has expired.
        /// </summary>
        /// <param name="context">
        /// A MessageContext value describing context for verification.
        /// </param>
        /// <param name="signatureParams">
        /// The SignatureParamsComponent to check.
        /// </param>
        /// <returns>
        /// True if the signature has expired, or false otherwise.
        /// </returns>
        private bool IsExpired(MessageContext context, SignatureParamsComponent signatureParams)
        {
            SignatureVerificationOptions options = context.VerificationOptions!;
            DateTimeOffset? created = signatureParams.Created;
            DateTimeOffset? expires = signatureParams.Expires;

            if (options.MaxSignatureAge.HasValue && created.HasValue &&
                created.Value + options.MaxSignatureAge.Value < DateTimeOffset.UtcNow)
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
        /// A MessageContext value describing context for verification.
        /// </param>
        /// <param name="signatureContext">
        /// The SignatureContext to verify.
        /// </param>
        /// <returns>
        /// True if the verification passed, or false otherwise.
        /// </returns>
        private bool VerifyMandatoryComponentsPresent(MessageContext context, SignatureContext signatureContext)
        {
            SignatureVerificationOptions options = context.VerificationOptions!;

            if (options.CreatedRequired && !signatureContext.SignatureParams.Created.HasValue)
            {
                logger.LogDebug(
                    "Verification failed ('created' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    signatureContext.Name, signatureContext.SignatureParams.OriginalValue);
                return false;
            }
            else if (options.ExpiresRequired && !signatureContext.SignatureParams.Expires.HasValue)
            {
                logger.LogDebug(
                    "Verification failed ('expires' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    signatureContext.Name, signatureContext.SignatureParams.OriginalValue);
                return false;
            }
            else if (options.NonceRequired && String.IsNullOrWhiteSpace(signatureContext.SignatureParams.Nonce))
            {
                logger.LogDebug(
                    "Verification failed ('nonce' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    signatureContext.Name, signatureContext.SignatureParams.OriginalValue);
                return false;
            }
            else if (options.AlgorithmRequired && String.IsNullOrWhiteSpace(signatureContext.SignatureParams.Algorithm))
            {
                logger.LogDebug(
                    "Verification failed ('alg' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    signatureContext.Name, signatureContext.SignatureParams.OriginalValue);
                return false;
            }
            else if (options.KeyIdRequired && String.IsNullOrWhiteSpace(signatureContext.SignatureParams.KeyId))
            {
                logger.LogDebug(
                    "Verification failed ('keyid' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    signatureContext.Name, signatureContext.SignatureParams.OriginalValue);
                return false;
            }
            else if (options.TagRequired && String.IsNullOrWhiteSpace(signatureContext.SignatureParams.Tag))
            {
                logger.LogDebug(
                    "Verification failed ('tag' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    signatureContext.Name, signatureContext.SignatureParams.OriginalValue);
                return false;
            }
            else if (null != signatureContext.SignatureParams.Nonce &&
                     null != options.VerifyNonce &&
                     !options.VerifyNonce(signatureContext.SignatureParams))
            {
                logger.LogDebug(
                    "Nonce verification failed for signature '{name}' with spec '{inputSpec}'.",
                    signatureContext.Name, signatureContext.SignatureParams.OriginalValue);
                return false;
            }

            IReadOnlyCollection<SignatureComponent> presentComponents = signatureContext.SignatureParams.Components;
            bool allComponentsPresent = true;

            foreach (SignatureComponent component in options.RequiredSignatureComponents)
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
        /// A MessageContext value describing context for verification.
        /// </param>
        /// <param name="signatureContext">
        /// A SignatureContext value with details on the signature to verify.
        /// </param>
        /// <returns>
        /// A Task which results in a VerificationResult value describing the outcome of the verification when it completes.
        /// </returns>
        private async Task<VerificationResult> VerifySignatureAsync(MessageContext context, SignatureContext signatureContext)
        {
            ReadOnlyMemory<byte> expectedSignature = signatureContext.Signature;
            ReadOnlyMemory<byte> input = context.GetSignatureInput(signatureContext.SignatureParams, out _);

            if (logger.IsEnabled(LogLevel.Debug))
            {
#if NETSTANDARD2_0
                logger.LogDebug("Verifying signature '{sigName}' input spec '{inputSpec}' against signature '{sig}'.",
                    signatureContext.Name, signatureContext.InputSpec, Convert.ToBase64String(signatureContext.Signature.Span.ToArray()));
                logger.LogDebug("Signature input generated from request: '{input}'.", Encoding.UTF8.GetString(input.Span.ToArray()));
#elif NETSTANDARD2_1_OR_GREATER || NET
                logger.LogDebug("Verifying signature '{sigName}' input spec '{inputSpec}' against signature '{sig}'.",
                    signatureContext.Name, signatureContext.InputSpec, Convert.ToBase64String(signatureContext.Signature.Span));
                logger.LogDebug("Signature input generated from request: '{input}'.", Encoding.UTF8.GetString(input.Span));
#endif
            }

            try
            {
                VerificationResult result = await verifier.VerifyAsync(
                    signatureContext.SignatureParams,
                    input,
                    expectedSignature,
                    context.Aborted);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Verification failed with an unhandled exception.");
                throw;
            }
        }
    }
}
