using NSign.Client;
using NSign.Signatures;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for dependency injection.
    /// </summary>
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="AddContentDigestHandler"/> message handler to the HTTP client.
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IHttpClientBuilder"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IHttpClientBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IHttpClientBuilder AddContentDigestHandler(this IHttpClientBuilder clientBuilder)
        {
            clientBuilder.Services.AddTransient<AddContentDigestHandler>();

            return clientBuilder.AddHttpMessageHandler<AddContentDigestHandler>();
        }

        /// <summary>
        /// Adds the <see cref="SigningHandler"/> message handler to the HTTP client. This also registers the
        /// <see cref="DefaultMessageSigner"/> as the default <see cref="IMessageSigner"/> in the services.
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IHttpClientBuilder"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IHttpClientBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IHttpClientBuilder AddSigningHandler(this IHttpClientBuilder clientBuilder)
        {
            clientBuilder.Services
                .AddTransient<SigningHandler>()
                .AddTransient<IMessageSigner, DefaultMessageSigner>();

            return clientBuilder.AddHttpMessageHandler<SigningHandler>();
        }

        /// <summary>
        /// Adds both the <see cref="AddContentDigestHandler"/> and the <see cref="SigningHandler"/> message handlers to the HTTP
        /// client, in that order.
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IHttpClientBuilder"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IHttpClientBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IHttpClientBuilder AddContentDigestAndSigningHandlers(this IHttpClientBuilder clientBuilder)
        {
            // Digest must come before signing to make sure signing also has the 'Digest' header available.
            return clientBuilder.AddContentDigestHandler().AddSigningHandler();
        }

        /// <summary>
        /// Adds the <see cref="SignatureVerificationHandler"/> message handler to the HTTP client. This also registers
        /// the <see cref="DefaultMessageVerifier"/> as the default <see cref="IMessageVerifier"/> in the services.
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IHttpClientBuilder"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IHttpClientBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IHttpClientBuilder AddSignatureVerificationHandler(this IHttpClientBuilder clientBuilder)
        {
            clientBuilder.Services
                .AddTransient<SignatureVerificationHandler>()
                .AddTransient<IMessageVerifier, DefaultMessageVerifier>();

            return clientBuilder.AddHttpMessageHandler<SignatureVerificationHandler>();
        }
    }
}
