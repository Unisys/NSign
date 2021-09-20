using NSign.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for dependency injection.
    /// </summary>
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="AddDigestHandler"/> message handler to the HTTP client.
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IHttpClientBuilder"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IHttpClientBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IHttpClientBuilder AddDigestHandler(this IHttpClientBuilder clientBuilder)
        {
            clientBuilder.Services.AddTransient<AddDigestHandler>();

            return clientBuilder.AddHttpMessageHandler<AddDigestHandler>();
        }

        /// <summary>
        /// Adds the <see cref="SigningHandler"/> message handler to the HTTP client.
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IHttpClientBuilder"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IHttpClientBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IHttpClientBuilder AddSigningHandler(this IHttpClientBuilder clientBuilder)
        {
            clientBuilder.Services.AddTransient<SigningHandler>();

            return clientBuilder.AddHttpMessageHandler<SigningHandler>();
        }

        /// <summary>
        /// Adds both the <see cref="AddDigestHandler"/> and the <see cref="SigningHandler"/> message handlers to the HTTP
        /// client, in that order.
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IHttpClientBuilder"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IHttpClientBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IHttpClientBuilder AddDigestAndSigningHandlers(this IHttpClientBuilder clientBuilder)
        {
            // Digest must come before signing to make sure signing also has the 'Digest' header available.
            return clientBuilder.AddDigestHandler().AddSigningHandler();
        }
    }
}
