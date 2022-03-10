using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NSign.Signatures;
using StructuredFieldValues;
using System;
using System.Collections.Generic;

namespace NSign.AspNetCore
{
    partial class HttpContextExtensions
    {
        /// <summary>
        /// Implements the InputVisitorBase class to assist with checking for existence of signature components.
        /// </summary>
        private sealed class InputCheckingVisitor : InputVisitorBase
        {
            /// <summary>
            /// Initializes a new instance of InputCheckingVisitor.
            /// </summary>
            /// <param name="context">
            /// The HttpContext defining the context for this visitor.
            /// </param>
            public InputCheckingVisitor(HttpContext context) : base(context) { }

            /// <summary>
            /// Gets or sets a flag which indicates whether or not all the tested components were found.
            /// </summary>
            public bool Found { get; private set; } = true;

            /// <inheritdoc/>
            public override void Visit(SignatureComponent component)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override void Visit(HttpHeaderComponent httpHeader)
            {
                if (Found)
                {
                    Found &= TryGetHeaderValues(httpHeader.ComponentName, out _);
                }
            }

            /// <inheritdoc/>
            public override void Visit(HttpHeaderDictionaryStructuredComponent httpHeaderDictionary)
            {
                if (Found)
                {
                    Found &=
                        TryGetHeaderValues(httpHeaderDictionary.ComponentName, out StringValues values) &&
                        HasKey(values, httpHeaderDictionary.Key);
                }
            }

            /// <inheritdoc/>
            public override void Visit(DerivedComponent derived)
            {
                if (!Found)
                {
                    return;
                }

                switch (derived.ComponentName)
                {
                    case Constants.DerivedComponents.Method:
                    case Constants.DerivedComponents.TargetUri:
                    case Constants.DerivedComponents.Authority:
                    case Constants.DerivedComponents.Scheme:
                    case Constants.DerivedComponents.RequestTarget:
                    case Constants.DerivedComponents.Path:
                    case Constants.DerivedComponents.Query:
                    case Constants.DerivedComponents.Status:
                        break;

                    case Constants.DerivedComponents.SignatureParams:
                    case Constants.DerivedComponents.QueryParams:
                    case Constants.DerivedComponents.RequestResponse:
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
            public override void Visit(QueryParamsComponent queryParams)
            {
                if (!Found)
                {
                    return;
                }

                Found &= context.Request.Query.TryGetValue(queryParams.Name, out _);
            }

            /// <inheritdoc/>
            public override void Visit(RequestResponseComponent requestResponse)
            {
                if (Found)
                {
                    // We need to check the 'signature' header on the related _request_ message
                    Found &=
                        context.Request.Headers.TryGetValue(Constants.Headers.Signature, out StringValues values) &&
                        HasKey(values, requestResponse.Key);
                }
            }

            /// <summary>
            /// Checks if the given set of values for a structured dictionary header has an entry for the given
            /// <paramref name="key"/>.
            /// </summary>
            /// <param name="structuredDictValues">
            /// An <see cref="StringValues"/> object defining all the values for the header.
            /// </param>
            /// <param name="key">
            /// A string value that represents the key to look for.
            /// </param>
            /// <returns>
            /// True if the key is found, or false otherwise.
            /// </returns>
            private static bool HasKey(StringValues structuredDictValues, string key)
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
