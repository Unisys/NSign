namespace NSign.Signatures
{
    /// <summary>
    /// Represents a read-only specification of signature input.
    /// </summary>
    public readonly struct SignatureInputSpec
    {
        /// <summary>
        /// Initializes a new instance of SignatureInputSpec.
        /// </summary>
        /// <param name="name">
        /// The name of the signature and its input.
        /// </param>
        public SignatureInputSpec(string name)
        {
            Name = name;
            SignatureParameters = new SignatureParamsComponent();
        }

        /// <summary>
        /// Initializes a new instance of SignatureInputSpec.
        /// </summary>
        /// <param name="name">
        /// The name of the signature and its input.
        /// </param>
        /// <param name="value">
        /// The value from the 'Signature-Input' header matching the specified name.
        /// </param>
        public SignatureInputSpec(string name, string value) : this(name)
        {
            SignatureParameters = new SignatureParamsComponent(value);
        }

        /// <summary>
        /// Gets the name of the signature and its input in the HTTP message headers.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the SignatureParameters object that defines the components and parameters for the signature input.
        /// </summary>
        public SignatureParamsComponent SignatureParameters { get; }
    }
}
