using System;
using System.Diagnostics;

namespace NSign.Signatures
{
    /// <summary>
    /// Represents the '@query-param' derived signature component for a specific query parameter.
    /// </summary>
    [DebuggerDisplay("Type={Type}, Component={ComponentName}, Name={Name}")]
    public sealed class QueryParamComponent : DerivedComponent, ISignatureComponentWithName, IEquatable<QueryParamComponent>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="QueryParamComponent"/>.
        /// </summary>
        /// <param name="name">
        /// The name of the query parameter to use with this component.
        /// </param>
        public QueryParamComponent(string name) : this(name, bindRequest: false) { }

        /// <summary>
        /// Initializes a new instance of <see cref="QueryParamComponent"/>.
        /// </summary>
        /// <param name="name">
        /// The name of the query parameter to use with this component.
        /// </param>
        /// <param name="bindRequest">
        /// Whether or not the component should be bound to the request. This represents the <c>req</c> flag from the
        /// standard.
        /// </param>
        public QueryParamComponent(string name, bool bindRequest)
            : base(Constants.DerivedComponents.QueryParam, bindRequest)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            // When the component is created by the parser, the name would have
            // to be percent encoded. So in order to work with the decoded name
            // we need to decode it here first.
            Name = Uri.UnescapeDataString(name);
        }

        #region ISignatureComponentWithName Implementation

        /// <inheritdoc/>
        public string Name { get; }

        #endregion

        /// <inheritdoc/>
        public override void Accept(ISignatureComponentVisitor visitor)
        {
            visitor.Visit(this);
        }

        #region IEquatable<QueryParamComponent> Implementation

        /// <inheritdoc/>
        public bool Equals(QueryParamComponent? other)
        {
            if (null == other)
            {
                return false;
            }
            else if (ReferenceEquals(this, other))
            {
                return true;
            }

            return BindRequest == other.BindRequest &&
                StringComparer.OrdinalIgnoreCase.Equals(Name, other.Name);
        }

        #endregion

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is QueryParamComponent queryParam)
            {
                return base.Equals(queryParam);
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Name.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{ComponentName};name={Name}";
        }
    }
}
