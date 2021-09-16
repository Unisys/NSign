using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static NSign.AspNetCore.DigestVerificationOptions;

namespace NSign.AspNetCore
{
    public sealed class DigestVerificationMiddlewareTests
    {
        private readonly DefaultHttpContext httpContext = new DefaultHttpContext();
        private readonly DigestVerificationOptions options;
        private readonly DigestVerificationMiddleware middleware;
        private long numCallsToNext;

        public DigestVerificationMiddlewareTests()
        {
            options = new DigestVerificationOptions();

            middleware = new DigestVerificationMiddleware(
                new NullLogger<DigestVerificationMiddleware>(),
                new OptionsWrapper<DigestVerificationOptions>(options));
        }

        [Fact]
        public async Task MissingDigestHeaderCausesMissingHeaderResponseStatus()
        {
            options.MissingHeaderResponseStatus = 456;

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(456, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
        }

        [Theory]
        [InlineData(
            new string[]
            {
                "Sha-256=uU0nuZNNPgilLlLX2n2r+sSE7+N6U4DukIj3rOLvzek=, sha-512=MJ7MSJwS1utMxA9QyQLytNDtd+5RGnx6m808qG1M2G+YndNbxf9JlnDaNCVbRbDP2DDoH2Bdz33FVC6TrpzXbw==",
            },
            "hello world",
            VerificationBehavior.None)]
        [InlineData(
            new string[]
            {
                "blah=blotz=",
                "Sha-256=uU0nuZNNPgilLlLX2n2r+sSE7+N6U4DukIj3rOLvzek=",
                "sha-512=MJ7MSJwS1utMxA9QyQLytNDtd+5RGnx6m808qG1M2G+YndNbxf9JlnDaNCVbRbDP2DDoH2Bdz33FVC6TrpzXbw==",
            },
            "hello world",
            VerificationBehavior.IgnoreUnknownAlgorithms)]
        [InlineData(
            new string[]
            {
                "blah=blotz=",
                "Sha-256=LPJNul+wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ=", // Wrong hash
                "sha-512=MJ7MSJwS1utMxA9QyQLytNDtd+5RGnx6m808qG1M2G+YndNbxf9JlnDaNCVbRbDP2DDoH2Bdz33FVC6TrpzXbw==",
            },
            "hello world",
            VerificationBehavior.IgnoreUnknownAlgorithms | VerificationBehavior.RequireOnlySingleMatch)]
        [InlineData(
            new string[]
            {
                "Sha-256=uU0nuZNNPgilLlLX2n2r+sSE7+N6U4DukIj3rOLvzek=",
                "sha-512=m3HSJL1i83hdltRq0+o9czGb+8KJDKra4t/3JRlnPKcjI8PZm6XBHXx6zG4UuMXaDEZjR1wuXDre9G9zvN7AQw==", // Wrong hash
            },
            "hello world",
            VerificationBehavior.RequireOnlySingleMatch)]
        [InlineData(
            new string[]
            {
                "Sha-256=uU0nuZNNPgilLlLX2n2r+sSE7+N6U4DukIj3rOLvzek=",
                "sha-512=MJ7MSJwS1utMxA9QyQLytNDtd+5RGnx6m808qG1M2G+YndNbxf9JlnDaNCVbRbDP2DDoH2Bdz33FVC6TrpzXbw", // Invalid base64: missing padding
            },
            "hello world",
            VerificationBehavior.RequireOnlySingleMatch)]
        [InlineData(
            new string[]
            {
                "Sha-256=uU0nuZNNPgilLlLX2n2r+sSE7+N6U4DukIj3rOLvzek=",
                "sha-512=uU0nuZNNPgilLlLX2n2r+sSE7+N6U4DukIj3rOLvzek=", // Too little hash data
            },
            "hello world",
            VerificationBehavior.RequireOnlySingleMatch)]
        public async Task SuccessfulVerificationCauseNextMiddlewareToBeCalled(
            string[] headers,
            string body,
            VerificationBehavior behavior)
        {
            options.Behavior = behavior;

            using Stream bodyStream = MakeStream(body);
            httpContext.Request.Body = bodyStream;
            httpContext.Request.Headers.Add("Digest", headers);

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(200, httpContext.Response.StatusCode);
            Assert.Equal(1, Interlocked.Read(ref numCallsToNext));
        }

        [Theory]
        [InlineData("Sha-256=%&, sha-512=MJ7MSJwS1utMxA9QyQLytNDtd+5RGnx6m808qG1M2G+YndNbxf9JlnDaNCVbRbDP2DDoH2Bdz33FVC6TrpzXbw==")]
        [InlineData("SHA-256 = uU0nuZNNPgilLlLX2n2r+sSE7+N6U4DukIj3rOLvzek=, sha-512 = MJ7MSJwS1utMxA9QyQLytNDtd+5RGnx6m808qG1M2G+YndNbxf9JlnDaNCVbRbDP2DDoH2Bdz33FVC6TrpzXbw==")]
        public async Task MalformedDigestHeaderCausesVerificationFailuresResponseStatus(string headers)
        {
            options.VerificationFailuresResponseStatus = 555;

            httpContext.Request.Headers.Add("Digest", headers);

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(555, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
        }

        [Theory]
        [InlineData("MyHash=abc", VerificationBehavior.IgnoreUnknownAlgorithms)]
        [InlineData("MyHash=abc", VerificationBehavior.None)]
        public async Task OnlyUnknownAlgorithmsCausesVerificationFailuresResponseStatus(string headers, VerificationBehavior behavior)
        {
            options.VerificationFailuresResponseStatus = 999;
            options.Behavior = behavior;

            httpContext.Request.Headers.Add("Digest", headers);

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(999, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
        }

        //[InlineData(
        //    new string[]
        //    {
        //        "Sha-256=uU0nuZNNPgilLlLX2n2r+sSE7+N6U4DukIj3rOLvzek=",
        //        "sha-512=uU0nuZNNPgilLlLX2n2r+sSE7+N6U4DukIj3rOLvzek=",
        //    },
        //    "hello world",
        //    VerificationBehavior.RequireOnlySingleMatch)]
        //public async Task SuccessfulVerificationCauseNextMiddlewareToBeCalled(
        //    string[] headers,
        //    string body,
        //    VerificationBehavior behavior)
        //{
        //    options.Behavior = behavior;

        //    using Stream bodyStream = MakeStream(body);
        //    httpContext.Request.Body = bodyStream;
        //    httpContext.Request.Headers.Add("Digest", headers);

        //    await middleware.InvokeAsync(httpContext, CountingMiddleware);

        //    Assert.Equal(200, httpContext.Response.StatusCode);
        //    Assert.Equal(1, Interlocked.Read(ref numCallsToNext));
        //}

        private Task CountingMiddleware(HttpContext context)
        {
            Interlocked.Increment(ref numCallsToNext);
            return Task.CompletedTask;
        }

        private static Stream MakeStream(string data)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(data));
        }
    }
}
