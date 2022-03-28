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
        public static IHttpClientBuilder AddSignatureVerifiationHandler(this IHttpClientBuilder clientBuilder)
        {
            clientBuilder.Services
                .AddTransient<SignatureVerificationHandler>()
                .AddTransient<IMessageVerifier, DefaultMessageVerifier>();

            return clientBuilder.AddHttpMessageHandler<SignatureVerificationHandler>();
        }
    }
}
