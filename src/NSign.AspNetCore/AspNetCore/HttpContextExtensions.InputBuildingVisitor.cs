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
    partial class HttpContextExtensions
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
            /// <param name="context">
            /// The HttpContext defining the context for this visitor.
            /// </param>
            public InputBuildingVisitor(HttpContext context) : base(context) { }

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
                if (TryGetHeaderValues(httpHeader.ComponentName, out StringValues values))
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
                if (TryGetHeaderValues(httpHeaderDictionary.ComponentName, out StringValues values) &&
                    TryGetDictValue(values, httpHeaderDictionary.Key, out ParsedItem? lastValue))
                {
                    AddInputWithKey(httpHeaderDictionary,
                                    lastValue.Value.Value.SerializeAsString() +
                                    lastValue.Value.Parameters.SerializeAsParameters());
                    return;
                }


                throw new SignatureComponentMissingException(httpHeaderDictionary);
            }

            /// <inheritdoc/>
            public override void Visit(DerivedComponent derived)
            {
                string value = derived.ComponentName switch
                {
                    Constants.DerivedComponents.Status => context.Response.StatusCode.ToString(),
                    Constants.DerivedComponents.SignatureParams => throw new NotSupportedException("The '@signature-params' component cannot be included explicitly."),
                    Constants.DerivedComponents.QueryParams => throw new NotSupportedException("The '@query-params' component must have the 'name' parameter set."),
                    Constants.DerivedComponents.RequestResponse => throw new NotSupportedException("The '@request-response' component must have the 'key' parameter set."),

                    _ => context.Request.GetDerivedComponentValue(derived),
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
                if (!context.Request.Query.TryGetValue(queryParams.Name, out StringValues values))
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
                if (context.Request.Headers.TryGetValue(Constants.Headers.Signature, out StringValues values) &&
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

            /// <summary>
            /// Tries to get a dictionary entry from a set of structured dictionary header values.
            /// </summary>
            /// <param name="values">
            /// The <see cref="StringValues"/> value representing all the values for the header.
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
            private static bool TryGetDictValue(StringValues values, string key, out ParsedItem? lastValue)
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
