# NSign.SignatureProviders

Signature providers for signing and verifying signatures with NSign. This library currently supports both asymmetric
algorithms (using public-key cryptography) and symmetric algorithms (using HMAC). Signature providers typically
implement both the `ISigner` and the `IVerifier` interfaces, but implementation can also be split into separate classes.

## Asymmetric Signature Algorithms

Currently, the following asymmetrics signature algorithms are supported:

- ECDSA using curve P-256 DSS and SHA-256 (`ecdsa-p256-sha256` in [^1]), in class `ECDsaP256Sha256SignatureProvider`
- ECDSA using curve P-384 DSS and SHA-384 (`ecdsa-p384-sha384` in [^1]), in class `ECDsaP382Sha384SignatureProvider`
- RSASSA-PSS using SHA-512 (`rsa-pss-sha512` in [^1]) in class `RsaPssSha512SignatureProvider`
- RSASSA-PKCS1-v1_5 using SHA-256 (`rsa-v1_5-sha256` in [^1]) in class `RsaPkcs15Sha256SignatureProvider`

These signature providers can all be created by passing an instance of `X509Certificate2` and having the provider
extract the public key for signature verification from there. If the provider is to be used for signing, the certificate
that is provided **must** have a private key too, otherwise signing will fail / an exception will be thrown. Naturally,
the keys used in the certificate **must** match the key parameters/formats expected by the signature provider.

For instance, to use `rsa-pss-sha512` with a PEM-encoded certificate in a file called `the-cert.cer` for signature
verification, creating the provider as follows will do:

```csharp
var provider = new RsaPssSha512SignatureProvider(
	new X509Certificate2("the-cert.cer"),
	"the-cert-key-id"))
```

To use `rsa-pss-sha512` with a PFX file called `the-cert.pfx`, holding the private key for message signing, a provider
can be created as follows:

```csharp
var provider = new RsaPssSha512SignatureProvider(
	new X509Certificate2("the-cert.pfx", "here-goes-the-password-to-the-PFX"),
	"the-cert-key-id"))
```

Due to their nature, asymmetric signatures are often preferable over symmetric signatures because they do not require
both the signing and verifying party to share a secret (the key). Instead, the public key can be published anywhere /
through any means for verifiers to download and use.

## Symmetric Signature Algorithms

Currently, the following symmetric signature algorithms are supported:

- HMAC using SHA-256 (`hmac-sha256` in [^1]), in class `HmacSha256SignatureProvider`

This signature provider requires the (shared) key to be provided during construction.

## Further Information

See also:
- [NSign on github.com](https://github.com/Unisys/NSign)
- [HTTP Message Signatures (RFC 9421)](https://www.rfc-editor.org/rfc/rfc9421.html)

[^1]: See section _Signature Algorithms_ of _HTTP Message Signatures (RFC 9421)_,
	https://www.rfc-editor.org/rfc/rfc9421.html#name-signature-algorithms
