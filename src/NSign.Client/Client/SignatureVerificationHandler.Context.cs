using NSign.Signatures;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace NSign.Client
{
    partial class SignatureVerificationHandler
    {
        /// <summary>
        /// Holds the signature verification context of a single HTTP request/response pipeline.
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
            /// An IEnumerable&lt;string&gt; identifying all the values from all 'signature' headers in the request.
            /// </param>
            /// <param name="signatureInputValues">
            /// An IEnumerable&lt;string&gt; identifying all the values from all 'signature-input' headers in the request.
            /// </param>
            /// <param name="request">
            /// The HttpRequest that caused the response this context is for.
            /// </param>
            /// <param name="response">
            /// The HttpResponse this context is for.
            /// </param>
            /// <param name="options">
            /// The SignatureVerificationOptions object describing the options for signature verification.
            /// </param>
            public Context(
                IEnumerable<string> signatureValues,
                IEnumerable<string> signatureInputValues,
                HttpRequestMessage request,
                HttpResponseMessage response,
                SignatureVerificationOptions options)
            {
                Signatures = ParseHeaders(signatureValues, signatureInputValues).ToImmutableList();
                Request = request;
                Response = response;
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
            /// Gets the HttpRequestMessage object this context is for.
            /// </summary>
            public HttpRequestMessage Request { get; }

            /// <summary>
            /// Gets the HttpResponseMessage object this context is for.
            /// </summary>
            public HttpResponseMessage Response { get; }

            /// <summary>
            /// Gets the RequestSignatureVerificationOptions object describing the options for signature verification.
            /// </summary>
            public SignatureVerificationOptions Options { get; }

            /// <summary>
            /// Gets a byte array representing the actual input for signature verification for this context.
            /// </summary>
            /// <param name="inputSpec">
            /// The SignatureInputSpec object representing the input spec that defines how to build the signature input.
            /// </param>
            /// <returns>
            /// A byte array representing the input for signature verification.
            /// </returns>
            public byte[] GetSignatureInput(SignatureInputSpec inputSpec)
            {
                Visitor visitor = new Visitor(Request, Response);

                inputSpec.SignatureParameters.Accept(visitor);

                return Encoding.ASCII.GetBytes(visitor.SignatureInput);
            }

            #region Private Methods

            /// <summary>
            /// Parses the 'signature' and 'signature-input' headers to identify all signatures with their corresponding
            /// input specs for evaluation.
            /// </summary>
            /// <param name="signatureValues">
            /// An IEnumerable&lt;string&gt; identifying all the values from all 'signature' headers in the request.
            /// </param>
            /// <param name="signatureInputValues">
            /// An IEnumerable&lt;string&gt; identifying all the values from all 'signature-input' headers in the request.
            /// </param>
            /// <returns>
            /// An IEnumerable of SignatureContext values representing all the signatures' details found in the headers.
            /// </returns>
            private static IEnumerable<SignatureContext> ParseHeaders(
                IEnumerable<string> signatureValues,
                IEnumerable<string> signatureInputValues)
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
            /// An IEnumerable&lt;string&gt; identifying all the values from all 'signature' headers in the request.
            /// </param>
            /// <returns>
            /// A Dictionary of string and array of byte representing all the identified signatures with their name and
            /// signature hash.
            /// </returns>
            private static Dictionary<string, byte[]> ParseSignatures(IEnumerable<string> signatureValues)
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
            /// An IEnumerable&lt;string&gt; identifying all the values from all 'signature-input' headers in the request.
            /// </param>
            /// <returns>
            /// A Dictionary of string and string representing all the identified signature inputs with their name and
            /// unparsed input spec.
            /// </returns>
            private static Dictionary<string, string> ParseSignatureInputs(IEnumerable<string> signatureInputValues)
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
