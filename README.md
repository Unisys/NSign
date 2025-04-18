[![GitHub](https://img.shields.io/github/license/Unisys/NSign)](https://github.com/Unisys/NSign/blob/main/LICENSE)
[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/Unisys/NSign/build-test.yml?branch=main)](https://github.com/Unisys/NSign/actions/workflows/build-test.yml)
[![GitHub issues](https://img.shields.io/github/issues/Unisys/NSign)](https://github.com/Unisys/NSign/issues)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/Unisys/NSign)](https://github.com/Unisys/NSign/pulls)
[![Coverage Status](https://coveralls.io/repos/github/Unisys/NSign/badge.svg?branch=main)](https://coveralls.io/github/Unisys/NSign?branch=main)

# NSign

## HTTP Message Signatures (RFC 9421) for .NET

NSign (/ˈensaɪn/) provides libraries to sign HTTP messages and verify signatures
based on [HTTP Message Signatures (RFC 9421)](https://datatracker.ietf.org/doc/rfc9421/).
That standard defines mechanisms to sign and verify HTTP messages e.g. used in
webhook-like scenarios where a provider needs to sign HTTP request messages
before sending them to subscribers, and subscribers need to verify incoming
messages' signatures for authentication. Signatures can also be applied to HTTP
response messages for a client to verify on receipt.

## Libraries and Nuget packages

| Library | Purpose | Nuget package |
|---|---|---|
| NSign.Abstractions | Abstractions (interfaces, object model, etc) for all NSign libraries. | [![Nuget](https://img.shields.io/nuget/v/NSign.Abstractions)](https://nuget.org/packages/NSign.Abstractions) |
| NSign.SignatureProviders | Signature providers (signers and verifiers) for symmetric and asymmetric signatures. | [![Nuget](https://img.shields.io/nuget/v/NSign.SignatureProviders)](https://nuget.org/packages/NSign.SignatureProviders) |
| NSign.AspNetCore | Middleware for verifying signatures on HTTP requests and signing HTTP responses. | [![Nuget](https://img.shields.io/nuget/v/NSign.AspNetCore)](https://nuget.org/packages/NSign.AspNetCore) |
| NSign.Client | HTTP message pipeline handlers (for the `System.Net.Http.HttpClient` class) for signing HTTP request messages and verifying signatures on HTTP response messages. | [![Nuget](https://img.shields.io/nuget/v/NSign.Client)](https://nuget.org/packages/NSign.Client) |
| NSign.BouncyCastle | NSign extensions using BouncyCastle; includes a signature provider for `ed25519` (EdDSA using curve edwards25519) | [![Nuget](https://img.shields.io/nuget/v/NSign.BouncyCastle)](https://nuget.org/packages/NSign.BouncyCastle) |

## Usage

Below are some usage examples of the `NSign.*` libraries. Additional sample code can also be found under
[examples/](examples/).

### Validate signed requests in AspNetCore Server (.Net 8.0 and 9.0)

The following excerpt of an ASP.NET Core's `Startup` class can be used to verify signatures on requests sent to `/webhooks`
endpoints (and endpoints starting with `/webhooks/`). It also makes sure that signatures include the following request
components in their input:
- the request method
- the requested target URI
- the `content-type` header of the request
- the `content-digest` header of the request
- the timestamp when the signature was created (by default)
- the expiration of the signature (by default)

Then, after signature verification has succeeded, it will also make sure that the digest of the request body matches the
values from the 'content-digest' header.

```csharp
namespace WebhooksEndpoint
{
    public class Startup
    {
        // ...

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers()
                .Services

                .Configure<RequestSignatureVerificationOptions>(ConfigureSignatureVerification)
                .AddContentDigestVerification()
                .AddSignatureVerification(CreateRsaPssSha512())

                // If you want to sign responses, configure services like this:
                .ConfigureMessageSigningOptions((options) =>
                {
                    options
                        .WithMandatoryComponent(SignatureComponent.Status)
                        .WithMandatoryComponent(SignatureComponent.Path)
                        // Include the 'sample' signature from the request in the response signature, if present.
                        .WithOptionalComponent(
                            new HttpHeaderDictionaryStructuredComponent(Constants.Headers.Signature,
                                                                        "sample",
                                                                        bindRequest: true))
                        ;
                    options.SignatureName = "resp";
                    options.SetParameters = (sigParams) =>
                    {
                        sigParams
                            .WithCreatedNow()
                            .WithExpires(TimeSpan.FromMinutes(5))
                            .WithTag("server-signed")
                            ;
                    };
                })
                .ValidateOnStart()
                .Services
                .AddResponseSigning(new HmacSha256SignatureProvider(System.Text.Encoding.UTF8.GetBytes("my-key"), "my-key"))
                ;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();
            // Validate signatures (and digests) only for webhook requests.
            app.UseWhen(IsWebhook, UseSignatureVerification);
            // If you want to sign all responses:
            app.UseResponseSigning();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void ConfigureSignatureVerification(RequestSignatureVerificationOptions options)
        {
            options.TagsToVerify.Add("client-signed");

            options.RequiredSignatureComponents.Add(SignatureComponent.Method);
            options.RequiredSignatureComponents.Add(SignatureComponent.RequestTargetUri);
            options.RequiredSignatureComponents.Add(SignatureComponent.ContentDigest);
            options.RequiredSignatureComponents.Add(SignatureComponent.ContentType);
        }

        private static void UseSignatureVerification(IApplicationBuilder app)
        {
            app
                .UseSignatureVerification()
                .UseContentDigestVerification()
                ;
        }

        public static IVerifier CreateRsaPssSha512()
        {
            return new RsaPssSha512SignatureProvider(GetCertificate(), "my-cert");
        }

        private static X509Certificate2 GetCertificate()
        {
            return new X509Certificate2(@"path\to\certificate.cer");
        }

        private static bool IsWebhook(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments("/webhooks");
        }
    }
}
```

### Service sending signed requests

The following example shows a very simple console app setting up hosting with dependency injection. It will make sure to
sign all requests made through the `HttpClient` named `WebhooksCaller` are updated with a `content-digest` header holding
the SHA-256 hash of the request content, and a signature secured with the private key of the certificate in
`path\to\certificate.pfx`. It ensures that signatures include number of mandatory fields, as well as optionally the
`content-length` header of the request, and that signatures are valid for 5 minutes only.

```csharp
namespace WebhooksCaller
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).RunConsoleAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(ConfigureServices)
            ;

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services
                .Configure<AddContentDigestOptions>(options => options.WithHash(AddContentDigestOptions.Hash.Sha256))
                .ConfigureMessageSigningOptions(ConfigureRequestSigner)
                // If you also want to verify signatures on responses:
                .Configure<SignatureVerificationOptions>((options) => {
                    // Configure options for response signature verification here.
                })

                .AddHttpClient<ICaller, Caller>("WebhooksCaller")
                .ConfigureHttpClient(ConfigureCallerClient)
                .AddContentDigestAndSigningHandlers()
                // If you also want to verify signatures on responses:
                .AddSignatureVerificationHandler()
                .Services

                .AddHostedService<CallerService>()

                .AddSingleton<ISigner>(new RsaPssSha512SignatureProvider(
                    new X509Certificate2(@"path\to\certificate.pfx", "PasswordForPfx"),
                    "my-cert"))
                ;
        }

        private static void ConfigureRequestSigner(MessageSigningOptions options)
        {
            options.SignatureName = "sample";
            options
                .WithMandatoryComponent(SignatureComponent.Method)
                .WithMandatoryComponent(SignatureComponent.RequestTargetUri)
                .WithMandatoryComponent(SignatureComponent.Scheme)
                .WithMandatoryComponent(SignatureComponent.Query)
                .WithMandatoryComponent(SignatureComponent.ContentDigest)
                .WithMandatoryComponent(SignatureComponent.ContentType)
                .WithOptionalComponent(SignatureComponent.ContentLength)
                .SetParameters = SetSignatureCreatedAndExpries
                ;
        }

        private static void SetSignatureCreatedAndExpries(SignatureParamsComponent signatureParams)
        {
            signatureParams
                .WithCreatedNow()
                .WithExpires(TimeSpan.FromMinutes(5))
                .WithTag("client-signed")
                ;
        }

        private static void ConfigureCallerClient(HttpClient client)
        {
            client.BaseAddress = new Uri("https://localhost:5001/webhooks/my/endpoint");
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NSignSample", "0.1-beta"));
        }
    }
}
```

## Missing Features

- [ ] Support for JSON Web Signature algorithms
- [ ] Support for `Accept-Signature`
