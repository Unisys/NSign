using Microsoft.AspNetCore.Http;
using NSign.Signatures;
using System;

namespace NSign.AspNetCore
{
    /// <summary>
    /// Extensions for HttpRequest objects.
    /// </summary>
    internal static partial class HttpRequestExtensions
    {
        /// <summary>
        /// Gets the value of the given <paramref name="derivedComponent"/> for the specified <paramref name="request"/>.
        /// </summary>
        /// <param name="request">
        /// The <see cref="HttpRequest"/> object for which the derived component's value should be retrieved.
        /// </param>
        /// <param name="derivedComponent">
        /// The <see cref="DerivedComponent"/> specifying which value to retrieve.
        /// </param>
        /// <returns>
        /// A string that represents the requested value.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown for unsupported derived components. This includes e.g. the '@query-params' component which has dedicated
        /// logic for value retrieval, or the '@status' or '@request-response' components which are not support for
        /// request messages in the first place.
        /// </exception>
        public static string GetDerivedComponentValue(this HttpRequest request, DerivedComponent derivedComponent)
        {
            string value = derivedComponent.ComponentName switch
            {
                Constants.DerivedComponents.SignatureParams
                    => throw new NotSupportedException("The '@signature-params' component value cannot be retrieved like this."),
                Constants.DerivedComponents.Method => request.Method,
                // TODO: Need to figure out a way to deal with reverse proxies changing paths, i.e. getting the original path/prefix.
                Constants.DerivedComponents.TargetUri => $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}",
                Constants.DerivedComponents.Authority => request.Host.Value.ToLower(),
                Constants.DerivedComponents.Scheme => request.Scheme.ToLower(),
                // TODO: Need to figure out a way to deal with reverse proxies changing paths, i.e. getting the original path/prefix.
                Constants.DerivedComponents.RequestTarget => $"{request.PathBase}{request.Path}{request.QueryString}",
                // TODO: Need to figure out a way to deal with reverse proxies changing paths, i.e. getting the original path/prefix.
                Constants.DerivedComponents.Path => $"{request.PathBase}{request.Path}",
                Constants.DerivedComponents.Query => request.QueryString.HasValue ? request.QueryString.Value! : "?",
                Constants.DerivedComponents.QueryParams
                    => throw new NotSupportedException("The '@query-params' component value cannot be retrieved like this."),
                Constants.DerivedComponents.Status
                    => throw new NotSupportedException("The '@status' component value cannot be retrieved for request messages."),
                Constants.DerivedComponents.RequestResponse
                    => throw new NotSupportedException("The '@request-response' component value cannot be retrieved for request messages."),

                _ => throw new NotSupportedException($"Non-standard derived signature component '{derivedComponent.ComponentName}' cannot be retrieved."),
            };

            return value!;
        }
    }
}
