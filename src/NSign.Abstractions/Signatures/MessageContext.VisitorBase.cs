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
        }
    }
}
