using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSign.Signatures;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using static NSign.RequestSigningOptions;

namespace NSign.Client
{
    /// <summary>
    /// Implements a <see cref="DelegatingHandler"/> that adds <c>Signature-Input</c> and <c>Signature</c> headers with
    /// signatures as per configured options to outgoing HTTP request messages.
    /// </summary>
    public sealed class SigningHandler : DelegatingHandler
    {
        /// <summary>
        /// The ILogger to use.
        /// </summary>
        private readonly ILogger<SigningHandler> logger;

        /// <summary>
        /// The <see cref="ISigner"/> to use to sign outgoing request messages.
        /// </summary>
        private readonly ISigner signer;

        /// <summary>
        /// The IOptions of <see cref="RequestSigningOptions"/> to define how to sign requests.
        /// </summary>
        private readonly IOptions<RequestSigningOptions> options;

        /// <summary>
        /// Initializes a new instance of SigningHandler.
        /// </summary>
        /// <param name="logger">
        /// The ILogger to use.
        /// </param>
        /// <param name="signer">
        /// The <see cref="ISigner"/> to use to sign outgoing request messages.
        /// </param>
        /// <param name="options">
        /// The IOptions of <see cref="RequestSigningOptions"/> to define how to sign requests.
        /// </param>
        public SigningHandler(ILogger<SigningHandler> logger, ISigner signer, IOptions<RequestSigningOptions> options)
        {
            this.logger = logger;
            this.signer = signer;
            this.options = options;
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestSigningOptions options = this.options.Value;

            if (String.IsNullOrWhiteSpace(options.SignatureName))
            {
                throw new InvalidOperationException("The SignatureName must be set to a non-blank string. Signing failed.");
            }

            SignatureInputSpec inputSpec = new SignatureInputSpec(options.SignatureName);

            options.SetParameters?.Invoke(inputSpec.SignatureParameters);

            foreach (ComponentSpec componentSpec in options.ComponentsToInclude)
            {
                // Add only fields which are mandatory, or refer to components that exist on the request.
                if (componentSpec.Mandatory ||
                    request.HasSignatureComponent(componentSpec.Component))
                {
                    inputSpec.SignatureParameters.AddComponent(componentSpec.Component);
                }
            }

            if (options.UseUpdateSignatureParams)
            {
                signer.UpdateSignatureParams(inputSpec.SignatureParameters);
            }

            byte[] signature = await signer.SignAsync(
                request.GetSignatureInput(inputSpec, out string sigInput), cancellationToken);

            logger.LogTrace("Using signature-input '{params}' for signature '{sig}' of request '{method} {url}'.",
                sigInput, inputSpec.Name, request.Method, request.RequestUri.OriginalString);

            request.Headers.Add(Constants.Headers.SignatureInput, $"{inputSpec.Name}={sigInput}");
            request.Headers.Add(Constants.Headers.Signature, $"{inputSpec.Name}=:{Convert.ToBase64String(signature)}:");

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
