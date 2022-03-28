using NSign.Signatures;
using System.Security.Cryptography;

namespace NSign.Providers
{
    /// <summary>
    /// Provides HMAC SHA-256 signatures.
    /// </summary>
    public sealed class HmacSha256SignatureProvider : HmacSignatureProvider
    {
        /// <summary>
        /// The key used for signing.
        /// </summary>
        private readonly byte[] key;

        /// <summary>
        /// Initializes a new instance of HmacSha256SignatureProvider.
        /// </summary>
        /// <param name="key">
        /// The key used for signing.
        /// </param>
        public HmacSha256SignatureProvider(byte[] key) : this(key, null) { }

        /// <summary>
        /// Initializes a new instance of HmacSha256SignatureProvider.
        /// </summary>
        /// <param name="key">
        /// The key used for signing.
        /// </param>
        /// <param name="keyId">
        /// The value for the KeyId parameter of signatures produced with this provider or null if the value should not
        /// be set / is not important.
        /// </param>
        public HmacSha256SignatureProvider(byte[] key, string? keyId)
            : base(Constants.SignatureAlgorithms.HmacSha256, keyId)
        {
            this.key = key;
        }

        /// <inheritdoc/>
        public override void UpdateSignatureParams(SignatureParamsComponent signatureParams)
        {
            base.UpdateSignatureParams(signatureParams);
            signatureParams.Algorithm = Constants.SignatureAlgorithms.HmacSha256;
        }

        /// <inheritdoc/>
        protected override HMAC GetAlgorithm() => new HMACSHA256(key);
    }
}
