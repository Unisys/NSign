using NSign;
using NSign.Signatures;
using RuntimeKeySelection;
using RuntimeKeySelection.Security;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddHttpContextAccessor()
    .AddControllers()
    .Services

    .AddAuthentication(BasicAuthentication.Scheme)
    .AddScheme<BasicAuthentication.Options, BasicAuthentication.Handler>(BasicAuthentication.Scheme, BasicAuthentication.Default)
    .Services

    .Configure<OutboundCallOptions>(builder.Configuration.GetSection("Outbound"))
    .AddHttpClient("SignedMessageSender")
    .ConfigureHttpClient((client) =>
    {
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NSignExample.RuntimeKeySelection", "0.1"));
    })
    .AddSigningHandler()
    .Services


    .ConfigureMessageSigningOptions(options =>
    {
        options.SignatureName = "mysig";
        options
            .WithMandatoryComponent(SignatureComponent.ContentType)
            .WithOptionalComponent(SignatureComponent.ContentLength)
            .SetParameters = (signingOptions) => signingOptions
                .WithCreated(DateTimeOffset.UtcNow.AddMinutes(-2))
                .WithExpires(TimeSpan.FromMinutes(10))
            ;
    })
    .Services

    .AddSingleton<ISigner, UserBasedSignatureProvider>()
    ;

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
