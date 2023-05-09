using Microsoft.AspNetCore.Builder;
using NSign;
using NSign.AspNetCore;
using NSign.Signatures;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for dependency injection.
    /// </summary>
    public static class DependencyInjectionExtensions
    {
        #region Digest Verification Middleware

        /// <summary>
        /// Adds the <see cref="ContentDigestVerificationMiddleware"/> to the request pipeline.
        /// </summary>
        /// <param name="app">
        /// The <see cref="IApplicationBuilder"/> instance.
        /// </param>
        /// <returns>
        /// The <see cref="IApplicationBuilder"/> instance.
        /// </returns>
        public static IApplicationBuilder UseContentDigestVerification(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ContentDigestVerificationMiddleware>();
        }

        /// <summary>
        /// Adds the <see cref="ContentDigestVerificationMiddleware"/> as a transient service.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> instance.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> instance.
        /// </returns>
        public static IServiceCollection AddContentDigestVerification(this IServiceCollection services)
        {
            return services.AddTransient<ContentDigestVerificationMiddleware>();
        }

        #endregion

        #region Signature Verification Middleware

        /// <summary>
        /// Adds the <see cref="SignatureVerificationMiddleware"/> to the request pipeline.
        /// </summary>
        /// <param name="app">
        /// The <see cref="IApplicationBuilder"/> instance.
        /// </param>
        /// <returns>
        /// The <see cref="IApplicationBuilder"/> instance.
        /// </returns>
        public static IApplicationBuilder UseSignatureVerification(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SignatureVerificationMiddleware>();
        }

        /// <summary>
        /// Adds signature verification with the given <typeparamref name="TImpl"/> implementation of <see cref="IVerifier"/>
        /// as a singleton. This also registers the <see cref="SignatureVerificationMiddleware"/> as a singleton.
        /// </summary>
        /// <typeparam name="TImpl">
        /// A type implementing <see cref="IVerifier"/> that should be used for verifying signatures.
        /// </typeparam>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> instance.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> instance.
        /// </returns>
        public static IServiceCollection AddSignatureVerificationSingleton<TImpl>(this IServiceCollection services)
            where TImpl : class, IVerifier
        {
            return services
                .AddSingleton<IMessageVerifier, DefaultMessageVerifier>()
                .AddSingleton<SignatureVerificationMiddleware>()
                .AddSingleton<IVerifier, TImpl>()
                ;
        }

        /// <summary>
        /// Adds signature verification with the given instance of <typeparamref name="TImpl"/> implementation of
        /// <see cref="IVerifier"/> as a singleton. This also registers the <see cref="SignatureVerificationMiddleware"/>
        /// as a singleton.
        /// </summary>
        /// <typeparam name="TImpl">
        /// A type implementing <see cref="IVerifier"/> that should be used for verifying signatures.
        /// </typeparam>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> instance.
        /// </param>
        /// <param name="implementation">
        /// The instance of <typeparamref name="TImpl"/> to use.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> instance.
        /// </returns>
        public static IServiceCollection AddSignatureVerification<TImpl>(this IServiceCollection services, TImpl implementation)
            where TImpl : class, IVerifier
        {
            return services
                .AddSingleton<IMessageVerifier, DefaultMessageVerifier>()
                .AddSingleton<SignatureVerificationMiddleware>()
                .AddSingleton<IVerifier>(implementation)
                ;
        }

        /// <summary>
        /// Adds signature verification with the given <typeparamref name="TImpl"/> implementation of <see cref="IVerifier"/>
        /// as a scoped service. This also registers the <see cref="SignatureVerificationMiddleware"/> as a scoped service.
        /// </summary>
        /// <typeparam name="TImpl">
        /// A type implementing <see cref="IVerifier"/> that should be used for verifying signatures.
        /// </typeparam>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> instance.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> instance.
        /// </returns>
        public static IServiceCollection AddSignatureVerification<TImpl>(this IServiceCollection services)
            where TImpl : class, IVerifier
        {
            return services
                .AddScoped<IMessageVerifier, DefaultMessageVerifier>()
                .AddScoped<SignatureVerificationMiddleware>()
                .AddScoped<IVerifier, TImpl>()
                ;
        }

        /// <summary>
        /// Adds signature verification with the given factory to create instances of <see cref="IVerifier"/> as a scoped
        /// service. This also registers the <see cref="SignatureVerificationMiddleware"/> as a scoped service.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> instance.
        /// </param>
        /// <param name="factory">
        /// The factory that creates the service.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> instance.
        /// </returns>
        public static IServiceCollection AddSignatureVerification(
            this IServiceCollection services,
            Func<IServiceProvider, IVerifier> factory)
        {
            return services
                .AddScoped<IMessageVerifier, DefaultMessageVerifier>()
                .AddScoped<SignatureVerificationMiddleware>()
                .AddScoped(factory)
                ;
        }

        #endregion

        #region Response Signing Middleware

        /// <summary>
        /// Adds the <see cref="ResponseSigningMiddleware"/> to the request pipeline.
        /// </summary>
        /// <param name="app">
        /// The <see cref="IApplicationBuilder"/> instance.
        /// </param>
        /// <returns>
        /// The <see cref="IApplicationBuilder"/> instance.
        /// </returns>
        public static IApplicationBuilder UseResponseSigning(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ResponseSigningMiddleware>();
        }

        /// <summary>
        /// Adds response signing with the given <typeparamref name="TImpl"/> implementation of <see cref="ISigner"/>
        /// as a singleton. This also registers the <see cref="ResponseSigningMiddleware"/> as a singleton.
        /// </summary>
        /// <typeparam name="TImpl">
        /// A type implementing <see cref="ISigner"/> that should be used for signing responses.
        /// </typeparam>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> instance.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> instance.
        /// </returns>
        public static IServiceCollection AddResponseSigningSingleton<TImpl>(this IServiceCollection services)
            where TImpl : class, ISigner
        {
            return services
                .AddSingleton<IMessageSigner, DefaultMessageSigner>()
                .AddSingleton<ResponseSigningMiddleware>()
                .AddSingleton<ISigner, TImpl>()
                ;
        }

        /// <summary>
        /// Adds response signing with the given instance of <typeparamref name="TImpl"/> implementation of
        /// <see cref="ISigner"/> as a singleton. This also registers the <see cref="ResponseSigningMiddleware"/>
        /// as a singleton.
        /// </summary>
        /// <typeparam name="TImpl">
        /// A type implementing <see cref="ISigner"/> that should be used for signing responses.
        /// </typeparam>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> instance.
        /// </param>
        /// <param name="implementation">
        /// The instance of <typeparamref name="TImpl"/> to use.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> instance.
        /// </returns>
        public static IServiceCollection AddResponseSigning<TImpl>(this IServiceCollection services, TImpl implementation)
            where TImpl : class, ISigner
        {
            return services
                .AddSingleton<IMessageSigner, DefaultMessageSigner>()
                .AddSingleton<ResponseSigningMiddleware>()
                .AddSingleton<ISigner>(implementation)
                ;
        }

        /// <summary>
        /// Adds response signing with the given <typeparamref name="TImpl"/> implementation of <see cref="ISigner"/>
        /// as a scoped service. This also registers the <see cref="ResponseSigningMiddleware"/> as a scoped service.
        /// </summary>
        /// <typeparam name="TImpl">
        /// A type implementing <see cref="ISigner"/> that should be used for signing responses.
        /// </typeparam>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> instance.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> instance.
        /// </returns>
        public static IServiceCollection AddResponseSigning<TImpl>(this IServiceCollection services)
            where TImpl : class, ISigner
        {
            return services
                .AddScoped<IMessageSigner, DefaultMessageSigner>()
                .AddScoped<ResponseSigningMiddleware>()
                .AddScoped<ISigner, TImpl>()
                ;
        }

        /// <summary>
        /// Adds response signing with the given factory to create instances of <see cref="ISigner"/> as a scoped
        /// service. This also registers the <see cref="ResponseSigningMiddleware"/> as a scoped service.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> instance.
        /// </param>
        /// <param name="factory">
        /// The factory that creates the service.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> instance.
        /// </returns>
        public static IServiceCollection AddResponseSigning(
            this IServiceCollection services,
            Func<IServiceProvider, ISigner> factory)
        {
            return services
                .AddScoped<IMessageSigner, DefaultMessageSigner>()
                .AddScoped<ResponseSigningMiddleware>()
                .AddScoped(factory)
                ;
        }

        #endregion
    }
}
