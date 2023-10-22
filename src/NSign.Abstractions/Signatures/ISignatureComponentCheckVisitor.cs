namespace NSign.Signatures
{
    /// <summary>
    /// Contract for a SignatureComponent visitor that should validate the signature.
    /// </summary>
    public interface ISignatureComponentCheckVisitor : ISignatureComponentVisitor
    {
        /// <summary>
        /// Gets or sets a flag which indicates whether or not all the tested components were found.
        /// </summary>
        public bool Found { get; }
    }
}