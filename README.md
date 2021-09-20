![GitHub](https://img.shields.io/github/license/rogerk-unifysquare/nsign)
![GitHub Workflow Status](https://img.shields.io/github/workflow/status/rogerk-unifysquare/NSign/Build%20and%20Test)
![GitHub issues](https://img.shields.io/github/issues/rogerk-unifysquare/NSign)
![GitHub pull requests](https://img.shields.io/github/issues-pr/rogerk-unifysquare/NSign)

# NSign - HTTP message signatures and verification for .NET

NSign (/ˈensaɪn/) provides libraries to sign HTTP messages based on the most recent (Aug 2021) draft of the
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
| NSign.Abstractions | Abstractions (interfaces, object model, etc) for all NSign libraries. | ![Nuget](https://img.shields.io/nuget/v/NSign.Abstractions) ![Nuget](https://img.shields.io/nuget/dt/NSign.Abstractions) |
| NSign.SignatureProviders | Signature providers (signers and verifiers) for symmetric and asymmetric signatures. | ![Nuget](https://img.shields.io/nuget/v/NSign.SignatureProviders) ![Nuget](https://img.shields.io/nuget/dt/NSign.SignatureProviders) |
| NSign.AspNetCore | Middleware for verifying signatures on HTTP requests. | ![Nuget](https://img.shields.io/nuget/v/NSign.AspNetCore) ![Nuget](https://img.shields.io/nuget/dt/NSign.AspNetCore) |
| NSign.Client | HTTP message pipeline handlers for signing HTTP request messages. | ![Nuget](https://img.shields.io/nuget/v/NSign.Client) ![Nuget](https://img.shields.io/nuget/dt/NSign.Client) |

Please note that initially the `NSign.AspNetCore` and `NSign.Client` libraries are targeting HTTP *request* messages only.
It's planned however to add support for signing HTTP *response* messages in `NSign.AspNetCore` and verify signatures on
them in `NSign.Client` at a later stage too.

More information to follow ... stay tuned.
