using NSign.Providers;
using NSign.Signatures;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace NSign.BouncyCastle.Providers
{
    /// <summary>
    /// Implements <see cref="ISigner"/> and <see cref="IVerifier"/> for <c>EdDSA</c> using <c>ed2551</c>.
    /// </summary>
    public sealed class EdDsaEdwards25519SignatureProvider : SignatureProvider
    {
        /// <summary>
        /// The algorithm name define in the standard.
        /// </summary>
        private const string AlgorithmName = "ed25519";

        /// <summary>
        /// The <see cref="Ed25519PrivateKeyParameters"/> object that represents the private key
        /// to use for input signing.
        /// </summary>
        private readonly Ed25519PrivateKeyParameters? privateKey;

        /// <summary>
        /// The <see cref="Ed25519PublicKeyParameters"/> object that represents the public key to
        /// use for signature verification.
        /// </summary>
        private readonly Ed25519PublicKeyParameters publicKey;

        /// <summary>
        /// Initializes a new instance of <see cref="EdDsaEdwards25519SignatureProvider"/> using only
        /// a public key. This instance can only be used to verify signatures.
        /// </summary>
        /// <param name="publicKey">
        /// The <see cref="Ed25519PublicKeyParameters"/> object that represents the public key to
        /// use for signature verification.
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if
        /// the value should not be set / is not important.
        /// </param>
        public EdDsaEdwards25519SignatureProvider(Ed25519PublicKeyParameters publicKey, string? keyId)
            : this(null, publicKey, keyId)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="EdDsaEdwards25519SignatureProvider"/>.
        /// </summary>
        /// <param name="privateKey">
        /// The <see cref="Ed25519PrivateKeyParameters"/> object that represents the private key
        /// to use for input signing.
        /// </param>
        /// <param name="publicKey">
        /// The <see cref="Ed25519PublicKeyParameters"/> object that represents the public key to
        /// use for signature verification.
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if
        /// the value should not be set / is not important.
        /// </param>
        public EdDsaEdwards25519SignatureProvider(
            Ed25519PrivateKeyParameters? privateKey,
            Ed25519PublicKeyParameters publicKey,
            string? keyId) : base(keyId)
        {
            this.privateKey = privateKey;
            this.publicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        }

        /// <inheritdoc/>
        public override Task<ReadOnlyMemory<byte>> SignAsync(
            ReadOnlyMemory<byte> input,
            CancellationToken cancellationToken)
        {
            if (null == privateKey)
            {
                throw new InvalidOperationException(
                    "Cannot sign without a private key. " +
                    "Please make sure the provider is created with a valid private key.");
            }

            Ed25519Signer signer = new Ed25519Signer();
            signer.Init(forSigning: true, privateKey);
            signer.BlockUpdate(input.Span);
            byte[] signature = signer.GenerateSignature();

            return Task.FromResult(new ReadOnlyMemory<byte>(signature));
        }

        /// <inheritdoc/>
        public override void UpdateSignatureParams(SignatureParamsComponent signatureParams)
        {
            base.UpdateSignatureParams(signatureParams);
            signatureParams.Algorithm = AlgorithmName;
        }

        /// <inheritdoc/>
        public override Task<VerificationResult> VerifyAsync(
            SignatureParamsComponent signatureParams,
            ReadOnlyMemory<byte> input,
            ReadOnlyMemory<byte> expectedSignature,
            CancellationToken cancellationToken)
        {
            // If the signature parameters has the 'alg' parameter set, it must match the
            // algorithm provided with this instance. The same goes for the 'keyid' parameter,
            // provided that it is set with this instance.
            if (!signatureParams.HasMatchingAlgorithm(AlgorithmName) ||
                (!String.IsNullOrEmpty(KeyId) && !signatureParams.HasMatchingKeyId(KeyId)))
            {
                return Task.FromResult(VerificationResult.NoMatchingVerifierFound);
            }

            Ed25519Signer verifier = new Ed25519Signer();
            verifier.Init(forSigning: false, publicKey);
            verifier.BlockUpdate(input.Span);

            VerificationResult result = VerificationResult.SignatureMismatch;
            if (verifier.VerifySignature(expectedSignature.ToArray()))
            {
                result = VerificationResult.SuccessfullyVerified;
            }

            return Task.FromResult(result);
        }
    }
}