using NSign.Signatures;
using System;

namespace NSign.Providers
{
    /// <summary>
    /// Provides extensions for <see cref="SignatureParamsComponent"/> objects.
    /// </summary>
    public static class SignatureParamsComponentExtensions
    {
        /// <summary>
        /// Checks if the given <see cref="SignatureParamsComponent"/> object has the algorithm parameter set to the given
        /// <paramref name="expectedAlgorithm"/> or does not have the algorithm parameter set.
        /// </summary>
        /// <param name="signatureParams">
        /// The SignatureParamsComponent object to check.
        /// </param>
        /// <param name="expectedAlgorithm">
        /// The name of the expected algorithm.
        /// </param>
        /// <returns>
        /// True if no algorithm is set, or the algorithm matches the expectedAlgorithm, and false if the algorithm does
        /// not match.
        /// </returns>
        public static bool HasMatchingAlgorithm(this SignatureParamsComponent signatureParams, string expectedAlgorithm)
        {
            return String.IsNullOrEmpty(signatureParams.Algorithm) ||
                   StringComparer.Ordinal.Equals(expectedAlgorithm, signatureParams.Algorithm);
        }

        /// <summary>
        /// Checks if the given <see cref="SignatureParamsComponent"/> object has the keyid parameter set to the given
        /// <paramref name="expectedKeyId"/> or does not have the keyid parameter set.
        /// </summary>
        /// <param name="signatureParams">
        /// The SignatureParamsComponent object to check.
        /// </param>
        /// <param name="expectedKeyId">
        /// The name of the expected keyid.
        /// </param>
        /// <returns>
        /// True if no keyid is set, or the keyid matches the expectedKeyId, and false if the keyid does not match.
        /// </returns>
        public static bool HasMatchingKeyId(this SignatureParamsComponent signatureParams, string expectedKeyId)
        {
            return String.IsNullOrEmpty(signatureParams.KeyId) ||
                   StringComparer.Ordinal.Equals(expectedKeyId, signatureParams.KeyId);
        }
    }
}
