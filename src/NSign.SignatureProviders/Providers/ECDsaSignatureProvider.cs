using NSign.Signatures;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace NSign.Providers
{
    /// <summary>
    /// Base class for ECDSA (asymmetric) signature providers.
    /// </summary>
    public abstract class ECDsaSignatureProvider : SignatureProvider, IDisposable
    {
        /// <summary>
        /// The private key used to sign.
        /// </summary>
        private readonly ECDsa? privateKey;

        /// <summary>
        /// The public key used to verify signatures.
        /// </summary>
        private readonly ECDsa publicKey;

        /// <summary>
        /// The OID value of the curve required to use this signature.
        /// </summary>
        private readonly string requiredCurveOid;

        /// <summary>
        /// The name of the curve rqeuired to use this instance.
        /// </summary>
        private readonly string requiredCurveName;

        /// <summary>
        /// The name of the symmetric signature algorithm provided by this instance.
        /// </summary>
        private readonly string algorithmName;

        /// <summary>
        /// Initializes a new instance of ECDsaSignatureProvider.
        /// </summary>
        /// <param name="certificate">
        /// The <see cref="X509Certificate2"/> to use to get the public key and the private key (only needed if the
        /// provider is used to create signatures).
        /// </param>
        /// <param name="requiredCurveOid">
        /// The OID value of the curve required to use this instance.
        /// </param>
        /// <param name="requiredCurveName">
        /// The name of the curve rqeuired to use this instance.
        /// </param>
        /// <param name="algorithmName">
        /// The name of the asymmetric signature algorithm provided by this instance.
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if the value should not
        /// be set / is not important.
        /// </param>
        public ECDsaSignatureProvider(
            X509Certificate2 certificate,
            string requiredCurveOid,
            string requiredCurveName,
            string algorithmName,
            string? keyId
        ) : base(keyId)
        {
            Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));

            privateKey = Certificate.GetECDsaPrivateKey();
            publicKey = Certificate.GetECDsaPublicKey();

            this.requiredCurveOid = requiredCurveOid;
            this.requiredCurveName = requiredCurveName;

            // Give derived classes a chance to reject the certificate because the curve doesn't match.
            CheckKeyAlgorithm(publicKey, nameof(certificate));

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
        public X509Certificate2 Certificate { get; }

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

            return Task.FromResult(new ReadOnlyMemory<byte>(privateKey.SignData(input.ToArray(), SignatureHash)));
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
                (!String.IsNullOrEmpty(KeyId) && !signatureParams.HasMatchingKeyId(KeyId)))
            {
                return Task.FromResult(VerificationResult.NoMatchingVerifierFound);
            }

            VerificationResult result = VerificationResult.SignatureMismatch;
            if (publicKey.VerifyData(input.Span, expectedSignature.Span, SignatureHash))
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
        /// Checks if the given public key matches the parameters required by this signature provider.
        /// </summary>
        /// <param name="publicKey">
        /// The ECDsa object representing the public key of the certificate.
        /// </param>
        /// <param name="parameterName">
        /// The name of the parameter which held the certificate the given public key is from.
        /// </param>
        /// <remarks>
        /// The base implementation (<see cref="CheckKeyAlgorithm"/>) only checks that there is a valid ECDsa key. It
        /// does not check for a specific elliptic curve or other parameters of the key.
        /// </remarks>
        protected virtual void CheckKeyAlgorithm(ECDsa publicKey, string parameterName)
        {
            if (null == publicKey)
            {
                throw new ArgumentException("The certificate does not use elliptic curve keys.", parameterName);
            }

            ECParameters parameters = publicKey.ExportParameters(false);

            if (parameters.Curve.Oid.Value != requiredCurveOid)
            {
                throw new ArgumentException(
                    $"A certificate with elliptic curve {requiredCurveName} (oid: {requiredCurveOid}) is expected, " +
                    $"but curve '{parameters.Curve.Oid.FriendlyName}' (oid: {parameters.Curve.Oid.Value}) was provided.",
                    parameterName);
            }
        }
    }
}
