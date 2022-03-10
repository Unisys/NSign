using NSign.Signatures;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text;
using System.Web;

namespace NSign.Client
{
    /// <summary>
    /// Extensions for HttpRequestMessage objects.
    /// </summary>
    internal static partial class HttpRequestMessageExtensions
    {
        /// <summary>
        /// Checks if the given <paramref name="component"/> exists on the specified <paramref name="request"/>.
        /// </summary>
        /// <param name="request">
        /// The HttpRequestMessage to check for existence of the component.
        /// </param>
        /// <param name="component">
        /// The SignatureComponent to check.
        /// </param>
        /// <returns>
        /// True if the SignatureComponent exists (is valid) on the given request, or false otherwise.
        /// </returns>
        public static bool HasSignatureComponent(this HttpRequestMessage request, SignatureComponent component)
        {
            InputCheckingVisitor visitor = new InputCheckingVisitor(request);

            component.Accept(visitor);

            return visitor.Found;
        }

        /// <summary>
        /// Gets a byte array which serves as input for signing.
        /// </summary>
        /// <param name="request">
        /// The HttpRequestMessage to use for specific signature input.
        /// </param>
        /// <param name="inputSpec">
        /// The SignatureInputSpec value that defines the spec of the input for the signature.
        /// </param>
        /// <param name="signatureParamsValue">
        /// If successful, is updated with the string that represents the full signature input.
        /// </param>
        /// <returns>
        /// A byte array representing the signature input for signing.
        /// </returns>
        public static byte[] GetSignatureInput(
            this HttpRequestMessage request,
            SignatureInputSpec inputSpec,
            out string signatureParamsValue)
        {
            InputBuildingVisitor visitor = new InputBuildingVisitor(request);

            inputSpec.SignatureParameters.Accept(visitor);
            signatureParamsValue = visitor.SignatureParamsValue;

            return Encoding.ASCII.GetBytes(visitor.SignatureInput);
        }

        /// <summary>
        /// Gets the value of the given <paramref name="derivedComponent"/> for the specified <paramref name="request"/>.
        /// </summary>
        /// <param name="request">
        /// The <see cref="HttpRequestMessage"/> object for which the derived component's value should be retrieved.
        /// </param>
        /// <param name="derivedComponent">
        /// The <see cref="DerivedComponent"/> specifying which value to retrieve.
        /// </param>
        /// <returns>
        /// A string that represents the requested value.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown for unsupported derived components. This includes e.g. the '@query-params' component which has dedicated
        /// logic for value retrieval, or the '@status' or '@request-response' components which are not support for
        /// request messages in the first place.
        /// </exception>
        public static string GetDerivedComponentValue(this HttpRequestMessage request, DerivedComponent derivedComponent)
        {
            return derivedComponent.ComponentName switch
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
                    throw new NotSupportedException(
                        $"Non-standard derived signature component '{derivedComponent.ComponentName}' cannot be retrieved."),
            };
        }

        /// <summary>
        /// Gets the values for the given query parameter from the given request.
        /// </summary>
        /// <param name="request">
        /// The HttpRequestMessage from which to get the values.
        /// </param>
        /// <param name="queryParams">
        /// The QueryParamsComponent component which defines which query parameter values to get.
        /// </param>
        /// <returns>
        /// An array of string values representing the values or null if there's no such parameter.
        /// </returns>
        public static string[] GetQueryParamValues(this HttpRequestMessage request, QueryParamsComponent queryParams)
        {
            NameValueCollection query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            string[] values = query.GetValues(queryParams.Name);

            return values;
        }

        /// <summary>
        /// Implements the ISignatureComponentVisitor interface as a base class for HttpRequestMessage-related inspection.
        /// </summary>
        private abstract class InputVisitorBase : ISignatureComponentVisitor
        {
            /// <summary>
            /// The HttpRequestMessage defining the context for this visitor.
            /// </summary>
            protected readonly HttpRequestMessage request;

            /// <summary>
            /// Initializes a new instance of InputVisitorBase.
            /// </summary>
            /// <param name="request">
            /// The HttpRequestMessage defining the context for this visitor.
            /// </param>
            protected InputVisitorBase(HttpRequestMessage request)
            {
                this.request = request;
            }

            /// <inheritdoc/>
            public abstract void Visit(SignatureComponent component);

            /// <inheritdoc/>
            public abstract void Visit(HttpHeaderComponent httpHeader);

            /// <inheritdoc/>
            public abstract void Visit(HttpHeaderDictionaryStructuredComponent httpHeaderDictionary);

            /// <inheritdoc/>
            public abstract void Visit(DerivedComponent derived);

            /// <inheritdoc/>
            public abstract void Visit(SignatureParamsComponent signatureParams);

            /// <inheritdoc/>
            public abstract void Visit(QueryParamsComponent queryParams);

            /// <inheritdoc/>
            public abstract void Visit(RequestResponseComponent requestResponse);

            /// <summary>
            /// Tries to get the request header values for the header with the given <paramref name="headerName"/>.
            /// </summary>
            /// <param name="headerName">
            /// The name of the header to get the values for.
            /// </param>
            /// <param name="values">
            /// If the header exists, is updated with the values of the header.
            /// </param>
            /// <returns>
            /// True if the header exists, or false otherwise.
            /// </returns>
            /// <remarks>
            /// This method also checks the headers on the content of the request, provided it is set. It also emulates
            /// synthetic headers like 'content-length' which are in some cases calculated based on the content.
            /// </remarks>
            protected bool TryGetHeaderValues(string headerName, out IEnumerable<string> values)
            {
                if (request.Headers.TryGetValues(headerName, out values))
                {
                    return true;
                }

                if (null == request.Content)
                {
                    return false;
                }

                if (request.Content.Headers.TryGetValues(headerName, out values))
                {
                    return true;
                }

                switch (headerName)
                {
                    case "content-length":
                        if (request.Content.Headers.ContentLength.HasValue)
                        {
                            values = new string[] { request.Content.Headers.ContentLength.Value.ToString(), };
                            return true;
                        }
                        else
                        {
                            values = null;
                            return false;
                        }

                    default:
                        return false;
                }
            }
        }
    }
}
