﻿using Microsoft.Extensions.Logging;
using Moq;
using System;
using Xunit;

namespace NSign.Signatures
{
    public sealed partial class MessageContextTests
    {
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>(MockBehavior.Loose);
        private readonly TestMessageContext context;

        public MessageContextTests()
        {
            context = new TestMessageContext(mockLogger.Object);
        }

        [Fact]
        public void LoggerIsPassed()
        {
            Assert.Same(mockLogger.Object, context.Logger);
        }

        [Fact]
        public void SigningOptionsDefaultsToNull()
        {
            Assert.Null(context.SigningOptions);
        }

        [Fact]
        public void VerificationOptionsDefaultsToNull()
        {
            Assert.Null(context.VerificationOptions);
        }

        [Theory]
        [InlineData("sig1", "blah", null)]
        [InlineData("test", "qwer", "(\"@query-param\";name=\"test\");key=\"test\"")]
        [InlineData("inexistent", null, null)]
        public void GetRequestSignatureWorks(string sigName, string? expectedSignature, string? expectedInput)
        {
            context.OnGetRequestHeaderValues = (header) =>
            {
                return header.ToLowerInvariant() switch
                {
                    "signature" => new string[] { "sig1=:blah:, sig2=:asdf:,test=:qwer:" },
                    "signature-input" => new string[] { "test=(\"@query-param\";name=\"test\");key=\"test\"" },
                    _ => Array.Empty<string>(),
                };
            };

            SignatureContext? sig = context.GetRequestSignature(sigName);

            if (null != expectedSignature)
            {
                Assert.True(sig.HasValue);
                Assert.Equal(expectedSignature, Convert.ToBase64String(sig!.Value.Signature.Span));

                if (null != expectedInput)
                {
                    Assert.NotNull(sig.Value.InputSpec);
                    Assert.Equal(expectedInput, sig.Value.InputSpec);
                }
                else
                {
                    Assert.Null(sig.Value.InputSpec);
                }
            }
            else
            {
                Assert.False(sig.HasValue);
            }
        }

        [Theory]
        [InlineData("sig1", "blah", null)]
        [InlineData("test", "qwer", "(\"x-header\";key=\"test\");alg=\"test\"")]
        [InlineData("inexistent", null, null)]
        public void GetResponseSignatureWorksWhenHasResponse(string sigName, string? expectedSignature, string? expectedInput)
        {
            context.HasResponseValue = true;
            context.OnGetHeaderValues = (header) =>
            {
                return header.ToLowerInvariant() switch
                {
                    "signature" => new string[] { "sig1=:blah:, sig2=:asdf:,test=:qwer:" },
                    "signature-input" => new string[] { "test=(\"x-header\";key=\"test\");alg=\"test\"" },
                    _ => Array.Empty<string>(),
                };
            };

            SignatureContext? sig = context.GetResponseSignature(sigName);

            if (null != expectedSignature)
            {
                Assert.True(sig.HasValue);
                Assert.Equal(expectedSignature, Convert.ToBase64String(sig!.Value.Signature.Span));

                if (null != expectedInput)
                {
                    Assert.NotNull(sig.Value.InputSpec);
                    Assert.Equal(expectedInput, sig.Value.InputSpec);
                }
                else
                {
                    Assert.Null(sig.Value.InputSpec);
                }
            }
            else
            {
                Assert.False(sig.HasValue);
            }
        }

        [Fact]
        public void GetResponseSignatureThrowsWhenNotHasResponse()
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => context.GetResponseSignature("any"));
            Assert.Equal("Cannot get response signatures if no response is available.", ex.Message);
        }

        [Fact]
        public void SignaturesForVerificationUsesRequestSignaturesWhenNotHasResponse()
        {
            context.HasResponseValue = false;
            context.OnGetRequestHeaderValues = (header) =>
            {
                return header.ToLowerInvariant() switch
                {
                    "signature" => new string[] { "test=:qwer:" },
                    _ => Array.Empty<string>(),
                };
            };

            Assert.Collection(context.SignaturesForVerification,
                (sig) =>
                {
                    Assert.Equal("test", sig.Name);
                    Assert.Equal("qwer", Convert.ToBase64String(sig.Signature.Span));
                });
        }

        [Fact]
        public void SignaturesForVerificationUsesMessageSignaturesWhenHasResponse()
        {
            context.HasResponseValue = true;
            context.OnGetHeaderValues = (header) =>
            {
                return header.ToLowerInvariant() switch
                {
                    "signature" => new string[] { "test=:qwer:" },
                    _ => Array.Empty<string>(),
                };
            };

            Assert.Collection(context.SignaturesForVerification,
                (sig) =>
                {
                    Assert.Equal("test", sig.Name);
                    Assert.Equal("qwer", Convert.ToBase64String(sig.Signature.Span));
                });
        }

        [Fact]
        public void HasSignaturesForVerificationUsesRequestSignaturesWhenNotHasResponse()
        {
            context.HasResponseValue = false;
            context.OnGetRequestHeaderValues = (header) =>
            {
                return header.ToLowerInvariant() switch
                {
                    "signature" => new string[] { "test=:qwer:" },
                    _ => Array.Empty<string>(),
                };
            };

            Assert.True(context.HasSignaturesForVerification);
        }

        [Fact]
        public void HasSignaturesForVerificationUsesMessageSignaturesWhenHasResponse()
        {
            context.HasResponseValue = true;
            context.OnGetHeaderValues = (header) =>
            {
                return header.ToLowerInvariant() switch
                {
                    "signature" => new string[] { "test=:qwer:" },
                    _ => Array.Empty<string>(),
                };
            };

            Assert.True(context.HasSignaturesForVerification);
        }

        [Theory]
        [InlineData(false, "x-first", true)]
        [InlineData(false, "x-second", true)]
        [InlineData(false, "x-third", true)]
        [InlineData(false, "x-fourth", false)]
        [InlineData(true, "y-first", true)]
        [InlineData(true, "y-second", true)]
        [InlineData(true, "y-third", true)]
        [InlineData(true, "y-fourth", false)]
        public void HasHeaderWorks(bool bindRequest, string name, bool exists)
        {
            context.OnGetHeaderValues = (headerName) =>
            {
                return headerName.ToLowerInvariant() switch
                {
                    "x-first" => new string[] { "a", "b", "c", },
                    "x-second" => new string[] { "x", },
                    "x-third" => new string[] { "", },
                    _ => Array.Empty<string>(),
                };
            };
            context.OnGetRequestHeaderValues = (headerName) =>
            {
                return headerName.ToLowerInvariant() switch
                {
                    "y-first" => new string[] { "a", "b", "c", },
                    "y-second" => new string[] { "x", },
                    "y-third" => new string[] { "", },
                    _ => Array.Empty<string>(),
                };
            };

            Assert.Equal(exists, context.HasHeader(bindRequest, name));
        }

        [Theory]
        [InlineData("@method", false, true)]
        [InlineData("@method", true, true)]
        [InlineData("@target-uri", false, true)]
        [InlineData("@target-uri", true, true)]
        [InlineData("@authority", false, true)]
        [InlineData("@authority", true, true)]
        [InlineData("@scheme", false, true)]
        [InlineData("@scheme", true, true)]
        [InlineData("@request-target", false, true)]
        [InlineData("@request-target", true, true)]
        [InlineData("@path", false, true)]
        [InlineData("@path", true, true)]
        [InlineData("@query", false, true)]
        [InlineData("@query", true, true)]
        [InlineData("@status", false, false)]
        [InlineData("@status", true, true)]
        public void HasDerivedComponentWorksForSimpleDerivedComponents(string name, bool hasResponse, bool exists)
        {
            context.HasResponseValue = hasResponse;
            context.OnGetQueryParamValues = (paramName) =>
            {
                return paramName switch
                {
                    "first" => new string[] { "value1", "value2", },
                    "second" => new string[] { "value", },
                    "third" => new string[] { "" },
                    _ => Array.Empty<string>(),
                };
            };
            DerivedComponent comp = new DerivedComponent(name);

            Assert.Equal(exists, context.HasDerivedComponent(comp));
        }

        [Theory]
        [InlineData("first", true)]
        [InlineData("second", true)]
        [InlineData("third", true)]
        [InlineData("fourth", false)]
        public void HasDerivedComponentWorksForQueryParams(string name, bool exists)
        {
            context.OnGetQueryParamValues = (paramName) =>
            {
                return paramName switch
                {
                    "first" => new string[] { "value1", "value2", },
                    "second" => new string[] { "value", },
                    "third" => new string[] { "" },
                    _ => Array.Empty<string>(),
                };
            };
            QueryParamComponent comp = new QueryParamComponent(name);

            Assert.Equal(exists, context.HasDerivedComponent(comp));
        }

        [Theory]
        [InlineData("blah", "Cannot use BindRequest for request message signature components.")]
        [InlineData("@authority", "Cannot use BindRequest for request message signature components.")]
        [InlineData("@status", "Cannot use BindRequest for request message signature components.")]
        public void EnsureComponentIsAllowedThrowsSignatureComponentNotAllowedExceptionWhenRequestBindingRequiredButNotPossible(
            string componentName,
            string expectedExceptionMessage
            )
        {
            SignatureComponent comp = componentName[0] == '@' ?
                new DerivedComponent(componentName, bindRequest: true) :
                new HttpHeaderComponent(componentName, bindRequest: true);

            context.HasResponseValue = false;

            SignatureComponentNotAllowedException ex = Assert.Throws<SignatureComponentNotAllowedException>(
                () => context.EnsureComponentIsAllowed(comp));

            Assert.Equal(expectedExceptionMessage, ex.Message);
            Assert.Same(comp, ex.Component);
        }

        [Theory]
        [InlineData("@status", "Cannot use '@status' with request-response binding.")]
        public void EnsureComponentIsAllowedThrowsSignatureComponentNotAllowedExceptionWhenRequestBindingNotAllowed(
            string componentName,
            string expectedExceptionMessage
            )
        {
            SignatureComponent comp = componentName[0] == '@' ?
                new DerivedComponent(componentName, bindRequest: true) :
                new HttpHeaderComponent(componentName, bindRequest: true);

            context.HasResponseValue = true;

            SignatureComponentNotAllowedException ex = Assert.Throws<SignatureComponentNotAllowedException>(
                () => context.EnsureComponentIsAllowed(comp));

            Assert.Equal(expectedExceptionMessage, ex.Message);
            Assert.Same(comp, ex.Component);
        }
    }
}
