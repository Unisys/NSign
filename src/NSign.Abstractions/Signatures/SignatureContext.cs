namespace NSign.Signatures
{
    /// <summary>
    /// Holds the relevant details of a signature to verify.
    /// </summary>
    public readonly struct SignatureContext
    {
        /// <summary>
        /// Initializes a new instance of SignatureContext.
        /// </summary>
        /// <param name="name">
        /// The name of the signature.
        /// </param>
        /// <param name="inputSpec">
        /// The (unparsed) input spec string for the signature.
        /// </param>
        /// <param name="signature">
        /// A byte array representing the signature.
        /// </param>
        public SignatureContext(string name, string inputSpec, byte[] signature)
        {
            Name = name;
            InputSpec = inputSpec;
            Signature = signature;
        }

        /// <summary>
        /// Gets the name of the signature.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the (unparsed) input spec string for the signature.
        /// </summary>
        public string InputSpec { get; }

        /// <summary>
        /// Gets a byte array representing the signature.
        /// </summary>
        public byte[] Signature { get; }

        /// <summary>
        /// Gets a flag which indicates whether or not the InputSpec is present.
        /// </summary>
        public bool HasInputSpec => null != InputSpec;
    }
}
