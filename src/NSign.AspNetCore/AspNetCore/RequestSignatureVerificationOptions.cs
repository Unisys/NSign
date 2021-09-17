namespace NSign.AspNetCore
{
    /// <summary>
    /// Options class to control signature validation on HTTP request messages through the <see cref="SignatureVerificationMiddleware"/>.
    /// </summary>
    public sealed class RequestSignatureVerificationOptions : SignatureVerificationOptions
    {
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
    }
}
