# NSign - HTTP message signatures and verification for .NET

NSign (/ˈɛn.sən/) provides libraries to sign HTTP messages based on the most recent (Aug 2021) draft of the
[HTTP Message Signatures](https://datatracker.ietf.org/doc/draft-ietf-httpbis-message-signatures/) to-be standard from
the IETF. The key motivation for the standard is to have a standard way to sign and verify HTTP messages e.g. used in
webhook-like scenarios where a provider needs to sign HTTP request messages before sending them to subscribers, and
subscribers need to verify incoming messages' signatures for authentication. Signatures can however also be applied to
HTTP response messages for a client to verify on receipt.

__*Disclaimer*__: Since the standard is currently in draft state, much like the standard itself, the libraries and its
interfaces and implementations are subject to change.

## Libraries and Nuget packages

| Library | Purpose | Nuget package |
|---|---|---|
| NSign.Abstractions | Abstractions (interfaces, object model, etc) for all NSign libraries | Coming soon ... |
| NSign.Providers | Signature providers (signers and verifiers) for symmetric and asymmetric signatures | Coming soon ... |
| NSign.AspNetCore | Middleware for verifying signatures on HTTP requests. | Coming soon ... |
| NSign.Client | HTTP message pipeline handlers for signing HTTP request messages. | Coming soon ... |

Please note that initially the `NSign.AspNetCore` and `NSign.Client` libraries are targeting HTTP *request* messages only.
It's planned however to add support for signing HTTP *response* messages in `NSign.AspNetCore` and verify signatures on
them in `NSign.Client` at a later stage too.

More information to follow ... stay tuned.