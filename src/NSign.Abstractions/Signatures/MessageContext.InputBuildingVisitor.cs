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
            /// The parameter to use to indicate that a HTTP field is bound to the original byte sequence.
            /// </summary>
            private const string ParamByteSequence = ";" + Constants.ComponentParameters.ByteSequence;

            /// <summary>
            /// The parameter to use to indicate that a HTTP field is to be taken from the trailers.
            /// </summary>
            private const string ParamFromTrailers = ";" + Constants.ComponentParameters.FromTrailers;

            /// <summary>
            /// The parameter to use to indicate that a HTTP field is to be serialized as a structured field.
            /// </summary>
            private const string ParamStructuredField = ";" + Constants.ComponentParameters.StructuredField;

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
                bool fromTrailers = httpHeader.FromTrailers;
                bool bindRequest = httpHeader.BindRequest;
                string fieldName = httpHeader.ComponentName;

                if (TryGetHeaderOrTrailerValues(fromTrailers, bindRequest, fieldName, out IEnumerable<string> values))
                {
                    if (httpHeader.UseByteSequence)
                    {
                        AddByteSequenceInput(httpHeader, values);
                    }
                    else
                    {
                        AddInput(httpHeader, String.Join(", ", values));
                    }
                }
                else
                {
                    throw new SignatureComponentMissingException(httpHeader);
                }
            }

            /// <inheritdoc/>
            public override void Visit(HttpHeaderDictionaryStructuredComponent httpHeaderDictionary)
            {
                bool fromTrailers = httpHeaderDictionary.FromTrailers;
                bool bindRequest = httpHeaderDictionary.BindRequest;
                string fieldName = httpHeaderDictionary.ComponentName;

                if (TryGetHeaderOrTrailerValues(fromTrailers, bindRequest, fieldName, out IEnumerable<string> values) &&
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
            public override void Visit(HttpHeaderStructuredFieldComponent httpHeaderStructuredField)
            {
                bool fromTrailers = httpHeaderStructuredField.FromTrailers;
                bool bindRequest = httpHeaderStructuredField.BindRequest;
                string fieldName = httpHeaderStructuredField.ComponentName;

                if (TryGetHeaderOrTrailerValues(fromTrailers, bindRequest, fieldName, out IEnumerable<string> values))
                {
                    // Let's look up the type of the structured field so we know how to parse it.
                    if (!context.HttpFieldOptions.StructuredFieldsMap.TryGetValue(fieldName, out StructuredFieldType type))
                    {
                        throw new UnknownStructuredFieldComponentException(httpHeaderStructuredField);
                    }

                    if (!type.TryParseStructuredFieldValue(values, out StructuredFieldValue structuredValue))
                    {
                        throw new StructuredFieldParsingException(fieldName, type);
                    }

                    AddInput(httpHeaderStructuredField, structuredValue.Serialize());
                }
                else
                {
                    throw new SignatureComponentMissingException(httpHeaderStructuredField);
                }
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
                        string prefix = $"\"{component.ComponentName}\"";

                        if (component.BindRequest)
                        {
                            prefix += ParamBindRequest;
                        }

                        if (component is HttpHeaderComponent headerComponent)
                        {
                            if (headerComponent.UseByteSequence)
                            {
                                prefix += ParamByteSequence;
                            }

                            if (headerComponent.FromTrailers)
                            {
                                prefix += ParamFromTrailers;
                            }
                        }

                        if (component is ISignatureComponentWithKey componentWithKey)
                        {
                            sb.Append($"{prefix};{Constants.ComponentParameters.Key}=\"{componentWithKey.Key}\"");
                        }
                        else if (component is ISignatureComponentWithName componentWithName)
                        {
                            sb.Append($"{prefix};{Constants.ComponentParameters.Name}=\"{componentWithName.Name}\"");
                        }
                        else if (component is HttpHeaderStructuredFieldComponent)
                        {
                            sb.Append($"{prefix}{ParamStructuredField}");
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

                if (null != signatureParamsComponent.Tag)
                {
                    sb.Append($";{Constants.SignatureParams.Tag}=\"{signatureParamsComponent.Tag}\"");
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

                    if (component is HttpHeaderComponent headerComponent)
                    {
                        if (headerComponent.FromTrailers)
                        {
                            suffix += ParamFromTrailers;
                        }

                        if (component is HttpHeaderStructuredFieldComponent)
                        {
                            suffix += ParamStructuredField;
                        }
                    }

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

                    if (component is HttpHeaderComponent headerComponent && headerComponent.FromTrailers)
                    {
                        suffix += ParamFromTrailers;
                    }

                    AddInput(
                        $"\"{component.ComponentName}\"{suffix};{Constants.ComponentParameters.Key}=\"{component.Key}\"",
                        value);
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
            /// Adds a line to the signature input for the given HTTP field with byte-sequence encoding.
            /// </summary>
            /// <param name="component">
            /// The <see cref="HttpHeaderComponent"/> for which to add the values as byte sequences.
            /// </param>
            /// <param name="values">
            /// An <see cref="IEnumerable{T}"/> of string values representing the HTTP field's values.
            /// </param>
            private void AddByteSequenceInput(HttpHeaderComponent component, IEnumerable<string> values)
            {
                // Build the byte sequence list for the value of the input.
                StringBuilder builder = new StringBuilder();
                foreach (string value in values)
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(GetByteSequence(value));
                }

                // Now add the encoded values to the input.
                if (null == component.OriginalIdentifier)
                {
                    // The order of the parameters is relevant; it must match the order used when building the
                    // '@signature-params' component (see BuildSignatureParamsComponentValueAndVisitComponents). If
                    // they are not, signatures cannot correctly be validated, because input cannot be reconstruted.
                    string suffix = component.BindRequest ? ParamBindRequest : String.Empty;

                    suffix += ParamByteSequence;

                    if (component.FromTrailers)
                    {
                        suffix += ParamFromTrailers;
                    }

                    AddInput($"\"{component.ComponentName}\"{suffix}", builder.ToString());
                }
                else
                {
                    AddInput(component.OriginalIdentifier, builder.ToString());
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

            /// <summary>
            /// Get the ASCII byte-sequence for the given input string.
            /// </summary>
            /// <param name="value">
            /// The value to encode as byte-sequence.
            /// </param>
            /// <returns>
            /// A string value that represents the byte-sequence encoded value.
            /// </returns>
            private static string GetByteSequence(string value)
            {
                value = value.Trim();
                int byteLen = Encoding.ASCII.GetByteCount(value);

                if (byteLen <= 1024)
                {
                    Span<byte> buffer = stackalloc byte[byteLen];
                    byteLen = Encoding.ASCII.GetBytes(value, buffer);

                    return ((ReadOnlySpan<byte>)buffer).SerializeAsString();
                }
                else
                {
                    ReadOnlyMemory<byte> buffer = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes(value));

                    return buffer.SerializeAsString();
                }
            }

            #endregion
        }
    }
}
