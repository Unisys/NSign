using Microsoft.Extensions.Logging;
using NSign.Http;
using StructuredFieldValues;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace NSign.Signatures
{
    partial class MessageContext
    {
        /// <summary>
        /// Helper class to lazily load and parse signature and signature inputs on HTTP messages, both on requests and
        /// responses.
        /// </summary>
        private sealed class Signatures
        {
            #region Fields

            /// <summary>
            /// The <see cref="MessageContext"/> that owns these signatures.
            /// </summary>
            private readonly MessageContext context;

            /// <summary>
            /// A <see cref="Lazy{T}"/> of <see cref="Dictionary{TKey, TValue}"/> of string and <see cref="SignatureContext"/>
            /// that tracks parsed signatures and signature inputs from the request message.
            /// </summary>
            private readonly Lazy<Dictionary<string, SignatureContext>> request;

            /// <summary>
            /// A <see cref="Lazy{T}"/> of <see cref="Dictionary{TKey, TValue}"/> of string and <see cref="SignatureContext"/>
            /// that tracks parsed signatures and signature inputs from the response message, if any.
            /// </summary>
            private readonly Lazy<Dictionary<string, SignatureContext>> response;

            #endregion

            #region C'tors

            /// <summary>
            /// Initializes a new instance of <see cref="Signatures"/>.
            /// </summary>
            /// <param name="context">
            /// The <see cref="MessageContext"/> that owns these signatures.
            /// </param>
            public Signatures(MessageContext context)
            {
                this.context = context;

                request = new Lazy<Dictionary<string, SignatureContext>>(LoadRequestSignatures, LazyThreadSafetyMode.None);
                response = new Lazy<Dictionary<string, SignatureContext>>(LoadResponseSignatures, LazyThreadSafetyMode.None);
            }

            #endregion

            #region Public Interface

            /// <summary>
            /// Gets a flag which indicates whether or not there are any signatures on the request message.
            /// </summary>
            public bool HasRequestSignatures => request.Value.Count > 0;

            /// <summary>
            /// Gets a flag which indicates whether or not there are any signature on the response message.
            /// </summary>
            public bool HasResponseSignatures => response.Value.Count > 0;

            /// <summary>
            /// Gets an <see cref="IEnumerable{T}"/> of <see cref="SignatureContext"/> values representing the signatures
            /// from the request message.
            /// </summary>
            public IEnumerable<SignatureContext> RequestSignatures => request.Value.Values;

            /// <summary>
            /// Gets an <see cref="IEnumerable{T}"/> of <see cref="SignatureContext"/> values representing the signatures
            /// from the response message, if any.
            /// </summary>
            public IEnumerable<SignatureContext> ResponseSignatures => response.Value.Values;

            /// <summary>
            /// Tries to get a specific signature from the request message.
            /// </summary>
            /// <param name="key">
            /// The key/name of the signature to get.
            /// </param>
            /// <param name="signatureContext">
            /// If successful, holds the <see cref="SignatureContext"/> value that represents the parsed signature.
            /// </param>
            /// <returns>
            /// True if the signature was found, false otherwise.
            /// </returns>
            public bool TryGetRequestSignature(string key, out SignatureContext signatureContext)
            {
                return request.Value.TryGetValue(key, out signatureContext);
            }

            /// <summary>
            /// Tries to get a specific signature from the response message, if available.
            /// </summary>
            /// <param name="key">
            /// The key/name of the signature to get.
            /// </param>
            /// <param name="signatureContext">
            /// If successful, holds the <see cref="SignatureContext"/> value that represents the parsed signature.
            /// </param>
            /// <returns>
            /// True if the signature was found, false otherwise.
            /// </returns>
            public bool TryGetResponseSignature(string key, out SignatureContext signatureContext)
            {
                Debug.Assert(context.HasResponse, "Response signatures are only supported when there is a response.");

                return response.Value.TryGetValue(key, out signatureContext);
            }

            #endregion

            #region Private Methods

            /// <summary>
            /// Loads signatures from the request message.
            /// </summary>
            /// <returns>
            /// A <see cref="Dictionary{TKey, TValue}"/> of string and <see cref="SignatureContext"/> that represents
            /// the parsed signatures with optional signature input specs.
            /// </returns>
            private Dictionary<string, SignatureContext> LoadRequestSignatures()
            {
                IEnumerable<string> signatures = context.GetRequestHeaderValues(Constants.Headers.Signature);
                IEnumerable<string> inputs = context.GetRequestHeaderValues(Constants.Headers.SignatureInput);

                return LoadSignatures(signatures, inputs);
            }

            /// <summary>
            /// Loads signatures from the response message.
            /// </summary>
            /// <returns>
            /// A <see cref="Dictionary{TKey, TValue}"/> of string and <see cref="SignatureContext"/> that represents
            /// the parsed signatures with optional signature input specs.
            /// </returns>
            private Dictionary<string, SignatureContext> LoadResponseSignatures()
            {
                Debug.Assert(context.HasResponse, "Response signatures are only supported when there is a response.");

                IEnumerable<string> signatures = context.GetHeaderValues(Constants.Headers.Signature);
                IEnumerable<string> inputs = context.GetHeaderValues(Constants.Headers.SignatureInput);

                return LoadSignatures(signatures, inputs);
            }

            /// <summary>
            /// Loads signatures from the given header values.
            /// </summary>
            /// <param name="sigValues">
            /// The header values of the 'signature' headers to parse.
            /// </param>
            /// <param name="sigInputValues">
            /// The header values of the 'signature-input' headers to parse.
            /// </param>
            /// <returns>
            /// A <see cref="Dictionary{TKey, TValue}"/> of string and <see cref="SignatureContext"/> that represents
            /// the parsed signatures with signature input specs. Inputs or signatures that lack their counterpart are
            /// not included.
            /// </returns>
            private Dictionary<string, SignatureContext> LoadSignatures(
                IEnumerable<string> sigValues,
                IEnumerable<string> sigInputValues)
            {
                Dictionary<string, ReadOnlyMemory<byte>> signatures = ParseSignatures(sigValues);
                Dictionary<string, string> inputs = ParseSignatureInputs(sigInputValues);

                return (from sig in signatures
                        join input in inputs
                        on sig.Key equals input.Key
                        select new SignatureContext(sig.Key, input.Value, sig.Value))
                        .ToDictionary(ctx => ctx.Name);
            }

            /// <summary>
            /// Parses the values from 'signature' headers.
            /// </summary>
            /// <param name="signatureValues">
            /// An IEnumerable of string values identifying all the values from all 'signature' headers in the request.
            /// </param>
            /// <returns>
            /// A Dictionary of string and array of byte representing all the identified signatures with their name and
            /// signature hash.
            /// </returns>
            private Dictionary<string, ReadOnlyMemory<byte>> ParseSignatures(IEnumerable<string> signatureValues)
            {
                Dictionary<string, ReadOnlyMemory<byte>> signatures = new Dictionary<string, ReadOnlyMemory<byte>>();

                foreach (string signatureHeader in signatureValues)
                {
                    ParseError? error = SfvParser.ParseDictionary(
                        signatureHeader,
                        out IReadOnlyDictionary<string, ParsedItem>? map);

                    if (error.HasValue)
                    {
                        context.Logger.LogWarning("Failed to parse signature header '{header}': {error}",
                            signatureHeader, error);
                        continue;
                    }

                    foreach (KeyValuePair<string, ParsedItem> item in map)
                    {
                        if (item.Value.TryGetBinaryData(out ReadOnlyMemory<byte> signature))
                        {
                            signatures.Add(item.Key, signature.ToArray());
                        }
                        else
                        {
                            context.Logger.LogWarning("Signature '{sig}' does not have a binary value.", item.Key);
                        }
                    }
                }

                return signatures;
            }

            /// <summary>
            /// Parses the values from 'signature-input' headers. This does not include parsing signature input specs,
            /// which is deferred until when it is actually needed.
            /// </summary>
            /// <param name="signatureInputValues">
            /// A IEnumerable of string values identifying all the values from all 'signature-input' headers in the request.
            /// </param>
            /// <returns>
            /// A Dictionary of string and string representing all the identified signature inputs with their name and
            /// unparsed input spec.
            /// </returns>
            private Dictionary<string, string> ParseSignatureInputs(IEnumerable<string> signatureInputValues)
            {
                Dictionary<string, string> inputs = new Dictionary<string, string>();

                foreach (string inputHeader in signatureInputValues)
                {
                    ParseError? error = SfvParser.ParseDictionary(
                        inputHeader,
                        out IReadOnlyDictionary<string, ParsedItem>? map);

                    if (error.HasValue)
                    {
                        context.Logger.LogWarning("Failed to parse signature-input header '{header}': {error}",
                            inputHeader, error);
                        continue;
                    }

                    foreach (KeyValuePair<string, ParsedItem> item in map)
                    {
                        string value = item.Value.Value.SerializeAsString() + item.Value.Parameters.SerializeAsParameters();
                        inputs.Add(item.Key, value);
                    }
                }

                return inputs;
            }

            #endregion
        }
    }
}
