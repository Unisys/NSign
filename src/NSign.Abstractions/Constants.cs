namespace NSign
{
    /// <summary>
    /// Constants used for signing and verification of HTTP messages.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Constants for HTTP message headers.
        /// </summary>
        public static class Headers
        {
            /// <summary>
            /// The name of the header holding signature input specifications.
            /// </summary>
            public const string SignatureInput = "signature-input";

            /// <summary>
            /// The name of the header holding signatures.
            /// </summary>
            public const string Signature = "signature";

            /// <summary>
            /// The name of the header holding message body digest values.
            /// </summary>
            public const string Digest = "digest";

            /// <summary>
            /// The name of the header holding message body content type.
            /// </summary>
            public const string ContentType = "content-type";

            /// <summary>
            /// The name of the header holding message body content length.
            /// </summary>
            public const string ContentLength = "content-length";
        }

        /// <summary>
        /// Constants for HTTP message signing derived components.
        /// </summary>
        public static class DerivedComponents
        {
            /// <summary>
            /// The name of the derived component holding signature parameters.
            /// </summary>
            public const string SignatureParams = "@signature-params";

            /// <summary>
            /// The name of the derived component holding the HTTP method of the message's associated request.
            /// </summary>
            public const string Method = "@method";

            /// <summary>
            /// The name of the derived component holding the full target URI of the message's associated request.
            /// </summary>
            public const string TargetUri = "@target-uri";

            /// <summary>
            /// The name of the derived component holding the authority (host and port) of the message's associated request.
            /// </summary>
            public const string Authority = "@authority";

            /// <summary>
            /// The name of the derived component holding the schema of the message's associated request.
            /// </summary>
            public const string Scheme = "@scheme";

            /// <summary>
            /// The name of the derived component holding the target (path and query) of the message's associated request.
            /// </summary>
            public const string RequestTarget = "@request-target";

            /// <summary>
            /// The name of the derived component holding the path of the message's associated request.
            /// </summary>
            public const string Path = "@path";

            /// <summary>
            /// The name of the derived component holding the query of the message's associated request.
            /// </summary>
            public const string Query = "@query";

            /// <summary>
            /// The name of the derived component holding the dictionary-structured query parameter of a given name from
            /// the message's associated request.
            /// </summary>
            public const string QueryParam = "@query-param";

            /// <summary>
            /// The name of the derived component holding the status of the message's associated response.
            /// </summary>
            public const string Status = "@status";
        }

        /// <summary>
        /// Constants for signature component parameters as used e.g. in dictionary structured components.
        /// </summary>
        public static class ComponentParameters
        {
            /// <summary>
            /// The name of the 'key' parameter.
            /// </summary>
            public const string Key = "key";

            /// <summary>
            /// The name of the 'name' parameter.
            /// </summary>
            public const string Name = "name";

            /// <summary>
            /// The name of the 'req' parameter.
            /// </summary>
            public const string Request = "req";

            /// <summary>
            /// The name of the 'sf' parameter.
            /// </summary>
            public const string StructuredField = "sf";

            /// <summary>
            /// The name of the 'bs' parameter.
            /// </summary>
            public const string ByteSequence = "bs";

            /// <summary>
            /// The name of the 'tr' parameter.
            /// </summary>
            public const string FromTrailers = "tr";
        }

        /// <summary>
        /// Constants for signature parameters.
        /// </summary>
        public static class SignatureParams
        {
            /// <summary>
            /// The name of the 'created' parameter.
            /// </summary>
            public const string Created = "created";

            /// <summary>
            /// The name of the 'expires' parameter.
            /// </summary>
            public const string Expires = "expires";

            /// <summary>
            /// The name of the 'nonce' parameter.
            /// </summary>
            public const string Nonce = "nonce";

            /// <summary>
            /// The name of the 'alg' parameter.
            /// </summary>
            public const string Alg = "alg";

            /// <summary>
            /// The name of the 'keyid' parameter.
            /// </summary>
            public const string KeyId = "keyid";

            /// <summary>
            /// The name of the 'tag' parameter.
            /// </summary>
            public const string Tag = "tag";
        }

        /// <summary>
        /// Constants for names of hash algorithms.
        /// </summary>
        public static class HashAlgorithms
        {
            /// <summary>
            /// The name of the SHA-256 hash algorithm.
            /// </summary>
            public const string Sha256 = "SHA-256";

            /// <summary>
            /// The name of the SHA-512 hash algorithm.
            /// </summary>
            public const string Sha512 = "SHA-512";
        }

        /// <summary>
        /// Constants for names of signature algorithms.
        /// </summary>
        public static class SignatureAlgorithms
        {
            /// <summary>
            /// The name for signatues using RSA with PSS signature padding and SHA-512 signature hashes.
            /// </summary>
            public const string RsaPssSha512 = "rsa-pss-sha512";

            /// <summary>
            /// The name for signatures using RSA with PKCS #1 v1.5 signature padding and SHA-256 signature hashes.
            /// </summary>
            public const string RsaPkcs15Sha256 = "rsa-v1_5-sha256";

            /// <summary>
            /// The name for signatures using HMAC SHA-256.
            /// </summary>
            public const string HmacSha256 = "hmac-sha256";

            /// <summary>
            /// The name for signatures using ECDSA with curve P-256 and SHA-256.
            /// </summary>
            public const string EcdsaP256Sha256 = "ecdsa-p256-sha256";

            /// <summary>
            /// The name for signatures using ECDSA with curve P-384 and SHA-384.
            /// </summary>
            public const string EcdsaP384Sha384 = "ecdsa-p384-sha384";
        }
    }
}
