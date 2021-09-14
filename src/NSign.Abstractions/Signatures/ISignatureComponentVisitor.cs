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
        /// Visits the given SpecialtyComponent.
        /// </summary>
        /// <param name="specialty">
        /// The SpecialtyComponent that is to be visited.
        /// </param>
        void Visit(SpecialtyComponent specialty);

        /// <summary>
        /// Visits the given SignatureParamsComponent.
        /// </summary>
        /// <param name="signatureParams">
        /// The SignatureParamsComponent that is to be visited.
        /// </param>
        void Visit(SignatureParamsComponent signatureParams);

        /// <summary>
        /// Visits the given QueryParamsComponent.
        /// </summary>
        /// <param name="queryParams">
        /// The QueryParamsComponent that is to be visited.
        /// </param>
        void Visit(QueryParamsComponent queryParams);

        /// <summary>
        /// Visits the given RequestResponseComponent.
        /// </summary>
        /// <param name="requestResponse">
        /// The RequestResponseComponent that is to be visited.
        /// </param>
        void Visit(RequestResponseComponent requestResponse);
    }
}
