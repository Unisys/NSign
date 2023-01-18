# NSign.Client

Middleware/handlers for the `HttpClient` class from the `System.Net.Http` namespace to help with signing of outgoing
HTTP requests and verification of signatures on incoming HTTP responses.

## Usage

### Signing outgoing request messages

To have outgoing request messages signed, configure the middleware/handlers on an `HttpClient` as in the following
example. Please don't forget to adapt mandatory and optional signature components as well as signature parameters to
your use case.

```csharp
services
    .ConfigureMessageSigningOptions(options =>
    {
        options.SignatureName = "mysig";
        options
            .WithMandatoryComponent(SignatureComponent.Method)
            .WithMandatoryComponent(SignatureComponent.RequestTargetUri)
            .WithMandatoryComponent(SignatureComponent.Scheme)
            .WithMandatoryComponent(SignatureComponent.Query)
            .WithMandatoryComponent(SignatureComponent.Digest)
            .WithMandatoryComponent(SignatureComponent.ContentType)
            .WithOptionalComponent(SignatureComponent.ContentLength)
            .SetParameters = (signatureParams) => 
            {
                signatureParams
                    .WithCreatedNow()
                    .WithExpires(TimeSpan.FromMinutes(5))
                    .WithNonce(Guid.NewGuid().ToString("N"))
                    .WithTag("my-signature")
                    ;
            };
    })
    .AddHttpClient<IMyService, MyServiceImpl>("MyService")
    .AddSigningHandler()
    ;
```

You will also need to configure a signature provider that actually signs the requests. See
[NSign.SignatureProviders](https://nuget.org/packages/NSign.SignatureProviders) for currently available standard
implemenations. It is important to register the signature provider through the `ISigner` interface, for instance:

```csharp
services
    .AddSingleton<ISigner>(new RsaPssSha512SignatureProvider(
        new X509Certificate2(@"path\to\certificate.pfx", "PasswordForPfx"),
        "my-cert"))
    ;
```

> **NOTE:** The signature provider must have access to the private key when asymmetric signatures are used. It must have
access to the shared key when symmetric signatures are used.

### Verifying signatures on response messages

To have incoming response messages' signatures verified, configure the middleware/handlers on an `HttpClient` as in the
following example. Please don't forget to adapt required signature components as well as signature parameters to your
use case. Also make sure that the `TagsToVerify` is updated to include the tags used by the remote service to identify
its signatures.

```csharp
services
    .Configure<SignatureVerificationOptions>((options) =>
    {
        options.TagsToVerify.Add("remote-service-signature");
        options.RequiredSignatureComponents.Add(SignatureComponent.Status));
        options.RequiredSignatureComponents.Add(SignatureComponent.RequestTargetUri));
        options.RequiredSignatureComponents.Add(SignatureComponent.ContentType));
        options.CreatedRequired =
            options.ExpiresRequired =
            options.KeyIdRequired =
            options.AlgorithmRequired =
            options.TagRequired = true;
        options.MaxSignatureAge = TimeSpan.FromMinutes(5);
    })
    .AddHttpClient<IMyService, MyServiceImpl>("MyService")
    .AddSignatureVerificationHandler()
    ;
```

You will also need to configure a signature provider that actually verifies signatures on the responses. See
[NSign.SignatureProviders](https://nuget.org/packages/NSign.SignatureProviders) for currently available standard
implemenations. It is important to register the signature provider through the `IVerifier` interface, for instance:

```csharp
services
    .AddSingleton<IVerifier>(new RsaPssSha512SignatureProvider(
        new X509Certificate2(@"path\to\certificate.cer"),
        "my-cert"))
    ;
```

> **NOTE:** The signature provider only requires access to the public key when asymmetric signatures are used. It must
have access to the shared key when symmetric signatures are used.

## Further Information

See also:
- [NSign on github.com](https://github.com/Unisys/NSign)
- [HTTP Message Signatures (current draft)](https://datatracker.ietf.org/doc/draft-ietf-httpbis-message-signatures/)
