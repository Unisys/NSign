# NSign extensions using BouncyCastle

This library must be used together with other NSign libraries. It provides
support for signatures and signature verifications for EdDSA using curve
edwards25519 [^1].

## Usage

### Signature provider for EdDSA using cure edwards 25519

```csharp
var provider = new EdDsaEdwards25519SignatureProvider(
	privateKey,
	publicKey,
	"the-key-id"));
```

Here, `privateKey` and `publicKey` are instances of `Ed25519PrivateKeyParameters` and
`Ed25519PublicKeyParameters` from the [BouncyCastle.Cryptography](https://nuget.org/packages/BouncyCastle.Cryptography)
package respectively.

If you have the keys in PEM-formatted files, you can use the `PemReader` from the same BouncyCastle
package to read those keys. If you have a .pfx or a .cer file, you can use `openssl` to extract the
keys. For instance:

```bash
# Extract the ed25519 private key from a .pfx file holding an ed25519 private key:
openssl pkcs12 -in my.pfx -nocerts -nodes -out my-priv.pem

# Extract the public key from the above extracted private key
openssl pkey -in my-priv.pem -pubout -out my-pub.pem

## OR, if you just have the public key in a certificate file:
openssl x509 -in my.cer -pubkey -nocert -out my-pub.pem
```

Make sure to consult the documentation of your `openssl` installation for more details.

## Further Information

See also:
- [NSign on github.com](https://github.com/Unisys/NSign)
- [HTTP Message Signatures (RFC 9421)](https://www.rfc-editor.org/rfc/rfc9421.html)

[^1]: See section _EdDSA Using Curve edwards25519_ of _HTTP Message Signatures (RFC 9421)_,
	https://www.rfc-editor.org/rfc/rfc9421.html#name-eddsa-using-curve-edwards25
