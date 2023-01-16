using System;
using Xunit;

namespace NSign.Signatures
{
    partial class MessageContextTests
    {
        [Fact]
        public void RetrievingResponseSignatureThrowsIfNotHasResponse()
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => context.GetResponseSignature("blah"));
            Assert.Equal("Cannot get response signatures if no response is available.", ex.Message);
        }

        [Fact]
        public void SignatureParsingErrorDoesNotAffectOtherSignatures()
        {
            context.OnGetRequestHeaderValues = (headerName) =>
            {
                return headerName switch
                {
                    "signature" => new string[] { "bad=abcd, good=:abcd:, morebad=()", "other=blah", "#", },
                    "signature-input" => new string[] { "bad=(), good=(), morebad=\"\"", "other=blah", "#", },
                    _ => Array.Empty<string>(),
                };
            };

            SignatureContext? sig = context.GetRequestSignature("good");
            Assert.True(sig.HasValue);
            Assert.Equal("abcd", Convert.ToBase64String(sig!.Value.Signature.Span));
            Assert.NotNull(sig.Value.InputSpec);

            Assert.False(context.GetRequestSignature("bad").HasValue);
            Assert.False(context.GetRequestSignature("morebad").HasValue);
            Assert.False(context.GetRequestSignature("other").HasValue);
        }

        [Fact]
        public void SignatureInputParsingErrorDoesNotAffectOtherSignatures()
        {
            context.OnGetRequestHeaderValues = (headerName) =>
            {
                return headerName switch
                {
                    "signature" => new string[] { " good=:abcd:", },
                    "signature-input" => new string[] {
                        "#",
                        "bad=abcd, good=(\"@query-param\";name=\"test\" \"x-header\");nonce=\"blah\", morebad=123",
                        "other=blah",
                    },
                    _ => Array.Empty<string>(),
                };
            };

            SignatureContext? sig = context.GetRequestSignature("good");
            Assert.True(sig.HasValue);
            Assert.Equal("abcd", Convert.ToBase64String(sig!.Value.Signature.Span));
            Assert.NotNull(sig.Value.InputSpec);
            Assert.Equal("(\"@query-param\";name=\"test\" \"x-header\");nonce=\"blah\"", sig.Value.InputSpec);
        }
    }
}
