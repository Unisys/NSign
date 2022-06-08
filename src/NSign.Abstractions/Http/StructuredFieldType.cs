namespace NSign.Http
{
    /// <summary>
    /// Defines the different types of structured fields.
    /// </summary>
    /// <remarks>
    /// This is using the terminology from section 4.2 of RFC 8941.
    /// See also <seealso href="https://httpwg.org/specs/rfc8941.html#rfc.section.4.2"/>.
    /// </remarks>
    public enum StructuredFieldType
    {
        /// <summary>
        /// The structured field type is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The structured field type is 'dictionary'.
        /// </summary>
        Dictionary,

        /// <summary>
        /// The structured field type is 'list'.
        /// </summary>
        List,

        /// <summary>
        /// The structured field type is 'item'.
        /// </summary>
        Item,
    }
}
