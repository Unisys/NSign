using NSign.Signatures;
using System;
using System.Net.Http;

namespace NSign.Client
{
    /// <summary>
    /// Extensions for HttpRequestMessage objects.
    /// </summary>
    internal static class HttpRequestMessageExtensions
    {
        /// <summary>
        /// Gets the value of the given <paramref name="derivedComponent"/> for the specified <paramref name="request"/>.
        /// </summary>
        /// <param name="request">
        /// The <see cref="HttpRequestMessage"/> object for which the derived component's value should be retrieved.
        /// </param>
        /// <param name="derivedComponent">
        /// The <see cref="DerivedComponent"/> specifying which value to retrieve.
        /// </param>
        /// <returns>
        /// A string that represents the requested value.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown for unsupported derived components. This includes e.g. the '@query-param' component which has dedicated
        /// logic for value retrieval, or the '@status' or '@request-response' components which are not support for
        /// request messages in the first place.
        /// </exception>
        public static string GetDerivedComponentValue(this HttpRequestMessage request, DerivedComponent derivedComponent)
        {
            return derivedComponent.ComponentName switch
            {
                Constants.DerivedComponents.SignatureParams =>
                    throw new NotSupportedException("The '@signature-params' component cannot be included explicitly."),
                Constants.DerivedComponents.Method => request.Method.Method,
                Constants.DerivedComponents.TargetUri => request.RequestUri!.OriginalString,
                Constants.DerivedComponents.Authority => request.RequestUri!.Authority.ToLower(),
                Constants.DerivedComponents.Scheme => request.RequestUri!.Scheme.ToLower(),
                Constants.DerivedComponents.RequestTarget => request.RequestUri!.PathAndQuery,
                Constants.DerivedComponents.Path => request.RequestUri!.AbsolutePath,
                Constants.DerivedComponents.Query =>
                    String.IsNullOrWhiteSpace(request.RequestUri!.Query) ? "?" : request.RequestUri.Query,
                Constants.DerivedComponents.QueryParam =>
                    throw new NotSupportedException("The '@query-param' component must have the 'name' parameter set."),
                Constants.DerivedComponents.Status =>
                    throw new NotSupportedException("The '@status' component cannot be included in request signatures."),

                _ =>
                    throw new NotSupportedException(
                        $"Non-standard derived signature component '{derivedComponent.ComponentName}' cannot be retrieved."),
            };
        }
    }
}
