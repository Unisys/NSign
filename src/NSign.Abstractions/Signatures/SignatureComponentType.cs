namespace NSign.Signatures
{
    /// <summary>
    /// Defines the different types of signature components.
    /// </summary>
    public enum SignatureComponentType
    {
        /// <summary>
        /// The component type is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The component is a HTTP message header field.
        /// </summary>
        HttpHeader,

        /// <summary>
        /// The component is a derived component.
        /// </summary>
        Derived,
    }
}
