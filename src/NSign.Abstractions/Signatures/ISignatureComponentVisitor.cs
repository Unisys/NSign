namespace NSign.Signatures
{
    /// <summary>
    /// Interface for visitors of signature components and derived classes.
    /// </summary>
    public interface ISignatureComponentVisitor
    {
        /// <summary>
        /// Visits the given SignatureComponent.
        /// </summary>
        /// <param name="component">
        /// The SignatureComponent that is to be visited.
        /// </param>
        void Visit(SignatureComponent component);

        /// <summary>
        /// Visits the given HttpHeaderComponent.
        /// </summary>
        /// <param name="httpHeader">
        /// The HttpHeaderComponent that is to be visited.
        /// </param>
        void Visit(HttpHeaderComponent httpHeader);

        /// <summary>
        /// Visits the given HttpHeaderDictionaryStructuredComponent.
        /// </summary>
        /// <param name="httpHeaderDictionary">
        /// The HttpHeaderDictionaryStructuredComponent that is to be visited.
        /// </param>
        void Visit(HttpHeaderDictionaryStructuredComponent httpHeaderDictionary);

        /// <summary>
        /// Visits the given HttpHeaderStructuredFieldComponent.
        /// </summary>
        /// <param name="httpHeaderStructuredField">
        /// The HttpHeaderStructuredFieldComponent that is to be visited.
        /// </param>
        void Visit(HttpHeaderStructuredFieldComponent httpHeaderStructuredField);

        /// <summary>
        /// Visits the given DerivedComponent.
        /// </summary>
        /// <param name="derived">
        /// The DerivedComponent that is to be visited.
        /// </param>
        void Visit(DerivedComponent derived);

        /// <summary>
        /// Visits the given SignatureParamsComponent.
        /// </summary>
        /// <param name="signatureParams">
        /// The SignatureParamsComponent that is to be visited.
        /// </param>
        void Visit(SignatureParamsComponent signatureParams);

        /// <summary>
        /// Visits the given QueryParamComponent.
        /// </summary>
        /// <param name="queryParam">
        /// The QueryParamComponent that is to be visited.
        /// </param>
        void Visit(QueryParamComponent queryParam);
    }
}
