namespace NSign.Signatures
{
    /// <summary>
    /// Defines the interface for a component used as input in HTTP message signatures.
    /// </summary>
    public interface ISignatureComponent
    {
        /// <summary>
        /// Gets a SignatureComponentType value defining the type of the signature component.
        /// </summary>
        SignatureComponentType Type { get; }

        /// <summary>
        /// Gets a string representing the signature component's name.
        /// </summary>
        string ComponentName { get; }

        /// <summary>
        /// Gets the original (i.e. parsed from the message header) identifier string for this component. This <strong>must
        /// not</strong> be set except by the parser when it reads this from an input source. This is mainly used for
        /// making sure that we can reconstruct the identifiers for the signature input exactly as they were provided
        /// in the source header, including order of parameters and all that.
        /// </summary>
        string? OriginalIdentifier { get; }

        /// <summary>
        /// Gets a flag which indicates whether or not this component's value should be taken from the request message
        /// associated with the response message the signature component is from or for. This represents the <c>req</c>
        /// flag from the standard.
        /// </summary>
        bool BindRequest { get; }
    }
}
