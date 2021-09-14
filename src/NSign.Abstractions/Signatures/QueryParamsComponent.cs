using System;
using System.Diagnostics;

namespace NSign.Signatures
{
    /// <summary>
    /// Represents the '@query-params' specialty signature component for a specific query parameter.
    /// </summary>
    [DebuggerDisplay("Type={Type}, Component={ComponentName}, Name={Name}")]
    public sealed class QueryParamsComponent : SpecialtyComponent, ISignatureComponentWithName, IEquatable<QueryParamsComponent>
    {
        /// <summary>
        /// Initializes a new instance of QueryParamsComponent.
        /// </summary>
        /// <param name="name">
        /// The name of the query parameter to use with this component.
        /// </param>
        public QueryParamsComponent(string name) : base(Constants.SpecialtyComponents.QueryParams)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name.ToLower();
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

        #region IEquatable<QueryParamsComponent> Implementation

        /// <inheritdoc/>
        public bool Equals(QueryParamsComponent other)
        {
            if (null == other)
            {
                return false;
            }
            else if (ReferenceEquals(this, other))
            {
                return true;
            }

            return StringComparer.OrdinalIgnoreCase.Equals(Name, other.Name);
        }

        #endregion

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return base.Equals(obj as QueryParamsComponent);
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
