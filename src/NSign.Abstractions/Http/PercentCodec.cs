using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NSign.Http
{
    /// <summary>
    /// Percent-encoding encoder/decoder. Does percent-encoding according to <see href="https://url.spec.whatwg.org/"/>
    /// </summary>
    public static class PercentCodec
    {
        /// <summary>
        /// Regex to match all characters that need percent encoding. This is the set of all non-ASCII code points, as
        /// well as ASCII code points except alpha-numeric ASCII plus '*', '-', '.', '_'. Thus, it's easier to create
        /// a regular expression that is based on the code points that do <b>not</b> require encoding.
        /// </summary>
        private static readonly Regex EncodingNeeded = new Regex("[^a-zA-Z0-9*\\-._]", RegexOptions.Singleline);

        /// <summary>
        /// Pre-calculated replacement strings for percent encoding of individual byte values.
        /// </summary>
        private static readonly string[] ByteEncoding = Enumerable.Range(0, 255)
            .Select(b => $"%{b.ToString("X2")}").ToArray();

        /// <summary>
        /// Encode the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">
        /// The value to encode.
        /// </param>
        /// <returns>
        /// The percent-encoded value.
        /// </returns>
        public static string Encode(string value)
        {
            return Encode(value, decodeFirst: false);
        }

        /// <summary>
        /// Encode the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">
        /// The value to encode. The value is first decoded (through <see cref="Uri.UnescapeDataString"/>) to ensure
        /// already escaped code points are not double-escaped.
        /// </param>
        /// <param name="decodeFirst">
        /// A flag that indicates whether to decode the value first.
        /// </param>
        /// <returns>
        /// The percent-encoded value.
        /// </returns>
        public static string Encode(string value, bool decodeFirst)
        {
            if (decodeFirst)
            {
                value = Uri.UnescapeDataString(value);
            }

            return EncodingNeeded.Replace(value, Encode);
        }

        /// <summary>
        /// Encode the character matched by the <see cref="EncodingNeeded"/> regex.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Match"/> to percent encode.
        /// </param>
        /// <returns>
        /// The encoded value from the match.
        /// </returns>
        private static string Encode(Match match)
        {
            Span<byte> bytes = stackalloc byte[8]; // It should be safe to assume at most 8 bytes per code-point.
            int numBytes = Encoding.UTF8.GetBytes(match.Value, bytes);
            StringBuilder result = new StringBuilder(numBytes * 3);

            for (int i = 0; i < numBytes; i++)
            {
                result.Append(ByteEncoding[bytes[i]]);
            }

            return result.ToString();
        }
    }
}
