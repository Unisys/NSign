using NSign.Signatures;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace NSign.Providers
{
    /// <summary>
    /// Base class for RSA (asymmetric) signature providers.
    /// </summary>
    public abstract class RsaSignatureProvider : SignatureProvider, IDisposable
    {
        /// <summary>
        /// The private key used to sign.
        /// </summary>
        private readonly RSA? privateKey;

        /// <summary>
        /// The public key used to verify signatures.
        /// </summary>
        private readonly RSA publicKey;

        /// <summary>
        /// The name of the symmetric signature algorithm provided by this instance.
        /// </summary>
        private readonly string algorithmName;

        /// <summary>
        /// Initializes a new instance of RsaSignatureProvider.
        /// </summary>
        /// <param name="certificate">
        /// The <see cref="X509Certificate2"/> to use to get the public key and the private key (only needed if the
        /// provider is used to create signatures).
        /// </param>
        /// <param name="algorithmName">
        /// The name of the asymmetric signature algorithm provided by this instance.
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if the value should not
        /// be set / is not important.
        /// </param>
        public RsaSignatureProvider(X509Certificate2 certificate, string algorithmName, string? keyId) :
            this(
                // Also check that the certificate is not null.
                (certificate ?? throw new ArgumentNullException(nameof(certificate))).GetRSAPrivateKey(),
                // Also check that the certificate uses RSA keys.
                certificate.GetRSAPublicKey() ??
                    throw new ArgumentException("The certificate does not use RSA keys.", nameof(certificate)),
                algorithmName,
                keyId)
        {
            Certificate = certificate;
        }

        /// <summary>
        /// Initializes a new instance of RsaSignatureProvider.
        /// </summary>
        /// <param name="privateKey">
        /// The <see cref="RSA"/> object that represents the private key or null if signing with this provider is not
        /// needed.
        /// </param>
        /// <param name="publicKey">
        /// The <see cref="RSA"/> object that represents the public key to use for signature verification.
        /// </param>
        /// <param name="algorithmName">
        /// The name of the asymmetric signature algorithm provided by this instance.
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if the value should not
        /// be set / is not important.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="publicKey"/> is null.
        /// </exception>
        public RsaSignatureProvider(RSA? privateKey, RSA publicKey, string algorithmName, string? keyId) : base(keyId)
        {
            this.privateKey = privateKey;
            this.publicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));

            if (String.IsNullOrWhiteSpace(algorithmName))
            {
                throw new ArgumentNullException(nameof(algorithmName));
            }
            this.algorithmName = algorithmName;
        }

        /// <summary>
        /// Gets the <see cref="X509Certificate2"/> to use to get the public key and the private key.
        /// </summary>
        /// <remarks>
        /// A certificate with access to the private key is needed only when signatures must be created.
        /// </remarks>
        public X509Certificate2? Certificate { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (null != privateKey)
            {
                privateKey.Dispose();
            }

            if (null != publicKey)
            {
                publicKey.Dispose();
            }

            if (null != Certificate)
            {
                Certificate.Dispose();
            }
        }

        /// <inheritdoc/>
        public override Task<ReadOnlyMemory<byte>> SignAsync(ReadOnlyMemory<byte> input, CancellationToken cancellationToken)
        {
            if (null == privateKey)
            {
                throw new InvalidOperationException("Cannot sign using a certificate without a private key.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(
                new ReadOnlyMemory<byte>(privateKey.SignData(input.ToArray(), SignatureHash, SignaturePadding)));
        }

        /// <inheritdoc/>
        public override Task<VerificationResult> VerifyAsync(
            SignatureParamsComponent signatureParams,
            ReadOnlyMemory<byte> input,
            ReadOnlyMemory<byte> expectedSignature,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // If the signature parameters has the 'alg' parameter set, it must match the algorithm provided with this
            // instance. The same goes for the 'keyid' parameter, provided that it is set with this instance.
            if (!signatureParams.HasMatchingAlgorithm(algorithmName) ||
                (!String.IsNullOrEmpty(KeyId) && !signatureParams.HasMatchingKeyId(KeyId!)))
            {
                return Task.FromResult(VerificationResult.NoMatchingVerifierFound);
            }

            VerificationResult result = VerificationResult.SignatureMismatch;
#if NETSTANDARD2_0
            if (publicKey.VerifyData(input.Span.ToArray(), expectedSignature.Span.ToArray(), SignatureHash, SignaturePadding))
#elif NETSTANDARD2_1_OR_GREATER || NET
            if (publicKey.VerifyData(input.Span, expectedSignature.Span, SignatureHash, SignaturePadding))
#endif
            {
                result = VerificationResult.SuccessfullyVerified;
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public override void UpdateSignatureParams(SignatureParamsComponent signatureParams)
        {
            base.UpdateSignatureParams(signatureParams);

            if (!String.IsNullOrWhiteSpace(algorithmName))
            {
                signatureParams.Algorithm = algorithmName;
            }
        }

        /// <summary>
        /// Gets the <see cref="HashAlgorithmName"/> struct identifiying the hash to use for the signatures.
        /// </summary>
        protected abstract HashAlgorithmName SignatureHash { get; }

        /// <summary>
        /// Gets the <see cref="RSASignaturePadding"/> object identifying the signature padding mode to use for the signatures.
        /// </summary>
        protected abstract RSASignaturePadding SignaturePadding { get; }
    }
}
