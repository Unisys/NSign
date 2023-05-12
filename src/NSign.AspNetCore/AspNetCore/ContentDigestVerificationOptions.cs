using System;

namespace NSign.AspNetCore
{
    /// <summary>
    /// Options class to control digest verification on HTTP request messages.
    /// </summary>
    public sealed class ContentDigestVerificationOptions
    {
        /// <summary>
        /// Gets or sets the HTTP status code to use when the 'content-digest' header is missing. Defauls to <c>400</c>;
        /// </summary>
        public int MissingHeaderResponseStatus { get; set; } = 400;

        /// <summary>
        /// Gets or sets the HTTP status code to use when 'content-digest' header value verification has failed. Defauls to <c>400</c>;
        /// </summary>
        public int VerificationFailuresResponseStatus { get; set; } = 400;

        /// <summary>
        /// Gets or sets a <see cref="VerificationBehavior"/> value that defines the behavior for verification. Defaults
        /// to <c><see cref="VerificationBehavior.IgnoreUnknownAlgorithms"/></c>.
        /// </summary>
        public VerificationBehavior Behavior { get; set; } = VerificationBehavior.IgnoreUnknownAlgorithms;

        /// <summary>
        /// Defines flags to control the behavior of digest verification.
        /// </summary>
        [Flags]
        public enum VerificationBehavior
        {
            /// <summary>
            /// Do not ignore unknown algorithms and require all algorithms's digest to match.
            /// </summary>
            None = 0x00,

            /// <summary>
            /// Ignore unknown or unsupported algorithms.
            /// </summary>
            IgnoreUnknownAlgorithms = 0x01,

            /// <summary>
            /// Require only a single algorithm's digest to match.
            /// </summary>
            RequireOnlySingleMatch = 0x10,

            /// <summary>
            /// The 'Content-Digest' header is not required; it's verified only when present.
            /// </summary>
            Optional = 0x0100,
        }
    }
}
