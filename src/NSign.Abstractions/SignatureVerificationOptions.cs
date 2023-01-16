using NSign.Signatures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NSign
{
    /// <summary>
    /// Options class to control signature validation.
    /// </summary>
    public class SignatureVerificationOptions
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SignatureVerificationOptions"/>.
        /// </summary>
        public SignatureVerificationOptions()
        {
            ShouldVerify = DefaultShouldVerify;

            OnMissingSignatures = DefaultOnMissingSignatures;
            OnSignatureInputError = DefaultOnSignatureInputError;
            OnSignatureVerificationFailed = DefaultOnSignatureVerificationFailed;
            OnSignatureVerificationSucceeded = DefaultOnSignatureVerificationSucceeded;
        }

        /// <summary>
        /// Gets an <see cref="ICollection{T}"/> of string values representing the names of signatures to verify.
        /// </summary>
        [Obsolete(
            "Signature selection/verification by name is no longer recommended and support for it will be cut in " +
            "future versions. Use selection/verification by tag instead.",
            error: false)]
        public ICollection<string> SignaturesToVerify { get; } = new Collection<string>();

        /// <summary>
        /// Gets an <see cref="ICollection{T}"/> of string values representing the tags of signatures to verify.
        /// </summary>
        public ICollection<string> TagsToVerify { get; } = new Collection<string>();

        /// <summary>
        /// Gets or sets a function which takes a <see cref="SignatureContext"/> representing a signature as input and
        /// returns a boolean indicating whether the signature should be verified.
        /// </summary>
        /// <remarks>
        /// This defaults to return true for all signatures registered in the SignaturesToVerify collection.
        /// </remarks>
        public Func<SignatureContext, bool> ShouldVerify { get; set; }

        /// <summary>
        /// The callback to be invoked when signatures for verification are missing.
        /// </summary>
        public Func<MessageContext, Task> OnMissingSignatures { get; set; }

        /// <summary>
        /// The callback to be invoked when signatures have input errors.
        /// </summary>
        /// <remarks>
        /// The <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="String"/> and <see cref="VerificationResult"/>
        /// that maps the names of the signatures with input errors to <see cref="VerificationResult"/> values.
        /// </remarks>
        public Func<MessageContext, IReadOnlyDictionary<string, VerificationResult>, Task> OnSignatureInputError { get; set; }

        /// <summary>
        /// The callback to be invoked when signature verification has failed.
        /// </summary>
        /// <remarks>
        /// The <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="String"/> and <see cref="VerificationResult"/>
        /// that maps the names of the signatures with verification failures to <see cref="VerificationResult"/> values.
        /// </remarks>
        public Func<MessageContext, IReadOnlyDictionary<string, VerificationResult>, Task> OnSignatureVerificationFailed { get; set; }

        /// <summary>
        /// The callback to be invoked when signature verification has succeeded.
        /// </summary>
        public Func<MessageContext, Task> OnSignatureVerificationSucceeded { get; set; }

        /// <summary>
        /// Gets or sets a function which takes a SignatureParamsComponent object as input, validates the nonce from it
        /// and returns a boolean indicating whether or not the nonce was accepted. If unset, does not verify the nonce.
        /// </summary>
        public Func<SignatureParamsComponent, bool>? VerifyNonce { get; set; }

        /// <summary>
        /// Gets an ICollection of SignatureComponent objects representing all the components a signature must present in
        /// order to be accepted.
        /// </summary>
        public ICollection<SignatureComponent> RequiredSignatureComponents { get; } = new Collection<SignatureComponent>();

        /// <summary>
        /// Gets or sets a flag which indicates whether or not the 'created' parameter of signature input must be set in
        /// order for the signature to be accepted. Defaults to <c>true</c>.
        /// </summary>
        public bool CreatedRequired { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag which indicates whether or not the 'expires' parameter of signature input must be set in
        /// order for the signature to be accepted. Defaults to <c>true</c>.
        /// </summary>
        public bool ExpiresRequired { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag which indicates whether or not the 'nonce' parameter of signature input must be set in
        /// order for the signature to be accepted. Defaults to <c>false</c>.
        /// </summary>
        public bool NonceRequired { get; set; } = false;

        /// <summary>
        /// Gets or sets a flag which indicates whether or not the 'alg' parameter of signature input must be set in
        /// order for the signature to be accepted. Defaults to <c>false</c>.
        /// </summary>
        public bool AlgorithmRequired { get; set; } = false;

        /// <summary>
        /// Gets or sets a flag which indicates whether or not the 'keyid' parameter of signature input must be set in
        /// order for the signature to be accepted. Defaults to <c>false</c>.
        /// </summary>
        public bool KeyIdRequired { get; set; } = false;

        /// <summary>
        /// Gets or sets a flag which indicates whether the 'tag' parameter of signature input must be set in order for
        /// the signature to be accepted. Defaults to <c>false</c>.
        /// </summary>
        public bool TagRequired { get; set; } = false;

        /// <summary>
        /// Gets or sets a nullable TimeSpan value that indicates the maximum age of a signature (based on the 'created'
        /// parameter) in order for the signature to accepted. If a value is set, it is applied even if <c>ExpiresRequired == true</c>
        /// and an expiration is set on the signature, provided that the 'created' parameter is set too.
        /// Defaults to 5 minutes.
        /// </summary>
        public TimeSpan? MaxSignatureAge { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Provides the default implementation for the <see cref="ShouldVerify"/> predicate:
        /// 
        /// <list type="bullet">
        ///     <item>signatures with a tag matching a tag in <see cref="TagsToVerify"/> and</item>
        ///     <item>signatures with a name matching a name in <see cref="SignaturesToVerify"/></item>
        /// </list>
        /// 
        /// should be verified.
        /// </summary>
        /// <param name="context">
        /// The <see cref="SignatureContext"/> representing the signature the check.
        /// </param>
        /// <returns>
        /// True if the given signature should be verified, or false otherwise.
        /// </returns>
        public bool DefaultShouldVerify(SignatureContext context)
        {
#pragma warning disable CS0618 // Use of SignaturesToVerify will be removed in the future.
            return
                (null != context.SignatureParams.Tag && TagsToVerify.Contains(context.SignatureParams.Tag)) ||
                SignaturesToVerify.Contains(context.Name);
#pragma warning restore CS0618 // Use of SignaturesToVerify will be removed in the future.
        }

        /// <summary>
        /// Provides the default implementation for the <see cref="OnMissingSignatures"/> handler. Always throws a
        /// <see cref="SignatureMissingException"/>.
        /// </summary>
        /// <param name="context">
        /// The <see cref="MessageContext"/> that defines the context which is missing signatures.
        /// </param>
        /// <returns>
        /// A Task that tracks completion of the operation.
        /// </returns>
        /// <exception cref="SignatureMissingException">
        /// Always thrown.
        /// </exception>
        public Task DefaultOnMissingSignatures(MessageContext context)
        {
            throw new SignatureMissingException();
        }

        /// <summary>
        /// Provides the default implementation for the <see cref="OnSignatureInputError"/> handler. Always throws a
        /// <see cref="SignatureInputException"/>.
        /// </summary>
        /// <param name="context">
        /// The <see cref="MessageContext"/> that defines the context which has signature input errors.
        /// </param>
        /// <param name="results">
        /// A <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="String"/> and <see cref="VerificationResult"/>
        /// that maps the names of the signatures with input errors to <see cref="VerificationResult"/> values.
        /// </param>
        /// <returns>
        /// A Task that tracks completion of the operation.
        /// </returns>
        /// <exception cref="SignatureInputException">
        /// Always thrown.
        /// </exception>
        public Task DefaultOnSignatureInputError(
            MessageContext context,
            IReadOnlyDictionary<string, VerificationResult> results)
        {
            throw new SignatureInputException(results.Keys);
        }

        /// <summary>
        /// Provides the default implementation for the <see cref="OnSignatureVerificationFailed"/> handler. Always
        /// throws a <see cref="SignatureVerificationFailedException"/>.
        /// </summary>
        /// <param name="context">
        /// The <see cref="MessageContext"/> that defines the context which has signature verification failures.
        /// </param>
        /// <param name="verificationResults">
        /// A <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="String"/> and <see cref="VerificationResult"/>
        /// that maps the names of the signatures with verification failures to <see cref="VerificationResult"/> values.
        /// </param>
        /// <returns>
        /// A Task that tracks completion of the operation.
        /// </returns>
        /// <exception cref="SignatureVerificationFailedException">
        /// Always thrown.
        /// </exception>
        public Task DefaultOnSignatureVerificationFailed(
            MessageContext context,
            IReadOnlyDictionary<string, VerificationResult> verificationResults)
        {
            throw new SignatureVerificationFailedException(verificationResults.Keys);
        }

        /// <summary>
        /// Provides the default implementation for the <see cref="OnSignatureVerificationSucceeded"/> handler. Always
        /// returns <see cref="Task.CompletedTask"/> right away.
        /// </summary>
        /// <param name="context">
        /// The <see cref="MessageContext"/> that defines the context for which signature verification succeeded.
        /// </param>
        /// <returns>
        /// A Task that tracks completion of the operation.
        /// </returns>
        public Task DefaultOnSignatureVerificationSucceeded(MessageContext context)
        {
            return Task.CompletedTask;
        }
    }
}
