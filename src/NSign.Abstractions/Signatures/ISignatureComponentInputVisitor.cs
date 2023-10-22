namespace NSign.Signatures
{
    /// <summary>
    /// Contract for a SignatureComponent visitor that should build the signature input document.
    /// </summary>
    public interface ISignatureComponentInputVisitor : ISignatureComponentVisitor
    {
        /// <summary>
        /// The signature input as built by the visitor.
        /// </summary>
        string SignatureInput { get; }
        
        /// <summary>
        /// The value for the '@signature-params' component, as built by the visitor.
        /// </summary>
        string? SignatureParamsValue { get; }
    }
}