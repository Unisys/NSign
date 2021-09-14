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
    }
}
