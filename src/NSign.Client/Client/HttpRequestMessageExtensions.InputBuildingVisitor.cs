using NSign.Http;
using NSign.Signatures;
using StructuredFieldValues;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text;
using System.Web;

namespace NSign.Client
{
    partial class HttpRequestMessageExtensions
    {
        /// <summary>
        /// Implements the InputVisitorBase class to assist with building strings for signature input.
        /// </summary>
        private sealed class InputBuildingVisitor : InputVisitorBase
        {
            /// <summary>
            /// The StringBuilder which builds the full signature input to be used.
            /// </summary>
            private readonly StringBuilder signatureInput = new StringBuilder();

            /// <summary>
            /// Initializes a new instance of InputBuildingVisitor.
            /// </summary>
            /// <param name="request">
            /// The HttpRequestMessage defining the context for this visitor.
            /// </param>
            public InputBuildingVisitor(HttpRequestMessage request) : base(request) { }

            /// <summary>
            /// The signature input as built by the visitor.
            /// </summary>
            public string SignatureInput => signatureInput.ToString();

            /// <summary>
            /// The value for the '@signature-params' component, as built by the visitor.
            /// </summary>
            public string SignatureParamsValue { get; private set; }

            /// <inheritdoc/>
            public override void Visit(SignatureComponent component)
            {
                throw new NotSupportedException(
                    $"Custom classes derived from SignatureComponent are not supported; component '{component.ComponentName}'.");
            }

            /// <inheritdoc/>
            public override void Visit(HttpHeaderComponent httpHeader)
            {
                if (TryGetHeaderValues(httpHeader.ComponentName, out IEnumerable<string> values))
                {
                    AddInput(httpHeader, String.Join(", ", values));
                }
                else
                {
                    throw new SignatureComponentMissingException(httpHeader);
                }
            }

            /// <inheritdoc/>
            public override void Visit(HttpHeaderDictionaryStructuredComponent httpHeaderDictionary)
            {
                if (TryGetHeaderValues(httpHeaderDictionary.ComponentName, out IEnumerable<string> values))
                {
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
            public override void Visit(DerivedComponent derived)
            {
                string value = derived.ComponentName switch
                {
                    Constants.DerivedComponents.SignatureParams =>
                        throw new NotSupportedException("The '@signature-params' component cannot be included explicitly."),
                    Constants.DerivedComponents.Method => request.Method.Method,
                    Constants.DerivedComponents.TargetUri => request.RequestUri.OriginalString,
                    Constants.DerivedComponents.Authority => request.RequestUri.Authority.ToLower(),
                    Constants.DerivedComponents.Scheme => request.RequestUri.Scheme.ToLower(),
                    Constants.DerivedComponents.RequestTarget => request.RequestUri.PathAndQuery,
                    Constants.DerivedComponents.Path => request.RequestUri.AbsolutePath,
                    Constants.DerivedComponents.Query =>
                        String.IsNullOrWhiteSpace(request.RequestUri.Query) ?
                            "?" : request.RequestUri.Query,
                    Constants.DerivedComponents.QueryParams =>
                        throw new NotSupportedException("The '@query-params' component must have the 'name' parameter set."),
                    Constants.DerivedComponents.Status =>
                        throw new NotSupportedException("The '@status' component cannot be included in request signatures."),
                    Constants.DerivedComponents.RequestResponse =>
                        throw new NotSupportedException("The '@request-response' component must have the 'key' parameter set."),

                    _ =>
                        throw new NotSupportedException($"Non-standard derived signature component '{derived.ComponentName}' cannot be retrieved."),
                };

                AddInput(derived, value);
            }

            /// <inheritdoc/>
            public override void Visit(SignatureParamsComponent signatureParamsComponent)
            {
                StringBuilder sb = new StringBuilder("(");
                int fieldId = 0;

                foreach (SignatureComponent component in signatureParamsComponent.Components)
                {
                    component.Accept(this);

                    if (0 < fieldId++)
                    {
                        sb.Append(' ');
                    }

                    if (component is ISignatureComponentWithKey componentWithKey)
                    {
                        sb.Append($"\"{component.ComponentName}\";{Constants.ComponentParameters.Key}=\"{componentWithKey.Key}\"");
                    }
                    else if (component is ISignatureComponentWithName componentWithName)
                    {
                        sb.Append($"\"{component.ComponentName}\";{Constants.ComponentParameters.Name}=\"{componentWithName.Name}\"");
                    }
                    else
                    {
                        sb.Append($"\"{component.ComponentName}\"");
                    }
                }

                sb.Append(')');

                if (signatureParamsComponent.Created.HasValue)
                {
                    sb.Append($";{Constants.SignatureParams.Created}={signatureParamsComponent.Created.Value.ToUnixTimeSeconds()}");
                }

                if (signatureParamsComponent.Expires.HasValue)
                {
                    sb.Append($";{Constants.SignatureParams.Expires}={signatureParamsComponent.Expires.Value.ToUnixTimeSeconds()}");
                }

                if (null != signatureParamsComponent.Nonce)
                {
                    sb.Append($";{Constants.SignatureParams.Nonce}=\"{signatureParamsComponent.Nonce}\"");
                }

                if (null != signatureParamsComponent.Algorithm)
                {
                    sb.Append($";{Constants.SignatureParams.Alg}=\"{signatureParamsComponent.Algorithm}\"");
                }

                if (!String.IsNullOrWhiteSpace(signatureParamsComponent.KeyId))
                {
                    sb.Append($";{Constants.SignatureParams.KeyId}=\"{signatureParamsComponent.KeyId}\"");
                }

                SignatureParamsValue = sb.ToString();
                AddInput(signatureParamsComponent, SignatureParamsValue);
            }

            /// <inheritdoc/>
            public override void Visit(QueryParamsComponent queryParams)
            {
                NameValueCollection query = HttpUtility.ParseQueryString(request.RequestUri.Query);
                string[] values = query.GetValues(queryParams.Name);

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
            public override void Visit(RequestResponseComponent requestResponse)
            {
                throw new NotSupportedException();
            }

            #region Private Methods

            /// <summary>
            /// Adds a line to the signature input for the given component with the specified value.
            /// </summary>
            /// <param name="component">
            /// The SignatureComponent to add.
            /// </param>
            /// <param name="value">
            /// The component's value to use for the input.
            /// </param>
            private void AddInput(SignatureComponent component, string value)
            {
                AddInput(component.ComponentName, value);
            }

            /// <summary>
            /// Adds a line to the signature input for the given component with a key and the specified value.
            /// </summary>
            /// <param name="component">
            /// The ISignatureComponentWithKey to add.
            /// </param>
            /// <param name="value">
            /// The component's value to use for the input.
            /// </param>
            private void AddInputWithKey(ISignatureComponentWithKey component, string value)
            {
                AddInput($"{component.ComponentName}\";{Constants.ComponentParameters.Key}=\"{component.Key}", value);
            }

            /// <summary>
            /// Adds a line to the signature input for the given component with a name parameter and the specified value.
            /// </summary>
            /// <param name="component">
            /// The ISignatureComponentWithName to add.
            /// </param>
            /// <param name="value">
            /// The component's value to use for the input.
            /// </param>
            private void AddInputWithName(ISignatureComponentWithName component, string value)
            {
                AddInput($"{component.ComponentName}\";{Constants.ComponentParameters.Name}=\"{component.Name}", value);
            }

            /// <summary>
            /// Adds a line to the signature input for the given component with the specified value.
            /// </summary>
            /// <param name="componentSpec">
            /// The full name of the component to add, including any parameters needed.
            /// </param>
            /// <param name="value">
            /// The component's value to use for the input.
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
