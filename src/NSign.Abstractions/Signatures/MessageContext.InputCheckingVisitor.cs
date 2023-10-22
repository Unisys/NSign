using StructuredFieldValues;
using System;
using System.Collections.Generic;

namespace NSign.Signatures
{
    partial class MessageContext
    {
        /// <summary>
        /// Implements the InputVisitorBase class to assist with checking for existence of signature components.
        /// </summary>
        private sealed class InputCheckingVisitor : VisitorBase, ISignatureComponentCheckVisitor
        {
            /// <summary>
            /// Initializes a new instance of InputCheckingVisitor.
            /// </summary>
            /// <param name="context">
            /// The MessageContext defining the context for this visitor.
            /// </param>
            public InputCheckingVisitor(MessageContext context) : base(context) { }

            /// <summary>
            /// Gets or sets a flag which indicates whether or not all the tested components were found.
            /// </summary>
            public bool Found { get; private set; } = true;

            /// <inheritdoc/>
            public override void Visit(HttpHeaderComponent httpHeader)
            {
                if (httpHeader.FromTrailers)
                {
                    Found &= context.HasTrailer(httpHeader.BindRequest, httpHeader.ComponentName);
                }
                else
                {
                    Found &= context.HasHeader(httpHeader.BindRequest, httpHeader.ComponentName);
                }
            }

            /// <inheritdoc/>
            public override void Visit(HttpHeaderDictionaryStructuredComponent httpHeaderDictionary)
            {
                bool fromTrailers = httpHeaderDictionary.FromTrailers;
                bool bindRequest = httpHeaderDictionary.BindRequest;
                string fieldName = httpHeaderDictionary.ComponentName;

                Found &=
                    TryGetHeaderOrTrailerValues(fromTrailers, bindRequest, fieldName, out IEnumerable<string> values) &&
                    HasKey(values, httpHeaderDictionary.Key);
            }

            /// <inheritdoc/>
            public override void Visit(HttpHeaderStructuredFieldComponent httpHeaderStructuredField)
            {
                // Assume that the header value is a proper structured field, so we can leave the check to the normal
                // check for HttpHeaderComponent.
                Visit((HttpHeaderComponent)httpHeaderStructuredField);
            }

            /// <inheritdoc/>
            public override void Visit(DerivedComponent derived)
            {
                switch (derived.ComponentName)
                {
                    case Constants.DerivedComponents.Method:
                    case Constants.DerivedComponents.TargetUri:
                    case Constants.DerivedComponents.Authority:
                    case Constants.DerivedComponents.Scheme:
                    case Constants.DerivedComponents.RequestTarget:
                    case Constants.DerivedComponents.Path:
                    case Constants.DerivedComponents.Query:
                        break;

                    case Constants.DerivedComponents.Status:
                        Found &= context.HasResponse;
                        break;

                    case Constants.DerivedComponents.SignatureParams:
                    case Constants.DerivedComponents.QueryParam:
                        throw new NotSupportedException(
                            $"Derived component '{derived.ComponentName}' must be added through the corresponding class.");

                    default:
                        Found = false;
                        break;
                }
            }

            /// <inheritdoc/>
            public override void Visit(SignatureParamsComponent signatureParamsComponent)
            {
                // Intentionally left blank: the @signature-params component is always present.
            }

            /// <inheritdoc/>
            public override void Visit(QueryParamComponent queryParam)
            {
                Found &= context.HasExactlyOneQueryParamValue(queryParam.Name);
            }

            /// <summary>
            /// Checks if the given set of values for a structured dictionary header has an entry for the given
            /// <paramref name="key"/>.
            /// </summary>
            /// <param name="structuredDictValues">
            /// An <see cref="IEnumerable{String}"/> object defining all the values for the header.
            /// </param>
            /// <param name="key">
            /// A string value that represents the key to look for.
            /// </param>
            /// <returns>
            /// True if the key is found, or false otherwise.
            /// </returns>
            private static bool HasKey(IEnumerable<string> structuredDictValues, string key)
            {
                foreach (string value in structuredDictValues)
                {
                    if (null == SfvParser.ParseDictionary(value, out IReadOnlyDictionary<string, ParsedItem> actualDict) &&
                        actualDict.TryGetValue(key, out _))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
