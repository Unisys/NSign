using System;
using System.Collections.Generic;

namespace NSign.Http
{
    /// <summary>
    /// Defines options for HTTP fields for both signing and verification of signatures.
    /// </summary>
    public class HttpFieldOptions
    {
        /// <summary>
        /// Gets a <see cref="IDictionary{TKey, TValue}"/> mapping string values to <see cref="StructuredFieldType"/>
        /// values defining the structured field type for known headers.
        /// </summary>
        /// <remarks>
        /// By default this is initialized to the list from chapter 2 of
        /// <see href="https://datatracker.ietf.org/doc/draft-ietf-httpbis-retrofit/"/>.
        /// </remarks>
        public IDictionary<string, StructuredFieldType> StructuredFieldsMap { get; } =
            new Dictionary<string, StructuredFieldType>(StringComparer.OrdinalIgnoreCase)
            {
                { "Accept", StructuredFieldType.List },
                { "Accept-Encoding", StructuredFieldType.List },
                { "Accept-Language", StructuredFieldType.List },
                { "Accept-Patch", StructuredFieldType.List },
                { "Accept-Post", StructuredFieldType.List },
                { "Accept-Ranges", StructuredFieldType.List },
                { "Access-Control-Allow-Credentials", StructuredFieldType.Item },
                { "Access-Control-Allow-Headers", StructuredFieldType.List },
                { "Access-Control-Allow-Methods", StructuredFieldType.List },
                { "Access-Control-Allow-Origin", StructuredFieldType.Item },
                { "Access-Control-Expose-Headers", StructuredFieldType.List },
                { "Access-Control-Max-Age", StructuredFieldType.Item },
                { "Access-Control-Request-Headers", StructuredFieldType.List },
                { "Access-Control-Request-Method", StructuredFieldType.Item },
                { "Age", StructuredFieldType.Item },
                { "Allow", StructuredFieldType.List },
                { "ALPN", StructuredFieldType.List },
                { "Alt-Svc", StructuredFieldType.Dictionary },
                { "Alt-Used", StructuredFieldType.Item },
                { "Cache-Control", StructuredFieldType.Dictionary },
                { "CDN-Loop", StructuredFieldType.List },
                { "Clear-Site-Data", StructuredFieldType.List },
                { "Connection", StructuredFieldType.List },
                { "Content-Encoding", StructuredFieldType.List },
                { "Content-Language", StructuredFieldType.List },
                { "Content-Length", StructuredFieldType.List },
                { "Content-Type", StructuredFieldType.Item },
                { "Cross-Origin-Resource-Policy", StructuredFieldType.Item },
                { "DNT", StructuredFieldType.Item },
                { "Expect", StructuredFieldType.Dictionary },
                { "Expect-CT", StructuredFieldType.Dictionary },
                { "Forwarded", StructuredFieldType.Dictionary },
                { "Host", StructuredFieldType.Item },
                { "Keep-Alive", StructuredFieldType.Dictionary },
                { "Max-Forwards", StructuredFieldType.Item },
                { "Origin", StructuredFieldType.Item },
                { "Pragma", StructuredFieldType.Dictionary },
                { "Prefer", StructuredFieldType.Dictionary },
                { "Preference-Applied", StructuredFieldType.Dictionary },
                { "Retry-After", StructuredFieldType.Item },
                { "Sec-WebSocket-Extensions", StructuredFieldType.List },
                { "Sec-WebSocket-Protocol", StructuredFieldType.List },
                { "Sec-WebSocket-Version", StructuredFieldType.Item },
                { "Server-Timing", StructuredFieldType.List },
                { "Surrogate-Control", StructuredFieldType.Dictionary },
                { "TE", StructuredFieldType.List },
                { "Timing-Allow-Origin", StructuredFieldType.List },
                { "Trailer", StructuredFieldType.List },
                { "Transfer-Encoding", StructuredFieldType.List },
                { "Upgrade-Insecure-Requests", StructuredFieldType.Item },
                { "Vary", StructuredFieldType.List },
                { "X-Content-Type-Options", StructuredFieldType.Item },
                { "X-Frame-Options", StructuredFieldType.Item },
                { "X-XSS-Protection", StructuredFieldType.List },
            };
    }
}
