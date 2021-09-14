# NSign - HTTP message signatures and verification for .NET

NSign (/ˈɛn.sən/) provides libraries to sign HTTP messages based on the most recent (Aug 2021) draft of the
[HTTP Message Signatures](https://datatracker.ietf.org/doc/draft-ietf-httpbis-message-signatures/) to-be standard from
the IETF. The key motivation for the standard is to have a standard way to sign and verify HTTP messages e.g. used in
webhook-like scenarios where a provider needs to sign HTTP request messages before sending them to subscribers, and
subscribers need to verify incoming messages' signatures for authentication. Signatures can however also be applied to
HTTP response messages for a client to verify on receipt.

__*Disclaimer*__: Since the standard is currently in draft state, much like the standard itself, the libraries and its
interfaces and implementations are subject to change.

More information to follow ... stay tuned.