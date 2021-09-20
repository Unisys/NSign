using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using static NSign.Client.AddDigestOptions;

namespace NSign.Client
{
    /// <summary>
    /// Implements a <see cref="DelegatingHandler"/> that adds <c>Digest</c> headers with hashes for request bodies to
    /// outgoing requests.
    /// </summary>
    public sealed class AddDigestHandler : DelegatingHandler
    {
        /// <summary>
        /// The IOptions of AddDigestOptions defining which hashes to add.
        /// </summary>
        private readonly IOptions<AddDigestOptions> options;

        /// <summary>
        /// Initializes a new instance of AddDigestHandler.
        /// </summary>
        /// <param name="options">
        /// The IOptions of AddDigestOptions defining which hashes to add.
        /// </param>
        public AddDigestHandler(IOptions<AddDigestOptions> options)
        {
            this.options = options;
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AddDigestOptions options = this.options.Value;

            if (null != request.Content)
            {
                foreach (Hash hash in options.Hashes)
                {
                    request.Content.Headers.Add(Constants.Headers.Digest, await GetDigestValueAsync(request.Content, hash));
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Gets the digest header value for the given content and hashAlgorithm asynchronously.
        /// </summary>
        /// <param name="content">
        /// The HttpContext object describing the content to hash.
        /// </param>
        /// <param name="hashAlgorithm">
        /// The Hash algorithm to use for hashing.
        /// </param>
        /// <returns>
        /// A string value that represents the value for the 'Digest' for the content.
        /// </returns>
        private static Task<string> GetDigestValueAsync(HttpContent content, Hash hashAlgorithm)
        {
            using HashAlgorithm hash = GetConfiguredHash(hashAlgorithm, out string algName);

            // We must use HttpContent.CopyToAsync to get a copy of the stream without breaking the request pipeline
            // because the content stream would otherwise be closed by the time it needs to be sent over the wire. The
            // easiest way to achieve is is to use a pipe where the content stream is written to, and in parallel read
            // from for hashing.
            Pipe contentPipe = new Pipe();
            Task writeBody = WriteHttpContentAsync(content, contentPipe);

            using Stream streamToHash = contentPipe.Reader.AsStream();
            byte[] hashOutput = hash.ComputeHash(streamToHash);

            return writeBody
                .ContinueWith(_ => $"{algName}={Convert.ToBase64String(hashOutput, Base64FormattingOptions.None)}");
        }

        /// <summary>
        /// Gets the HashAlgorithm and the corresponding name for the 'Digest' header.
        /// </summary>
        /// <param name="alg">
        /// The Hash value that defines which hash algorithm to use for the 'Digest' header.
        /// </param>
        /// <param name="algName">
        /// If successful, holds the name of the hash algorithm to be used as the key for the 'Digest' header value.
        /// </param>
        /// <returns>
        /// An instance of HashAlgorithm that can be used to hash the request body.
        /// </returns>
        private static HashAlgorithm GetConfiguredHash(Hash alg, out string algName)
        {
            switch (alg)
            {
                case Hash.Sha256:
                    algName = Constants.HashAlgorithms.Sha256;
                    return SHA256.Create();

                case Hash.Sha512:
                    algName = Constants.HashAlgorithms.Sha512;
                    return SHA512.Create();

                default:
                    throw new NotSupportedException($"Hash algorithm '{alg}' is not supported.");
            }
        }

        /// <summary>
        /// Writes the given content to the specified contentPipe asynchronously.
        /// </summary>
        /// <param name="content">
        /// The HttpContent object defining the content to read.
        /// </param>
        /// <param name="contentPipe">
        /// The Pipe object to which to write the content.
        /// </param>
        /// <returns>
        /// A Task which tracks completion of the operation.
        /// </returns>
        private static Task WriteHttpContentAsync(HttpContent content, Pipe contentPipe)
        {
            using Stream contentStream = contentPipe.Writer.AsStream();

            return content.CopyToAsync(contentStream).ContinueWith(t => contentStream.Dispose());
        }
    }
}
