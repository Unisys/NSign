using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NSign.AspNetCore
{
    /// <summary>
    /// Implements an AspNetCore middleware that verifies the 'content-digest' header of incoming requests against the
    /// request body's corresponding digest. The only supported hashes are SHA-256 and SHA-512.
    /// <para/>
    /// <b>Note</b>: This middleware turns on buffering on the request body such that it can be read multiple times,
    /// since this is necessary to verify potentially multiple different digests of the body; plus it allows middleware
    /// further down in the request pipeline read the body too.
    /// <para/>
    /// <seealso cref="HttpRequestRewindExtensions.EnableBuffering(HttpRequest)"/>
    /// </summary>
    public sealed class ContentDigestVerificationMiddleware : IMiddleware
    {
        /// <summary>
        /// The max expected hash size is 512 bits from SHA-512.
        /// </summary>
        private const int MaxHashSizeBits = 512;

        /// <summary>
        /// The regular expression to parse the 'content-digest' header.
        /// See also <seealso href="https://www.rfc-editor.org/rfc/rfc3230"/>
        /// and <seealso href="https://www.rfc-editor.org/rfc/rfc9530"/>.
        /// </summary>
        private static readonly Regex HeaderValueParser = new Regex(@"(?<= ^|,\s*) ([\w_\-]+) = (?<colon>:?) ([0-9a-zA-Z+/=]+) \k<colon> (?= $|,\s*)",
            RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// The ILogger to use.
        /// </summary>
        private readonly ILogger<ContentDigestVerificationMiddleware> logger;

        /// <summary>
        /// The IOptions of <see cref="ContentDigestVerificationOptions"/> that holds options for this middleware.
        /// </summary>
        private readonly IOptions<ContentDigestVerificationOptions> options;

        /// <summary>
        /// Initializes a new instance of <see cref="ContentDigestVerificationMiddleware"/>.
        /// </summary>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        /// <param name="options">
        /// An IOptions of <see cref="ContentDigestVerificationOptions"/> that holds options for this middleware.
        /// </param>
        public ContentDigestVerificationMiddleware(
            ILogger<ContentDigestVerificationMiddleware> logger,
            IOptions<ContentDigestVerificationOptions> options)
        {
            this.logger = logger;
            this.options = options;
        }

        /// <inheritdoc/>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            ContentDigestVerificationOptions options = this.options.Value;

            if (!context.Request.Headers.TryGetValue(Constants.Headers.ContentDigest, out StringValues values))
            {
                logger.LogDebug("There is no 'Content-Digest' header in the request.");
                if (!options.Behavior.HasFlag(ContentDigestVerificationOptions.VerificationBehavior.Optional))
                {
                    // Terminate the request with the corresponding status code.
                    context.Response.StatusCode = options.MissingHeaderResponseStatus;
                }
                else
                {
                    // Just move on to the next handler in the pipeline.
                    await next(context);
                }

                return;
            }

            int numMatches = 0;
            int numUnknown = 0;
            int numMismatches = 0;

            using IMemoryOwner<byte> expectedHashBufferOwner = MemoryPool<byte>.Shared.Rent(MaxHashSizeBits / 8);
            Memory<byte> expectedHashBuffer = expectedHashBufferOwner.Memory;

            context.Request.EnableBuffering();
            try
            {
                foreach (KeyValuePair<string, string> hashEntry in ParseHeaders(values))
                {
                    if (!TryGetHashAlgorithm(hashEntry.Key, out IncrementalHash? hashAlgorithm, out int sizeBytes))
                    {
                        logger.LogDebug("Unsupported hash algorithm '{alg}'.", hashEntry.Key);
                        ++numUnknown;
                        continue;
                    }

                    Memory<byte> expectedHash = expectedHashBuffer.Slice(0, sizeBytes);

                    if (!Convert.TryFromBase64String(hashEntry.Value.ToString(), expectedHash.Span, out int actualSize) ||
                        expectedHash.Length != actualSize)
                    {
                        // The hash from the header doesn't match the expected length for the algorithm, so it must be a mismatch.
                        logger.LogDebug(
                            "Length of hash '{hash}' of algorithm '{alg}' doesn't match the expected length of {expectedSize} " +
                            "bytes or is not a valid base64-encoded value.",
                            hashEntry.Value, hashEntry.Key, sizeBytes);
                        ++numMismatches;
                        continue;
                    }

                    using IMemoryOwner<byte> actualHashOwner = MemoryPool<byte>.Shared.Rent(sizeBytes);
                    Memory<byte> actualHash = actualHashOwner.Memory;

                    if (!await TryComputeHashAsync(context.Request.Body, hashAlgorithm!, actualHash))
                    {
                        // This should really never happen, but be prepared in case it does.
                        logger.LogWarning("Hash algorithm '{alg}' tried to produce more than the expected size of {expectedSize} bytes.",
                            hashEntry.Key, sizeBytes);
                        ++numMismatches;
                    }
                    else if (CryptographicOperations.FixedTimeEquals(expectedHash.Span, actualHash.Span))
                    {
                        logger.LogDebug("Digest of type '{alg}' matches the expected digest.", hashEntry.Key);
                        ++numMatches;
                    }
                    else
                    {
                        logger.LogDebug("Digest of type '{alg}' does NOT match the expected digest.", hashEntry.Key);
                        ++numMismatches;
                    }

                    // Reset the body stream to the beginning again, either for the next digest or for any other middleware
                    // in the pipeline.
                    context.Request.Body.Position = 0;
                }

                if ((!options.Behavior.HasFlag(ContentDigestVerificationOptions.VerificationBehavior.IgnoreUnknownAlgorithms) && numUnknown > 0) ||
                    (!options.Behavior.HasFlag(ContentDigestVerificationOptions.VerificationBehavior.RequireOnlySingleMatch) && numMismatches > 0) ||
                    numMatches <= 0)
                {
                    context.Response.StatusCode = options.VerificationFailuresResponseStatus;
                }
                else
                {
                    await next(context);
                }
            }
            catch (InvalidDataException ex)
            {
                logger.LogInformation(ex, "Some digest header values are malformed.");
                context.Response.StatusCode = options.VerificationFailuresResponseStatus;
            }
        }

        /// <summary>
        /// Parse the given values from a 'content-digest' header.
        /// </summary>
        /// <param name="values">
        /// A StringValue value representing the values from all 'content-digest' headers.
        /// </param>
        /// <returns>
        /// An IEnumerable of KeyValuePair of string and string representing the hash algorithm names mapped to their
        /// respective base64 encoded values.
        /// </returns>
        private static IEnumerable<KeyValuePair<string, string>> ParseHeaders(StringValues values)
        {
            foreach (string? value in values)
            {
                if (value is null) {
                    continue;
                }

                MatchCollection matches = HeaderValueParser.Matches(value);

                if (matches.Count <= 0)
                {
                    throw new InvalidDataException($"The content-digest header value '{value}' could not be parsed.");
                }

                foreach (Match? match in matches)
                {
                    yield return new KeyValuePair<string, string>(match!.Groups[1].Value, match!.Groups[2].Value);
                }
            }
        }

        /// <summary>
        /// Tries to compute the hash of the given inputStream using the specified hashAlgorithm and storing the resulting
        /// hash in the resultBuffer.
        /// </summary>
        /// <param name="inputStream">
        /// A Stream that represents the input to hash.
        /// </param>
        /// <param name="hashAlgorithm">
        /// An IncrementalHash object which can calculate the hash.
        /// </param>
        /// <param name="resultBuffer">
        /// A Memory of byte value to be updated with the resulting hash.
        /// </param>
        /// <returns>
        /// A Task which results in a boolean value indicating whether or not the computation succeeded on completion.
        /// </returns>
        private async static Task<bool> TryComputeHashAsync(Stream inputStream, IncrementalHash hashAlgorithm, Memory<byte> resultBuffer)
        {
            Debug.Assert(null != inputStream, "The input stream must not be null.");
            Debug.Assert(null != hashAlgorithm, "The hash algorithm must not be null.");

            using IMemoryOwner<byte> bufferOwner = MemoryPool<byte>.Shared.Rent(16 * 1024);
            Memory<byte> buffer = bufferOwner.Memory;
            int read;

            while (0 < (read = await inputStream.ReadAsync(buffer)))
            {
                ReadOnlyMemory<byte> bufferRead = buffer.Slice(0, read);
                hashAlgorithm.AppendData(bufferRead.Span);
            }

            if (!hashAlgorithm.TryGetHashAndReset(resultBuffer.Span, out _))
            {
                // This should really never happen, but be prepared in case it does.
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to get the <see cref="IncrementalHash"/> matching the given algorithm name.
        /// </summary>
        /// <param name="algName">
        /// The name of the algorithm to try to find.
        /// </param>
        /// <param name="alg">
        /// If the algorithm was found, holds an instance of the corresponding <see cref="IncrementalHash"/> class.
        /// </param>
        /// <param name="sizeBytes">
        /// If the algorithm was found, holds the size (in bytes) of the hashes produced by the algorithm.
        /// </param>
        /// <returns>
        /// True if the algorithm was found, or false otherwise.
        /// </returns>
        private static bool TryGetHashAlgorithm(string algName, out IncrementalHash? alg, out int sizeBytes)
        {
            switch (algName.ToUpper())
            {
                case Constants.HashAlgorithms.Sha256:
                    alg = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                    sizeBytes = 256 / 8;
                    return true;

                case Constants.HashAlgorithms.Sha512:
                    alg = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
                    sizeBytes = 512 / 8;
                    return true;

                default:
                    alg = null;
                    sizeBytes = 0;
                    return false;
            }
        }
    }
}
