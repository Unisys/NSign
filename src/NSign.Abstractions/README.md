# NSign.Abstractions

Abstractions for signing and verification of signatures on HTTP messages. The abstractions by themselves are not very
meaningful in most cases, as they contain mostly interface definitions and abstract base classes. They are useful though
for building more extensions, or also things like middleware for other web server stacks or HTTP clients other than the
ASP.NET Core stack, for which the package [NSign.AspNetCore](https://nuget.org/packages/NSign.AspNetCore) already
provides middleware, or the `HttpClient` class from the `System.Net.Http` namespace, for which the
[NSign.Client](https://nuget.org/packages/NSign.Client) package already provides the middleware.

And, just as important, the package [NSign.SignatureProviders](https://nuget.org/packages/NSign.SignatureProviders)
holds implementations for standard signature algorithms outlined in RFC 9421 [^1].

## Further Information

See also:
- [NSign on github.com](https://github.com/Unisys/NSign)
- [HTTP Message Signatures (RFC 9421)](https://www.rfc-editor.org/rfc/rfc9421.html)

[^1]: See section _Signature Algorithms_ of _HTTP Message Signatures (RFC 9421)_,
	https://www.rfc-editor.org/rfc/rfc9421.html#name-signature-algorithms
