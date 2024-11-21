using Microsoft.AspNetCore.Http;
using NSign.Signatures;
using System;

namespace NSign.AspNetCore
{
    /// <summary>
    /// Extensions for HttpRequest objects.
    /// </summary>
    internal static class HttpRequestExtensions
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
        /// Thrown for unsupported derived components. This includes e.g. the '@query-param' component which has dedicated
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
                Constants.DerivedComponents.Authority => NormalizeAuthority(request),
                Constants.DerivedComponents.Scheme => request.Scheme.ToLower(),
                // TODO: Need to figure out a way to deal with reverse proxies changing paths, i.e. getting the original path/prefix.
                Constants.DerivedComponents.RequestTarget => $"{request.PathBase}{request.Path}{request.QueryString}",
                // TODO: Need to figure out a way to deal with reverse proxies changing paths, i.e. getting the original path/prefix.
                Constants.DerivedComponents.Path => $"{request.PathBase}{request.Path}",
                Constants.DerivedComponents.Query => request.QueryString.HasValue ? request.QueryString.Value! : "?",
                Constants.DerivedComponents.QueryParam
                    => throw new NotSupportedException("The '@query-param' component value cannot be retrieved like this."),
                Constants.DerivedComponents.Status
                    => throw new NotSupportedException("The '@status' component value cannot be retrieved for request messages."),

                _ => throw new NotSupportedException($"Non-standard derived signature component '{derivedComponent.ComponentName}' cannot be retrieved."),
            };

            return value!;
        }

        /// <summary>
        /// Normalizes the value for the <c>@authority</c> derived component:
        /// default ports are omitted and host values are lower-cased.
        /// </summary>
        /// <param name="request">
        /// The <see cref="HttpRequest"/> defining the values to use for the
        /// <c>@authority</c> derived component value.
        /// </param>
        /// <returns>
        /// A string value representing the <c>@authority</c> derived component
        /// value for the message.
        /// </returns>
        private static string NormalizeAuthority(HttpRequest request)
        {
            string scheme = request.Scheme;
            HostString host = request.Host;

            if (host.Port.HasValue &&
                ((StringComparer.OrdinalIgnoreCase.Equals("http", scheme) &&
                  host.Port.Value == 80) ||
                 (StringComparer.OrdinalIgnoreCase.Equals("https", scheme) &&
                  host.Port.Value == 443)))
            {
                return host.Host.ToLower();
            }

            return host.ToUriComponent();
        }
    }
}
