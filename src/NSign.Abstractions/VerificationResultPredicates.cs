using System.Collections.Generic;

namespace NSign
{
    /// <summary>
    /// Offers predicates for evaluation of <see cref="VerificationResult"/> values.
    /// </summary>
    public static class VerificationResultPredicates
    {
        /// <summary>
        /// Checks if a verification has a failing result.
        /// </summary>
        /// <param name="result">
        /// The KeyValuePair of string and VerificationResult to check.
        /// </param>
        /// <returns>
        /// True if the verification has failed, or false otherwise.
        /// </returns>
        public static bool VerificationFailed(KeyValuePair<string, VerificationResult> result)
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
        public static bool SignatureInputError(KeyValuePair<string, VerificationResult> result)
        {
            return result.Value == VerificationResult.SignatureInputMalformed ||
                result.Value == VerificationResult.SignatureInputComponentMissing;
        }
    }
}
