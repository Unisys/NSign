using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSign.Signatures;
using System;
using System.Threading.Tasks;
using static NSign.MessageSigningOptions;

namespace NSign.AspNetCore
{
    /// <summary>
    /// Implements an AspNetCore middleware that adds signatures to response messages.
    /// </summary>
    public sealed partial class ResponseSigningMiddleware : IMiddleware
    {
        /// <summary>
        /// The ILogger to use.
        /// </summary>
        private readonly ILogger<ResponseSigningMiddleware> logger;

        /// <summary>
        /// The <see cref="ISigner"/> to use to sign outgoing response messages.
        /// </summary>
        private readonly ISigner signer;

        /// <summary>
        /// The IOptions of <see cref="MessageSigningOptions"/> to define how to sign responses.
        /// </summary>
        private readonly IOptions<MessageSigningOptions> options;

        /// <summary>
        /// Initializes a new instance of ResponseSigningMiddleware.
        /// </summary>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        /// <param name="signer">
        /// The <see cref="ISigner"/> to use to sign outgoing response messages.
        /// </param>
        /// <param name="options">
        /// The IOptions of <see cref="MessageSigningOptions"/> to define how to sign responses.
        /// </param>
        public ResponseSigningMiddleware(
            ILogger<ResponseSigningMiddleware> logger,
            ISigner signer,
            IOptions<MessageSigningOptions> options)
        {
            this.logger = logger;
            this.signer = signer;
            this.options = options;
        }

        /// <inheritdoc/>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            MessageSigningOptions options = this.options.Value;
            if (String.IsNullOrWhiteSpace(options.SignatureName))
            {
                throw new InvalidOperationException("The SignatureName must be set to a non-blank string. Signing failed.");
            }

            // Register a handler for the event of the response starting before we're done here.
            ResponseContext responseCtx = new ResponseContext(context, options, signer, logger);
            context.Response.OnStarting(responseCtx.OnResponseStarting);

            await next(context);
        }

        /// <summary>
        /// Keeps context for a single request/response pipeline where the response should be signed.
        /// </summary>
        private readonly struct ResponseContext
        {
            /// <summary>
            /// Initializes a new instance of ResponseContext.
            /// </summary>
            /// <param name="httpContext">
            /// The <see cref="HttpContext"/> defining the request/response pipeline for which to sign the response.
            /// </param>
            /// <param name="options">
            /// The <see cref="MessageSigningOptions"/> object that defines how to sign responses.
            /// </param>
            /// <param name="signer">
            /// The <see cref="ISigner"/> to use to sign outgoing response messages.
            /// </param>
            /// <param name="logger">
            /// The ILogger to use.
            /// </param>
            public ResponseContext(HttpContext httpContext, MessageSigningOptions options, ISigner signer, ILogger logger)
            {
                HttpContext = httpContext;
                Options = options;
                Signer = signer;
                Logger = logger;
            }

            /// <summary>
            /// The <see cref="HttpContext"/> defining the request/response pipeline for which to sign the response.
            /// </summary>
            public HttpContext HttpContext { get; }

            /// <summary>
            /// The <see cref="MessageSigningOptions"/> object that defines how to sign responses.
            /// </summary>
            public MessageSigningOptions Options { get; }

            /// <summary>
            /// The <see cref="ISigner"/> to use to sign outgoing response messages.
            /// </summary>
            public ISigner Signer { get; }

            /// <summary>
            /// The ILogger to use.
            /// </summary>
            public ILogger Logger { get; }

            /// <summary>
            /// Handles the 'Starting' event of the response to be signed. This let's us add response headers after the
            /// response status has been defined and before the response body is written.
            /// </summary>
            /// <returns>
            /// A Task which tracks completion of the operation.
            /// </returns>
            public async Task OnResponseStarting()
            {
                SignatureInputSpec inputSpec = new SignatureInputSpec(Options.SignatureName);
                Options.SetParameters?.Invoke(inputSpec.SignatureParameters);

                foreach (ComponentSpec componentSpec in Options.ComponentsToInclude)
                {
                    // Add only fields which are mandatory, or refer to components that exist on the request.
                    if (componentSpec.Mandatory ||
                        HttpContext.HasSignatureComponent(componentSpec.Component))
                    {
                        inputSpec.SignatureParameters.AddComponent(componentSpec.Component);
                    }
                }

                if (Options.UseUpdateSignatureParams)
                {
                    Signer.UpdateSignatureParams(inputSpec.SignatureParameters);
                }

                byte[] signature = await Signer.SignAsync(
                    HttpContext.GetSignatureInput(inputSpec, out string sigInput),
                    HttpContext.RequestAborted);

                if (Logger.IsEnabled(LogLevel.Trace))
                {
                    HttpRequest request = HttpContext.Request;
                    Logger.LogTrace("Using signature-input '{input}' for signature '{sig}' of response to request '{method} {url}'.",
                        sigInput, inputSpec.Name, request.Method, request.PathBase + request.Path + request.QueryString);
                }

                // It's time to add the 'signature-input' and 'signature' headers.
                HttpResponse response = HttpContext.Response;
                response.Headers.Add(Constants.Headers.SignatureInput, $"{inputSpec.Name}={sigInput}");
                response.Headers.Add(Constants.Headers.Signature, $"{inputSpec.Name}=:{Convert.ToBase64String(signature)}:");
            }
        }
    }
}
