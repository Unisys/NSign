using System;

namespace NSign
{
    /// <summary>
    /// Utilities for NSign.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Performs basic string sanitization for data sent to logs.
        /// </summary>
        /// <param name="str">
        /// The <see cref="String"/> to sanitize.
        /// </param>
        /// <returns>
        /// The sanitized <see cref="String"/> or <c>null</c> if the input is null.
        /// </returns>
        public static string? SanitizeBasicForLog(this string? str)
        {
            return str?.Replace("\r", String.Empty).Replace("\n", String.Empty);
        }
    }
}
