﻿using NSign.Signatures;
using StructuredFieldValues;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Web;

namespace NSign.Client
{
    partial class HttpRequestMessageExtensions
    {
        /// <summary>
        /// Implements the InputVisitorBase class to assist with checking for existence of signature components.
        /// </summary>
        private sealed class InputCheckingVisitor : InputVisitorBase
        {
            /// <summary>
            /// Initializes a new instance of InputCheckingVisitor.
            /// </summary>
            /// <param name="request">
            /// The HttpRequestMessage defining the context for this visitor.
            /// </param>
            public InputCheckingVisitor(HttpRequestMessage request) : base(request) { }

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
                        TryGetHeaderValues(httpHeaderDictionary.ComponentName, out IEnumerable<string> values) &&
                        HasKey(values, httpHeaderDictionary.Key);
                }
            }

            /// <inheritdoc/>
            public override void Visit(SpecialtyComponent specialityComponent)
            {
                if (!Found)
                {
                    return;
                }

                switch (specialityComponent.ComponentName)
                {
                    case Constants.SpecialtyComponents.Method:
                    case Constants.SpecialtyComponents.TargetUri:
                    case Constants.SpecialtyComponents.Authority:
                    case Constants.SpecialtyComponents.Scheme:
                    case Constants.SpecialtyComponents.RequestTarget:
                    case Constants.SpecialtyComponents.Path:
                    case Constants.SpecialtyComponents.Query:
                        break;

                    case Constants.SpecialtyComponents.SignatureParams:
                    case Constants.SpecialtyComponents.QueryParams:
                    case Constants.SpecialtyComponents.Status:
                    case Constants.SpecialtyComponents.RequestResponse:
                        throw new NotSupportedException(
                            $"Speciality component '{specialityComponent.ComponentName}' must be added through the corresponding class.");

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

                NameValueCollection query = HttpUtility.ParseQueryString(request.RequestUri.Query);
                string[] values = query.GetValues(queryParams.Name);

                Found &= null != values;
            }

            /// <inheritdoc/>
            public override void Visit(RequestResponseComponent requestResponse)
            {
                throw new NotSupportedException();
            }

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
