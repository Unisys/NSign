using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NSign.Http;
using NSign.Signatures;
using StructuredFieldValues;
using System;
using System.Collections.Generic;
using System.Text;

namespace NSign.AspNetCore
{
    partial class HttpRequestExtensions
    {
        /// <summary>
        /// Implements the ISignatureComponentVisitor interface to help build signature input for an incoming HTTP request.
        /// </summary>
        private sealed class Visitor : ISignatureComponentVisitor
        {
            /// <summary>
            /// The HttpRequest for which to build signature input.
            /// </summary>
            private readonly HttpRequest request;

            /// <summary>
            /// A StringBuilder which tracks signature input as it is being built.
            /// </summary>
            private readonly StringBuilder signatureInput = new StringBuilder();

            /// <summary>
            /// Initializes a new instance of Visitor.
            /// </summary>
            /// <param name="request">
            /// The HttpRequest for which to build signature input.
            /// </param>
            public Visitor(HttpRequest request)
            {
                this.request = request;
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
                if (request.Headers.TryGetValue(httpHeader.ComponentName, out StringValues values))
                {
                    AddInput(httpHeader, values);
                }
                else
                {
                    throw new SignatureComponentMissingException(httpHeader);
                }
            }

            /// <inheritdoc/>
            public void Visit(HttpHeaderDictionaryStructuredComponent httpHeaderDictionary)
            {
                if (request.Headers.TryGetValue(httpHeaderDictionary.ComponentName, out StringValues values))
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
                    Constants.DerivedComponents.SignatureParams => throw new NotSupportedException("The '@signature-params' component cannot be included explicitly."),
                    Constants.DerivedComponents.Method => request.Method,
                    // TODO: Need to figure out a way to deal with reverse proxies changing paths, i.e. getting the original path/prefix.
                    Constants.DerivedComponents.TargetUri => $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}",
                    Constants.DerivedComponents.Authority => request.Host.Value.ToLower(),
                    Constants.DerivedComponents.Scheme => request.Scheme.ToLower(),
                    // TODO: Need to figure out a way to deal with reverse proxies changing paths, i.e. getting the original path/prefix.
                    Constants.DerivedComponents.RequestTarget => $"{request.PathBase}{request.Path}{request.QueryString}",
                    // TODO: Need to figure out a way to deal with reverse proxies changing paths, i.e. getting the original path/prefix.
                    Constants.DerivedComponents.Path => $"{request.PathBase}{request.Path}",
                    Constants.DerivedComponents.Query => request.QueryString.Value,
                    Constants.DerivedComponents.QueryParams => throw new NotSupportedException("The '@query-params' component must have the 'name' parameter set."),
                    Constants.DerivedComponents.Status => throw new NotSupportedException("The '@status' component cannot be included in request signatures."),
                    Constants.DerivedComponents.RequestResponse => throw new NotSupportedException("The '@request-response' component must have the 'key' parameter set."),

                    _ => throw new NotSupportedException($"Non-standard derived signature component '{derivedComponent.ComponentName}' cannot be retrieved."),
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
                if (!request.Query.TryGetValue(queryParams.Name, out StringValues values))
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
                throw new NotSupportedException();
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

            #endregion
        }
    }
}
