namespace NSign.Signatures
{
    /// <summary>
    /// Defines the interface for a dictionary-structured component with a 'key' parameter.
    /// </summary>
    public interface ISignatureComponentWithKey : ISignatureComponent
    {
        /// <summary>
        /// Gets the value to use for the 'key' parameter.
        /// </summary>
        string Key { get; }
    }
}
