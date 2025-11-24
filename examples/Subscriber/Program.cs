using NSign;
using NSign.AspNetCore;
using NSign.Providers;
using NSign.Signatures;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers().Services
    .Configure<ContentDigestVerificationOptions>(o =>
        o.Behavior |= ContentDigestVerificationOptions.VerificationBehavior.RequireOnlySingleMatch)
    .Configure<RequestSignatureVerificationOptions>((options) =>
    {
        options.TagsToVerify.Add("nsign-example-publisher");
        options.RequiredSignatureComponents.Add(SignatureComponent.ContentDigest);
        options.RequiredSignatureComponents.Add(new HttpHeaderStructuredFieldComponent(Constants.Headers.ContentType));
        options.CreatedRequired =
            options.ExpiresRequired =
            options.KeyIdRequired =
            options.AlgorithmRequired =
            options.TagRequired = true;
        options.MissingSignatureResponseStatus = 404;
        options.MaxSignatureAge = TimeSpan.FromMinutes(5);

        options.VerifyNonce = (SignatureParamsComponent signatureParams) =>
        {
            Console.WriteLine($"Got signature with tag={signatureParams.Tag} and nonce={signatureParams.Nonce}.");
            // TODO: Actually verify that the nonce was never used before.
            return true;
        };
    })
    .AddSignatureVerification(
        new RsaPssSha512SignatureProvider(
            X509CertificateLoader.LoadCertificateFromFile(@"examples.nsign.local.cer"),
            "examples.nsign.local"))
    .AddContentDigestVerification()
    ;

var app = builder.Build();

app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/webhooks"), ValidateSignatureAndDigest);
app.MapControllers();

app.Run();

static void ValidateSignatureAndDigest(IApplicationBuilder builder)
{
    builder.UseSignatureVerification().UseContentDigestVerification();
}
