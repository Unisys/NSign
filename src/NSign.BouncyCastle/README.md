# NSign extensions using BouncyCastle

This library must be used together with other NSign libraries. It provides support for signatures
and signature verifications for EdDSA using curve edwards25519.

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
package to read those keys. If you have a PFX file with private and public key, you can extract
the private key using `Pkcs12Store` (again, from the same BouncyCastle package). The public key
can be extracted easily either from a PFX or a certificate file using something like

```csharp
var publicKey = new Ed25519PublicKeyParameters(certificate.GetPublicKey());
```
