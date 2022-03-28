using System;

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
        /// A <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/> representing the signature.
        /// </param>
        public SignatureContext(string name, string? inputSpec, ReadOnlyMemory<byte> signature)
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
        public string? InputSpec { get; }

        /// <summary>
        /// Gets a <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/> representing the signature.
        /// </summary>
        public ReadOnlyMemory<byte> Signature { get; }
    }
}
