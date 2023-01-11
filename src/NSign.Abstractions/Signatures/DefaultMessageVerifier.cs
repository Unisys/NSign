using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
                if (!options.ShouldVerify(signatureContext.Name))
                {
                    logger.LogDebug("Signature '{name}' does not need to be verified.", signatureContext.Name);
                    continue;
                }

                VerificationResult result = await VerifySignaturesAsync(context, signatureContext);
                logger.LogInformation("Verification of signature '{name}' resulted in '{result}'.",
                    signatureContext.Name, result);
                results.Add(signatureContext.Name, result);
            }

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
        /// Verifies the signature from the given context asynchronously.
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
        private Task<VerificationResult> VerifySignaturesAsync(MessageContext context, SignatureContext signatureContext)
        {
            if (null == signatureContext.InputSpec)
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
        /// A MessageContext value describing context for verification.
        /// </param>
        /// <param name="inputSpec">
        /// The SignatureInputSpec to check.
        /// </param>
        /// <returns>
        /// True if the signature has expired, or false otherwise.
        /// </returns>
        private bool IsExpired(MessageContext context, SignatureInputSpec inputSpec)
        {
            SignatureVerificationOptions options = context.VerificationOptions!;
            DateTimeOffset? created = inputSpec.SignatureParameters.Created;
            DateTimeOffset? expires = inputSpec.SignatureParameters.Expires;

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
        /// <param name="inputSpec">
        /// The SignatureInputSpec value to verify.
        /// </param>
        /// <returns>
        /// True if the verification passed, or false otherwise.
        /// </returns>
        private bool VerifyMandatoryComponentsPresent(MessageContext context, SignatureInputSpec inputSpec)
        {
            SignatureVerificationOptions options = context.VerificationOptions!;
            SignatureParamsComponent sigParams = inputSpec.SignatureParameters;

            if (options.CreatedRequired && !sigParams.Created.HasValue)
            {
                logger.LogDebug(
                    "Verification failed ('created' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    inputSpec.Name, sigParams.OriginalValue);
                return false;
            }
            else if (options.ExpiresRequired && !sigParams.Expires.HasValue)
            {
                logger.LogDebug(
                    "Verification failed ('expires' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    inputSpec.Name, sigParams.OriginalValue);
                return false;
            }
            else if (options.NonceRequired && String.IsNullOrWhiteSpace(sigParams.Nonce))
            {
                logger.LogDebug(
                    "Verification failed ('nonce' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    inputSpec.Name, sigParams.OriginalValue);
                return false;
            }
            else if (options.AlgorithmRequired && String.IsNullOrWhiteSpace(sigParams.Algorithm))
            {
                logger.LogDebug(
                    "Verification failed ('alg' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    inputSpec.Name, sigParams.OriginalValue);
                return false;
            }
            else if (options.KeyIdRequired && String.IsNullOrWhiteSpace(sigParams.KeyId))
            {
                logger.LogDebug(
                    "Verification failed ('keyid' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    inputSpec.Name, sigParams.OriginalValue);
                return false;
            }
            else if (options.TagRequired && String.IsNullOrWhiteSpace(sigParams.Tag))
            {
                logger.LogDebug(
                    "Verification failed ('tag' parameter missing) for signature '{name}' with spec '{inputSpec}'.",
                    inputSpec.Name, sigParams.OriginalValue);
                return false;
            }
            else if (null != sigParams.Nonce && null != options.VerifyNonce && !options.VerifyNonce(inputSpec))
            {
                logger.LogDebug(
                    "Nonce verification failed for signature '{name}' with spec '{inputSpec}'.",
                    inputSpec.Name, sigParams.OriginalValue);
                return false;
            }

            ImmutableHashSet<SignatureComponent> presentComponents = sigParams.Components.ToImmutableHashSet();
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
        /// <param name="inputSpec">
        /// The SignatureInputSpec value to verify with.
        /// </param>
        /// <returns>
        /// A Task which results in a VerificationResult value describing the outcome of the verification when it completes.
        /// </returns>
        private async Task<VerificationResult> VerifySignatureAsync(
            MessageContext context,
            SignatureContext signatureContext,
            SignatureInputSpec inputSpec)
        {
            ReadOnlyMemory<byte> expectedSignature = signatureContext.Signature;
            ReadOnlyMemory<byte> input = context.GetSignatureInput(inputSpec, out _);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Verifying signature '{sigName}' input spec '{inputSpec}' against signature '{sig}'.",
                    signatureContext.Name, signatureContext.InputSpec, Convert.ToBase64String(signatureContext.Signature.Span));
                logger.LogDebug("Signature input generated from request: '{input}'.", Encoding.UTF8.GetString(input.Span));
            }

            try
            {
                VerificationResult result = await verifier.VerifyAsync(
                    inputSpec.SignatureParameters,
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
