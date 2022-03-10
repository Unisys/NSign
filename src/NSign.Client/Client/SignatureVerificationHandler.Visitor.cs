using NSign.Http;
using NSign.Signatures;
using StructuredFieldValues;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace NSign.Client
{
    partial class SignatureVerificationHandler
    {
        /// <summary>
        /// Implements the ISignatureComponentVisitor interface to help build signature input for an incoming HTTP
        /// response message for signature verification.
        /// </summary>
        private sealed class Visitor : ISignatureComponentVisitor
        {
            #region Fields

            /// <summary>
            /// The HttpRequestMessage which caused the response for which to build signature input.
            /// </summary>
            private readonly HttpRequestMessage request;

            /// <summary>
            /// The HttpResponseMessage for which to build signature input.
            /// </summary>
            private readonly HttpResponseMessage response;

            /// <summary>
            /// A StringBuilder which tracks signature input as it is being built.
            /// </summary>
            private readonly StringBuilder signatureInput = new StringBuilder();

            #endregion

            /// <summary>
            /// Initializes a new instance of Visitor.
            /// </summary>
            /// <param name="request">
            /// The HttpRequestMessage that caused the response for which to build signature input.
            /// </param>
            /// <param name="response">
            /// The HttpResponseMessage for which to build signature input.
            /// </param>
            public Visitor(HttpRequestMessage request, HttpResponseMessage response)
            {
                this.request = request;
                this.response = response;
            }

            /// <summary>
            /// Gets the signature input as built by the visitor.
            /// </summary>
            public string SignatureInput => signatureInput.ToString();

            /// <inheritdoc/>
            public void Visit(SignatureComponent component)
            {
                throw new NotSupportedException("Custom classes derived from SignatureComponent are not supported.");
            }

            /// <inheritdoc/>
            public void Visit(HttpHeaderComponent httpHeader)
            {
                if (response.Headers.TryGetValues(httpHeader.ComponentName, out IEnumerable<string> values))
                {
                    AddInput(httpHeader, String.Join(", ", values));
                }
                else
                {
                    throw new SignatureComponentMissingException(httpHeader);
                }
            }

            /// <inheritdoc/>
            public void Visit(HttpHeaderDictionaryStructuredComponent httpHeaderDictionary)
            {
                if (response.Headers.TryGetValues(httpHeaderDictionary.ComponentName, out IEnumerable<string> values))
                {
                    // Per RFC 8941, only the last value for a key is considered, so we're only tracking the last value
                    // we find.
                    ParsedItem? lastValue = null;

                    foreach (string value in values)
                    {
                        if (null == SfvParser.ParseDictionary(value, out IReadOnlyDictionary<string, ParsedItem> actualDict) &&
                            actualDict.TryGetValue(httpHeaderDictionary.Key, out ParsedItem valueForKey))
                        {
                            lastValue = valueForKey;
                        }
                    }

                    if (lastValue.HasValue)
                    {
                        AddInputWithKey(httpHeaderDictionary,
                                        lastValue.Value.Value.SerializeAsString() +
                                        lastValue.Value.Parameters.SerializeAsParameters());

                        return;
                    }
                }

                throw new SignatureComponentMissingException(httpHeaderDictionary);
            }

            /// <inheritdoc/>
            public void Visit(DerivedComponent derivedComponent)
            {
                string value = derivedComponent.ComponentName switch
                {
                    Constants.DerivedComponents.Status => ((int)response.StatusCode).ToString(),

                    _ => request.GetDerivedComponentValue(derivedComponent),
                };

                AddInput(derivedComponent, value);
            }

            /// <inheritdoc/>
            public void Visit(SignatureParamsComponent signatureParamsComponent)
            {
                if (String.IsNullOrWhiteSpace(signatureParamsComponent.OriginalValue))
                {
                    throw new InvalidOperationException(
                        "Signature input can only be created for SignatureParamsComponents received from HTTP requests.");
                }

                // Build the input for all the components registered in the signature parameters.
                foreach (SignatureComponent component in signatureParamsComponent.Components)
                {
                    component.Accept(this);
                }

                // And finally add the signature parameters as the last line for the input.
                AddInput(signatureParamsComponent, signatureParamsComponent.OriginalValue);
            }

            /// <inheritdoc/>
            public void Visit(QueryParamsComponent queryParams)
            {
                string[] values = request.GetQueryParamValues(queryParams);

                if (null == values)
                {
                    throw new SignatureComponentMissingException(queryParams);
                }

                foreach (string value in values)
                {
                    AddInputWithName(queryParams, value);
                }
            }

            /// <inheritdoc/>
            public void Visit(RequestResponseComponent requestResponse)
            {
                // The @request-response is a reference to a signature in the request message, so we need to look at
                // the request's headers here.
                if (request.Headers.TryGetValues(Constants.Headers.Signature, out IEnumerable<string> values) &&
                    TryGetDictValue(values, requestResponse.Key, out ParsedItem? lastValue))
                {
                    AddInputWithKey(requestResponse,
                           lastValue.Value.Value.SerializeAsString() +
                           lastValue.Value.Parameters.SerializeAsParameters());
                    return;
                }

                throw new SignatureComponentMissingException(requestResponse);
            }

            #region Private Methods

            /// <summary>
            /// Adds a line to the signature input for the given component.
            /// </summary>
            /// <param name="component">
            /// The component to add a line for.
            /// </param>
            /// <param name="value">
            /// The component's value to use.
            /// </param>
            private void AddInput(SignatureComponent component, string value)
            {
                AddInput(component.ComponentName, value);
            }

            /// <summary>
            /// Adds a line to the signature input for the given component with a 'key' parameter.
            /// </summary>
            /// <param name="component">
            /// The component to add a line for.
            /// </param>
            /// <param name="value">
            /// The component's value to use.
            /// </param>
            private void AddInputWithKey(ISignatureComponentWithKey component, string value)
            {
                AddInput($"{component.ComponentName}\";{Constants.ComponentParameters.Key}=\"{component.Key}", value);
            }

            /// <summary>
            /// Adds a line to the signature input for the given component with a 'name' parameter.
            /// </summary>
            /// <param name="component">
            /// The component to add a line for.
            /// </param>
            /// <param name="value">
            /// The component's value to use.
            /// </param>
            private void AddInputWithName(ISignatureComponentWithName component, string value)
            {
                AddInput($"{component.ComponentName}\";{Constants.ComponentParameters.Name}=\"{component.Name}", value);
            }

            /// <summary>
            /// Adds a line to the signature input for the given component.
            /// </summary>
            /// <param name="componentSpec">
            /// The full spec of the component, including its parameters if any.
            /// </param>
            /// <param name="value">
            /// The component's value to use.
            /// </param>
            private void AddInput(string componentSpec, string value)
            {
                if (signatureInput.Length > 0)
                {
                    signatureInput.Append('\n');
                }

                signatureInput.Append($"\"{componentSpec}\": {value}");
            }

            /// <summary>
            /// Tries to get a dictionary entry from a set of structured dictionary header values.
            /// </summary>
            /// <param name="values">
            /// The <see cref="IEnumerable{String}"/> value representing all the values for the header.
            /// </param>
            /// <param name="key">
            /// The key of the entry in the structured dictionary header to get the value for.
            /// </param>
            /// <param name="lastValue">
            /// On success, holds the last found value for the given key.
            /// </param>
            /// <returns>
            /// True if successful, or false otherwise.
            /// </returns>
            private static bool TryGetDictValue(IEnumerable<string> values, string key, out ParsedItem? lastValue)
            {
                lastValue = null;

                foreach (string value in values)
                {
                    if (null == SfvParser.ParseDictionary(value, out IReadOnlyDictionary<string, ParsedItem> actualDict) &&
                        actualDict.TryGetValue(key, out ParsedItem valueForKey))
                    {
                        lastValue = valueForKey;
                    }
                }

                return lastValue.HasValue;
            }

            #endregion
        }
    }
}
