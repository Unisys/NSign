using System;

namespace NSign.Signatures
{
    /// <summary>
    /// Extensions for the SignatureAlgorithms enum.
    /// </summary>
    public static class SignatureAlgorithmsExtensions
    {
        /// <summary>
        /// Gets the algorithm's name as per the spec.
        /// </summary>
        /// <param name="algorithm">
        /// The SignatureAlgorithm value for which to get the name.
        /// </param>
        /// <returns>
        /// A string representing the algorithm's name.
        /// </returns>
        public static string GetName(this SignatureAlgorithm algorithm)
        {
            return algorithm switch
            {
                SignatureAlgorithm.RsaPssSha512 => Constants.SignatureAlgorithms.RsaPssSha512,
                SignatureAlgorithm.RsaPkcs15Sha256 => Constants.SignatureAlgorithms.RsaPkcs15Sha256,
                SignatureAlgorithm.HmacSha256 => Constants.SignatureAlgorithms.HmacSha256,
                SignatureAlgorithm.EcdsaP256Sha256 => Constants.SignatureAlgorithms.EcdsaP256Sha256,
                SignatureAlgorithm.EcdsaP384Sha384 => Constants.SignatureAlgorithms.EcdsaP384Sha384,

                _ => throw new NotSupportedException($"Unsupported signature algorithm: {algorithm}"),
            };
        }

        /// <summary>
        /// Converts the given signature algorithm name to a SignatureAlgorithm value.
        /// </summary>
        /// <param name="algorithm">
        /// The name of a signature algorithm to convert.
        /// </param>
        /// <returns>
        /// A SignatureAlgorithm value representing the given algorithm.
        /// </returns>
        public static SignatureAlgorithm ToSignatureAlgorithm(this string algorithm)
        {
            return algorithm switch
            {
                Constants.SignatureAlgorithms.RsaPssSha512 => SignatureAlgorithm.RsaPssSha512,
                Constants.SignatureAlgorithms.RsaPkcs15Sha256 => SignatureAlgorithm.RsaPkcs15Sha256,
                Constants.SignatureAlgorithms.HmacSha256 => SignatureAlgorithm.HmacSha256,
                Constants.SignatureAlgorithms.EcdsaP256Sha256 => SignatureAlgorithm.EcdsaP256Sha256,
                Constants.SignatureAlgorithms.EcdsaP384Sha384 => SignatureAlgorithm.EcdsaP384Sha384,

                _ => throw new NotSupportedException($"Unsupported signature algorithm: '{algorithm}'"),
            };
        }
    }
}
