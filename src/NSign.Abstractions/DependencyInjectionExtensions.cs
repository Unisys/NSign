using Microsoft.Extensions.Options;
using NSign;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for dependency injection.
    /// </summary>
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Configures options for message signing.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> in which to register the actions for options configuration.
        /// </param>
        /// <param name="configureOptions">
        /// The action used to configure the <see cref="MessageSigningOptions"/> object.
        /// </param>
        /// <returns>
        /// The <see cref="OptionsBuilder{TOptions}"/> for chaining of additional calls.
        /// </returns>
        /// <remarks>
        /// This also registers validation to make sure that configured signature names are valid.
        /// </remarks>
        public static OptionsBuilder<MessageSigningOptions> ConfigureMessageSigningOptions(
            this IServiceCollection services,
            Action<MessageSigningOptions> configureOptions)
        {
            return services.AddOptions<MessageSigningOptions>()
                .Configure(configureOptions)
                .ValidateDataAnnotations()
                ;
        }
    }
}
