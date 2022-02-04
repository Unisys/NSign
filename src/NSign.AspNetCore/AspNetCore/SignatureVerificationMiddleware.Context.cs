using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace NSign.AspNetCore
{
    partial class SignatureVerificationMiddleware
    {
        /// <summary>
        /// Holds the signature verification context of a single HTTP request.
        /// </summary>
        private readonly struct Context
        {
            /// <summary>
            /// Basic parser for identifying individual signatures in the 'signature' header.
            /// </summary>
            private static readonly Regex SignatureParser = new Regex(
                "(?<=^|,\\s*) (\\w+) = : ([A-Za-z0-9+/=]+) : (?=,\\s*|$)",
                RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            /// <summary>
            /// Basic and simplified parser for identifying individual signature input specs in the 'signature-input' header.
            /// </summary>
            /// <remarks>
            /// This regex does NOT exactly parse according to RFC 8941, in particular when it comes down to commas in
            /// sf-string structured values; this however shouldn't be a problem here since the allowed values (HTTP
            /// headers and predefined derived components, as well as parameters and their values as per the standard)
            /// wouldn't use them.
            /// </remarks>
            private static readonly Regex SignatureInputParser = new Regex(
                "(?<=^|,\\s*) (\\w+) = ([^,]*) (?=,\\s*|$)",
                RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            /// <summary>
            /// Initializes a new instance of Context.
            /// </summary>
            /// <param name="signatureValues">
            /// A StringValues value identifying all the values from all 'signature' headers in the request.
            /// </param>
            /// <param name="signatureInputValues">
            /// A StringValues value identifying all the values from all 'signature-input' headers in the request.
            /// </param>
            /// <param name="request">
            /// The HttpRequest this context is for.
            /// </param>
            /// <param name="options">
            /// The RequestSignatureVerificationOptions object describing the options for signature verification.
            /// </param>
            public Context(
                StringValues signatureValues,
                StringValues signatureInputValues,
                HttpRequest request,
                RequestSignatureVerificationOptions options)
            {
                Signatures = ParseHeaders(signatureValues, signatureInputValues).ToImmutableList();
                Request = request;
                Options = options;
            }

            /// <summary>
            /// Gets a flag which indicates whether or not there are any signatures for verification.
            /// </summary>
            public bool HasSignatures => Signatures.Count > 0;

            /// <summary>
            /// Gets an ImmutableList of SignatureContext values defining the signatures and their input specs identified
            /// from the request.
            /// </summary>
            public ImmutableList<SignatureContext> Signatures { get; }

            /// <summary>
            /// Gets the HttpRequest object this context is for.
            /// </summary>
            public HttpRequest Request { get; }

            /// <summary>
            /// Gets the RequestSignatureVerificationOptions object describing the options for signature verification.
            /// </summary>
            public RequestSignatureVerificationOptions Options { get; }

            #region Private Methods

            /// <summary>
            /// Parses the 'signature' and 'signature-input' headers to identify all signatures with their corresponding
            /// input specs for evaluation.
            /// </summary>
            /// <param name="signatureValues">
            /// A StringValues value identifying all the values from all 'signature' headers in the request.
            /// </param>
            /// <param name="signatureInputValues">
            /// A StringValues value identifying all the values from all 'signature-input' headers in the request.
            /// </param>
            /// <returns>
            /// An IEnumerable of SignatureContext values representing all the signatures' details found in the headers.
            /// </returns>
            private static IEnumerable<SignatureContext> ParseHeaders(
                StringValues signatureValues,
                StringValues signatureInputValues)
            {
                Dictionary<string, byte[]> signatures = ParseSignatures(signatureValues);
                Dictionary<string, string> inputs = ParseSignatureInputs(signatureInputValues);

                return from sig in signatures
                       join input in inputs
                       on sig.Key equals input.Key into sigWithInput
                       from optionalInput in sigWithInput.DefaultIfEmpty()
                       select new SignatureContext(sig.Key, optionalInput.Value, sig.Value);
            }

            /// <summary>
            /// Parses the values from 'signature' headers.
            /// </summary>
            /// <param name="signatureValues">
            /// A StringValues value identifying all the values from all 'signature' headers in the request.
            /// </param>
            /// <returns>
            /// A Dictionary of string and array of byte representing all the identified signatures with their name and
            /// signature hash.
            /// </returns>
            private static Dictionary<string, byte[]> ParseSignatures(StringValues signatureValues)
            {
                Dictionary<string, byte[]> signatures = new Dictionary<string, byte[]>();

                foreach (string signatureHeader in signatureValues)
                {
                    MatchCollection matches = SignatureParser.Matches(signatureHeader);

                    if (matches.Count <= 0)
                    {
                        throw new FormatException($"Malformed signature header: '{signatureHeader}'.");
                    }

                    foreach (Match match in matches)
                    {
                        signatures.Add(match.Groups[1].Value, Convert.FromBase64String(match.Groups[2].Value));
                    }
                }

                return signatures;
            }

            /// <summary>
            /// Parses the values from 'signature-input' headers. This does not include parsing signature input specs,
            /// which is deferred until when it is actually needed.
            /// </summary>
            /// <param name="signatureInputValues">
            /// A StringValues value identifying all the values from all 'signature-input' headers in the request.
            /// </param>
            /// <returns>
            /// A Dictionary of string and string representing all the identified signature inputs with their name and
            /// unparsed input spec.
            /// </returns>
            private static Dictionary<string, string> ParseSignatureInputs(StringValues signatureInputValues)
            {
                Dictionary<string, string> inputs = new Dictionary<string, string>();

                foreach (string inputHeader in signatureInputValues)
                {
                    MatchCollection matches = SignatureInputParser.Matches(inputHeader);

                    if (matches.Count <= 0)
                    {
                        throw new FormatException($"Malformed signature-input header: '{inputHeader}'.");
                    }

                    foreach (Match match in matches)
                    {
                        inputs.Add(match.Groups[1].Value, match.Groups[2].Value);
                    }
                }

                return inputs;
            }

            #endregion
        }
    }
}
