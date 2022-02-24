using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NSign.Signatures;
using System.Text;

namespace NSign.AspNetCore
{
    /// <summary>
    /// Extensions for HttpContext objects.
    /// </summary>
    internal static partial class HttpContextExtensions
    {
        /// <summary>
        /// Checks if the given <paramref name="component"/> exists on the specified <paramref name="context"/>.
        /// </summary>
        /// <param name="context">
        /// The HttpContext to check for existence of the component.
        /// </param>
        /// <param name="component">
        /// The SignatureComponent to check.
        /// </param>
        /// <returns>
        /// True if the SignatureComponent exists (is valid) on the given context, or false otherwise.
        /// </returns>
        public static bool HasSignatureComponent(this HttpContext context, SignatureComponent component)
        {
            InputCheckingVisitor visitor = new InputCheckingVisitor(context);

            component.Accept(visitor);

            return visitor.Found;
        }

        /// <summary>
        /// Gets a byte array which serves as input for signing.
        /// </summary>
        /// <param name="context">
        /// The HttpContext to use for specific signature input.
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
            this HttpContext context,
            SignatureInputSpec inputSpec,
            out string signatureParamsValue)
        {
            InputBuildingVisitor visitor = new InputBuildingVisitor(context);

            inputSpec.SignatureParameters.Accept(visitor);
            signatureParamsValue = visitor.SignatureParamsValue;

            return Encoding.ASCII.GetBytes(visitor.SignatureInput);
        }

        /// <summary>
        /// Implements the ISignatureComponentVisitor interface as a base class for HttpContext-related inspection, e.g.
        /// for signing of HTTP response messages.
        /// </summary>
        private abstract class InputVisitorBase : ISignatureComponentVisitor
        {
            /// <summary>
            /// The HttpContext defining the context for this visitor.
            /// </summary>
            protected readonly HttpContext context;

            /// <summary>
            /// Initializes a new instance of InputVisitorBase.
            /// </summary>
            /// <param name="context">
            /// The HttpContext defining the context for this visitor.
            /// </param>
            protected InputVisitorBase(HttpContext context)
            {
                this.context = context;
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
            /// Tries to get the response header values for the header with the given <paramref name="headerName"/>.
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
            /// This method also checks the headers on the content of the response, provided it is set. It also emulates
            /// synthetic headers like 'content-length' which are in some cases calculated based on the content.
            /// </remarks>
            protected bool TryGetHeaderValues(string headerName, out StringValues values)
            {
                if (context.Response.Headers.TryGetValue(headerName, out values))
                {
                    return true;
                }

                switch (headerName)
                {
                    case "content-length":
                        if (context.Response.Headers.ContentLength.HasValue)
                        {
                            values = new StringValues(context.Response.Headers.ContentLength.Value.ToString());
                            return true;
                        }
                        break;
                }

                return false;
            }
        }
    }
}
