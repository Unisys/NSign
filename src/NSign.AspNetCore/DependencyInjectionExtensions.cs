using Microsoft.AspNetCore.Builder;
using NSign;
using NSign.AspNetCore;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for dependency injection.
    /// </summary>
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="DigestVerificationMiddleware"/> to the request pipeline.
        /// </summary>
        /// <param name="app">
        /// The <see cref="IApplicationBuilder"/> instance.
        /// </param>
        /// <returns>
        /// The <see cref="IApplicationBuilder"/> instance.
        /// </returns>
        public static IApplicationBuilder UseDigestVerification(this IApplicationBuilder app)
        {
            return app.UseMiddleware<DigestVerificationMiddleware>();
        }

        /// <summary>
        /// Adds the <see cref="DigestVerificationMiddleware"/> as a transient service.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> instance.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> instance.
        /// </returns>
        public static IServiceCollection AddDigestVerification(this IServiceCollection services)
        {
            return services.AddTransient<DigestVerificationMiddleware>();
        }

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
                .AddScoped<SignatureVerificationMiddleware>()
                .AddScoped(factory)
                ;
        }
    }
}
