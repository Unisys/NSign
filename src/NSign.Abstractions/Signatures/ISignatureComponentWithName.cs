namespace NSign.Signatures
{
    /// <summary>
    /// Defines the interface for a dictionary-structured component with a 'name' parameter.
    /// </summary>
    public interface ISignatureComponentWithName : ISignatureComponent
    {
        /// <summary>
        /// Gets the value to use for the 'name' parameter.
        /// </summary>
        string Name { get; }
    }
}
