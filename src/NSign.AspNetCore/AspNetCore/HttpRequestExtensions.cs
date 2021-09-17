using Microsoft.AspNetCore.Http;
using NSign.Signatures;
using System.Text;

namespace NSign.AspNetCore
{
    /// <summary>
    /// Extensions for HttpRequest objects.
    /// </summary>
    internal static partial class HttpRequestExtensions
    {
        /// <summary>
        /// Gets a byte array representing the actual input for signature verification for the given request.
        /// </summary>
        /// <param name="request">
        /// The HttpRequest for which to get the input.
        /// </param>
        /// <param name="inputSpec">
        /// The SignatureInputSpec object representing the input spec that defines how to build the signature input.
        /// </param>
        /// <returns>
        /// A byte array representing the input for signature verification.
        /// </returns>
        public static byte[] GetSignatureInput(this HttpRequest request, SignatureInputSpec inputSpec)
        {
            Visitor visitor = new Visitor(request);

            inputSpec.SignatureParameters.Accept(visitor);

            return Encoding.ASCII.GetBytes(visitor.SignatureInput);
        }
    }
}
