using NSign.Http;
using StructuredFieldValues;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NSign.Signatures
{
    partial class MessageContext
    {
        /// <summary>
        /// Implements the VisitorBase class to assist with building strings for signature input.
        /// </summary>
        private sealed class InputBuildingVisitor : VisitorBase
        {
            /// <summary>
            /// The parameter to use to indicate that a component is bound to the request.
            /// </summary>
            private const string ParamBindRequest = ";" + Constants.ComponentParameters.Request;

            /// <summary>
            /// The StringBuilder which builds the full signature input to be used.
            /// </summary>
            private readonly StringBuilder signatureInput = new StringBuilder();

            /// <summary>
            /// Initializes a new instance of InputBuildingVisitor.
            /// </summary>
            /// <param name="context">
            /// The MessageContext defining the context for this visitor.
            /// </param>
            public InputBuildingVisitor(MessageContext context) : base(context) { }

            /// <summary>
            /// The signature input as built by the visitor.
            /// </summary>
            public string SignatureInput => signatureInput.ToString();

            /// <summary>
            /// The value for the '@signature-params' component, as built by the visitor.
            /// </summary>
            public string? SignatureParamsValue { get; private set; }

            /// <inheritdoc/>
            public override void Visit(HttpHeaderComponent httpHeader)
            {
                bool bindRequest = httpHeader.BindRequest;

                if (TryGetHeaderValues(bindRequest, httpHeader.ComponentName, out IEnumerable<string> values))
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
                bool bindRequest = httpHeaderDictionary.BindRequest;

                if (TryGetHeaderValues(bindRequest, httpHeaderDictionary.ComponentName, out IEnumerable<string> values) &&
                    values.TryGetStructuredDictionaryValue(httpHeaderDictionary.Key, out ParsedItem? lastValue))
                {
                    Debug.Assert(lastValue.HasValue, "lastValue must have a value.");

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
                string? value = derived.ComponentName switch
                {
                    Constants.DerivedComponents.SignatureParams =>
                        throw new NotSupportedException("The '@signature-params' component cannot be included explicitly."),
                    Constants.DerivedComponents.QueryParam =>
                        throw new NotSupportedException("The '@query-param' component must have the 'name' parameter set."),

                    _ => context.GetDerivedComponentValue(derived),
                };

                AddInput(derived, value!);
            }

            /// <inheritdoc/>
            public override void Visit(SignatureParamsComponent signatureParamsComponent)
            {
                if (null == signatureParamsComponent.OriginalValue)
                {
                    BuildSignatureParamsComponentValueAndVisitComponents(signatureParamsComponent);
                }
                else
                {
                    foreach (SignatureComponent component in signatureParamsComponent.Components)
                    {
                        context.EnsureComponentIsAllowed(component);
                        component.Accept(this);
                    }

                    SignatureParamsValue = signatureParamsComponent.OriginalValue;
                    AddInput(signatureParamsComponent, signatureParamsComponent.OriginalValue);
                }
            }

            /// <inheritdoc/>
            public override void Visit(QueryParamComponent queryParam)
            {
                IEnumerable<string> values = context.GetQueryParamValues(queryParam.Name);
                int numValues = 0;

                foreach (string value in values)
                {
                    numValues++;
                    AddInputWithName(queryParam, value);
                }

                if (0 >= numValues)
                {
                    throw new SignatureComponentMissingException(queryParam);
                }
            }

            #region Private Methods

            /// <summary>
            /// Builds the '@signature-params` component value from scratch.
            /// </summary>
            /// <param name="signatureParamsComponent">
            /// The <see cref="SignatureParamsComponent"/> component for which to build the value from scratch.
            /// </param>
            private void BuildSignatureParamsComponentValueAndVisitComponents(
                SignatureParamsComponent signatureParamsComponent)
            {
                Debug.Assert(null == signatureParamsComponent.OriginalValue,
                    "The '@signature-params' component's original value must be null.");

                StringBuilder sb = new StringBuilder("(");
                int fieldId = 0;

                foreach (SignatureComponent component in signatureParamsComponent.Components)
                {
                    context.EnsureComponentIsAllowed(component);
                    component.Accept(this);

                    if (0 < fieldId++)
                    {
                        sb.Append(' ');
                    }

                    if (null != component.OriginalIdentifier)
                    {
                        sb.Append(component.OriginalIdentifier);
                    }
                    else
                    {
                        string prefix = $"\"{component.ComponentName}\"{(component.BindRequest ? ParamBindRequest : "")}";

                        if (component is ISignatureComponentWithKey componentWithKey)
                        {
                            sb.Append($"{prefix};{Constants.ComponentParameters.Key}=\"{componentWithKey.Key}\"");
                        }
                        else if (component is ISignatureComponentWithName componentWithName)
                        {
                            sb.Append($"{prefix};{Constants.ComponentParameters.Name}=\"{componentWithName.Name}\"");
                        }
                        else
                        {
                            sb.Append(prefix);
                        }
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
                if (null == component.OriginalIdentifier)
                {
                    string suffix = component.BindRequest ? ParamBindRequest : String.Empty;
                    AddInput($"\"{component.ComponentName}\"{suffix}", value);
                }
                else
                {
                    AddInput(component.OriginalIdentifier, value);
                }
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
                if (null == component.OriginalIdentifier)
                {
                    string suffix = component.BindRequest ? ParamBindRequest : String.Empty;
                    AddInput($"\"{component.ComponentName}\"{suffix};{Constants.ComponentParameters.Key}=\"{component.Key}\"", value);
                }
                else
                {
                    AddInput(component.OriginalIdentifier, value);
                }
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
                if (null == component.OriginalIdentifier)
                {
                    string suffix = component.BindRequest ? ParamBindRequest : String.Empty;
                    AddInput($"\"{component.ComponentName}\"{suffix};{Constants.ComponentParameters.Name}=\"{component.Name}\"", value);
                }
                else
                {
                    AddInput(component.OriginalIdentifier, value);
                }
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

                signatureInput.Append($"{componentSpec}: {value}");
            }

            #endregion
        }
    }
}
