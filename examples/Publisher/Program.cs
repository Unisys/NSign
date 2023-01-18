using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSign;
using NSign.Client;
using NSign.Providers;
using NSign.Signatures;
using Publisher;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;

IHost host = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices(SetupServices)
    .Build();

host.Run();


static void SetupServices(IServiceCollection services)
{
    services
        .AddHttpClient<WebhooksCaller>("WebhooksCaller")
        .ConfigureHttpClient((client) =>
        {
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NSignExample.Publisher", "0.1"));
        })
        .AddDigestAndSigningHandlers()
        .Services

        .Configure<AddDigestOptions>(options => options.WithHash(AddDigestOptions.Hash.Sha256))
        .ConfigureMessageSigningOptions(options =>
        {
            options.SignatureName = "pubsig";
            options
                .WithMandatoryComponent(SignatureComponent.Digest)
                .WithMandatoryComponent(new HttpHeaderStructuredFieldComponent(Constants.Headers.ContentType))
                .WithOptionalComponent(SignatureComponent.ContentLength)
                .SetParameters = (signingOptions) => signingOptions
                    .WithCreated(DateTimeOffset.UtcNow.AddMinutes(-2))
                    .WithExpires(TimeSpan.FromMinutes(10))
                    .WithNonce(Guid.NewGuid().ToString("N"))
                    .WithTag("nsign-example-publisher")
                ;
        }).Services
        .AddSingleton<ISigner>(
            new RsaPssSha512SignatureProvider(
                new X509Certificate2(@"examples.nsign.local.pfx", (string?)null), "examples.nsign.local"))
        .AddHostedService<PublisherWorker>()
        ;
}