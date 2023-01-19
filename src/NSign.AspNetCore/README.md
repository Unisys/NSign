# NSign.AspNetCore

Middleware for ASP.NET Core services to verify signatures on incoming HTTP requests and sign outgoing HTTP responses.

## Usage

### Verifying signatures on incoming request messages

To have incoming request messages' signatures verified, configure the middleware for the corresponding endpoints as in
the following example. Please don't forget to adapt endpoint filtering, required signature components as well as
signature parameters to your use case. Also make sure that the `TagsToVerify` is updated to include the tags used by the
callers to identify their signatures.

```csharp
# Service configuration
services
    .Configure<RequestSignatureVerificationOptions>((options) =>
    {
        options.TagsToVerify.Add("caller-id");
        options.RequiredSignatureComponents.Add(SignatureComponent.RequestTargetUri));
        options.RequiredSignatureComponents.Add(SignatureComponent.ContentType));
        options.CreatedRequired =
            options.ExpiresRequired =
            options.KeyIdRequired =
            options.AlgorithmRequired =
            options.TagRequired = true;
        options.MaxSignatureAge = TimeSpan.FromMinutes(5);

        options.VerifyNonce = (SignatureParamsComponent signatureParams) =>
        {
            Console.WriteLine($"Got signature with tag={signatureParams.Tag} and nonce={signatureParams.Nonce}.");
            // TODO: Actually verify that the nonce was never used before and return false if it was.
            return true;
        };
    })
    ;

# Middleware configuration - register signature verification before the actual middleware/controller handling the request:
app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/webhooks"), builder => builder.UseSignatureVerification());    
app.MapControllers();
```

You will also need to configure a signature provider that actually verifies the signatures on the requests. See
[NSign.SignatureProviders](https://nuget.org/packages/NSign.SignatureProviders) for currently available standard
implemenations. You can do so for instance as follows:

```csharp
services
    .AddSignatureVerification(new RsaPssSha512SignatureProvider(
        new X509Certificate2(@"path\to\certificate.cer"), "the-key-id"))
    ;
```

> **NOTE:** The signature provider only requires access to the public key when asymmetric signatures are used. It must
have access to the shared key when symmetric signatures are used.

### Signing outgoing response messages

To have outgoing response messages signed, configure the middleware for the corresponding endpoints as in the following
example. Please don't forget to adapt endpoint filtering, required signature components as well as signature parameters
to your use case.

```csharp
# Service configuration
services
    .ConfigureMessageSigningOptions((options) =>
    {
        options
            .WithMandatoryComponent(SignatureComponent.Status)
            .WithMandatoryComponent(SignatureComponent.Path)
            .WithMandatoryComponent(SignatureComponent.ContentType)
            // Include the 'x-my-header' signature from the response in the signature too, if present.
            .WithOptionalComponent(new HttpHeaderComponent("x-my-header"))
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
    ;

# Middleware configuration - register response signing before the actual middleware/controller handling the request:
app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/signed-responses"), builder => builder.UseResponseSigning());    
app.MapControllers();
```

You will also need to configure a signature provider that actually signs response messages. See
[NSign.SignatureProviders](https://nuget.org/packages/NSign.SignatureProviders) for currently available standard
implemenations. Register a signature provider for instance as follows:

```csharp
services
    .AddResponseSigning(new RsaPssSha512SignatureProvider(
        new X509Certificate2(@"path\to\certificate.pfx", "PasswordForPfx"),
        "my-cert"))
    ;
```

> **NOTE:** The signature provider must have access to the private key when asymmetric signatures are used. It must have
access to the shared key when symmetric signatures are used.

## Further Information


See also:
- [NSign on github.com](https://github.com/Unisys/NSign)
- [HTTP Message Signatures (current draft)](https://datatracker.ietf.org/doc/draft-ietf-httpbis-message-signatures/)
