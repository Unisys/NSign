using NSign.Signatures;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

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
