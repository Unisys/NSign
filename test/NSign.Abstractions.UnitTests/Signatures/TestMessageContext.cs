using Microsoft.Extensions.Logging;
using NSign.Http;
using System;
using System.Collections.Generic;
using System.Threading;

namespace NSign.Signatures
{
    internal sealed class TestMessageContext : MessageContext
    {
        public TestMessageContext(ILogger logger) : base(logger, new HttpFieldOptions())
        { }

        internal bool HasResponseValue { get; set; }

        internal CancellationToken AbortedValue { get; set; }

        internal Action<string, string>? OnAddHeader { get; set; }

        internal Func<DerivedComponent, string?>? OnGetDerivedComponentValue { get; set; }
        internal Func<string, IEnumerable<string>>? OnGetHeaderValues { get; set; }
        internal Func<string, IEnumerable<string>>? OnGetTrailerValues { get; set; }
        internal Func<string, IEnumerable<string>>? OnGetRequestHeaderValues { get; set; }
        internal Func<string, IEnumerable<string>>? OnGetRequestTrailerValues { get; set; }
        internal Func<string, IEnumerable<string>>? OnGetQueryParamValues { get; set; }

        public override bool HasResponse => HasResponseValue;

        public override CancellationToken Aborted => AbortedValue;

        public override void AddHeader(string headerName, string value)
        {
            OnAddHeader!(headerName, value);
        }

        public override string? GetDerivedComponentValue(DerivedComponent component)
        {
            return OnGetDerivedComponentValue!(component);
        }

        public override IEnumerable<string> GetHeaderValues(string headerName)
        {
            return OnGetHeaderValues!(headerName);
        }

        public override IEnumerable<string> GetTrailerValues(string fieldName)
        {
            return OnGetTrailerValues!(fieldName);
        }

        public override IEnumerable<string> GetQueryParamValues(string paramName)
        {
            return OnGetQueryParamValues!(paramName);
        }

        public override IEnumerable<string> GetRequestHeaderValues(string headerName)
        {
            return OnGetRequestHeaderValues!(headerName);
        }

        public override IEnumerable<string> GetRequestTrailerValues(string fieldName)
        {
            return OnGetRequestTrailerValues!(fieldName);
        }
    }
}
