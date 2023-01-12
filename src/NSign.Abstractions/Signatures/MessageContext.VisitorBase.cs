using System;
using System.Collections.Generic;
using System.Linq;

namespace NSign.Signatures
{
    partial class MessageContext
    {
        /// <summary>
        /// Implements the ISignatureComponentVisitor interface as a base class for MessageContext-related inspection.
        /// </summary>
        private abstract class VisitorBase : ISignatureComponentVisitor
        {
            /// <summary>
            /// The MessageContext defining the context for this visitor.
            /// </summary>
            protected readonly MessageContext context;

            /// <summary>
            /// Initializes a new instance of VisitorBase.
            /// </summary>
            /// <param name="context">
            /// The MessageContext defining the context for this visitor.
            /// </param>
            protected VisitorBase(MessageContext context)
            {
                this.context = context;
            }

            /// <inheritdoc/>
            public virtual void Visit(SignatureComponent component)
            {
                throw new NotSupportedException(
                    $"Custom classes derived from SignatureComponent are not supported; component '{component.ComponentName}'.");
            }

            /// <inheritdoc/>
            public abstract void Visit(HttpHeaderComponent httpHeader);

            /// <inheritdoc/>
            public abstract void Visit(HttpHeaderDictionaryStructuredComponent httpHeaderDictionary);

            /// <inheritdoc/>
            public abstract void Visit(HttpHeaderStructuredFieldComponent httpHeaderStructuredField);

            /// <inheritdoc/>
            public abstract void Visit(DerivedComponent derived);

            /// <inheritdoc/>
            public abstract void Visit(SignatureParamsComponent signatureParams);

            /// <inheritdoc/>
            public abstract void Visit(QueryParamComponent queryParam);

            /// <summary>
            /// Tries to get the values of the given header or trailer.
            /// </summary>
            /// <param name="fromTrailers">
            /// A flag which indicates whether to get the value from a trailer field.
            /// </param>
            /// <param name="bindRequest">
            /// A flag which indicates whether the header or trailer values should be taken from the request message
            /// when the context is for a response message.
            /// </param>
            /// <param name="fieldName">
            /// The name of the header or trailer field to get the values from.
            /// </param>
            /// <param name="values">
            /// If the header or trailer exists, is updated with the values of the field.
            /// </param>
            /// <returns>
            /// True if the header or trailer exists, or false otherwise.
            /// </returns>
            protected bool TryGetHeaderOrTrailerValues(
                bool fromTrailers,
                bool bindRequest,
                string fieldName,
                out IEnumerable<string> values)
            {
                if (fromTrailers)
                {
                    return TryGetTrailerValues(bindRequest, fieldName, out values);
                }
                else
                {
                    return TryGetHeaderValues(bindRequest, fieldName, out values);
                }
            }

            #region Headers

            /// <summary>
            /// Tries to get the values of the given header.
            /// </summary>
            /// <param name="bindRequest">
            /// A flag which indicates whether or not the header values should be taken from the request message when
            /// the context is for a response message.
            /// </param>
            /// <param name="headerName">
            /// The name of the header to get the values for.
            /// </param>
            /// <param name="values">
            /// If the header exists, is updated with the values of the header.
            /// </param>
            /// <returns>
            /// True if the header exists, or false otherwise.
            /// </returns>
            protected bool TryGetHeaderValues(bool bindRequest, string headerName, out IEnumerable<string> values)
            {
                if (bindRequest)
                {
                    return TryGetRequestHeaderValues(headerName, out values);
                }
                else
                {
                    return TryGetHeaderValues(headerName, out values);
                }
            }

            /// <summary>
            /// Tries to get the message header values for the header with the given <paramref name="headerName"/>.
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
            protected bool TryGetHeaderValues(string headerName, out IEnumerable<string> values)
            {
                values = context.GetHeaderValues(headerName);
                return values.Any();
            }

            /// <summary>
            /// Tries to get the request message header values for the header with the given <paramref name="headerName"/>.
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
            protected bool TryGetRequestHeaderValues(string headerName, out IEnumerable<string> values)
            {
                values = context.GetRequestHeaderValues(headerName);
                return values.Any();
            }

            #endregion

            #region Trailers

            /// <summary>
            /// Tries to get the values of the given trailer field.
            /// </summary>
            /// <param name="bindRequest">
            /// A flag which indicates whether or not the trailer values should be taken from the request message when
            /// the context is for a response message.
            /// </param>
            /// <param name="fieldName">
            /// The name of the trailer field to get the values for.
            /// </param>
            /// <param name="values">
            /// If the trailer exists, is updated with the values of the trailer.
            /// </param>
            /// <returns>
            /// True if the trailer exists, or false otherwise.
            /// </returns>
            protected bool TryGetTrailerValues(bool bindRequest, string fieldName, out IEnumerable<string> values)
            {
                if (bindRequest)
                {
                    return TryGetRequestTrailerValues(fieldName, out values);
                }
                else
                {
                    return TryGetTrailerValues(fieldName, out values);
                }
            }

            /// <summary>
            /// Tries to get the message trailer values for the trailer with the given <paramref name="fieldName"/>.
            /// </summary>
            /// <param name="fieldName">
            /// The name of the trailer to get the values for.
            /// </param>
            /// <param name="values">
            /// If the trailer exists, is updated with the values of the trailer.
            /// </param>
            /// <returns>
            /// True if the trailer exists, or false otherwise.
            /// </returns>
            protected bool TryGetTrailerValues(string fieldName, out IEnumerable<string> values)
            {
                values = context.GetTrailerValues(fieldName);
                return values.Any();
            }

            /// <summary>
            /// Tries to get the request message trailer values for the trailer with the given <paramref name="fieldName"/>.
            /// </summary>
            /// <param name="fieldName">
            /// The name of the trailer to get the values for.
            /// </param>
            /// <param name="values">
            /// If the trailer exists, is updated with the values of the trailer.
            /// </param>
            /// <returns>
            /// True if the trailer exists, or false otherwise.
            /// </returns>
            protected bool TryGetRequestTrailerValues(string fieldName, out IEnumerable<string> values)
            {
                values = context.GetRequestTrailerValues(fieldName);
                return values.Any();
            }

            #endregion
        }
    }
}
