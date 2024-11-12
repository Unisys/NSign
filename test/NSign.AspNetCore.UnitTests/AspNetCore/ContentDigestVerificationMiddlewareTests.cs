using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static NSign.AspNetCore.ContentDigestVerificationOptions;

namespace NSign.AspNetCore
{
    public sealed class ContentDigestVerificationMiddlewareTests
    {
        private readonly DefaultHttpContext httpContext = new DefaultHttpContext();
        private readonly ContentDigestVerificationOptions options;
        private readonly ContentDigestVerificationMiddleware middleware;
        private long numCallsToNext;

        public ContentDigestVerificationMiddlewareTests()
        {
            options = new ContentDigestVerificationOptions();

            middleware = new ContentDigestVerificationMiddleware(
                new NullLogger<ContentDigestVerificationMiddleware>(),
                new OptionsWrapper<ContentDigestVerificationOptions>(options));
        }

        [Fact]
        public async Task MissingDigestHeaderCausesMissingHeaderResponseStatus()
        {
            options.MissingHeaderResponseStatus = 456;

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(456, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
        }

        [Fact]
        public async Task MissingDigestHeaderDoesNotCauseMissingHeaderResponseStatusWhenDigestIsOptional()
        {
            options.MissingHeaderResponseStatus = 456;
            options.Behavior |= VerificationBehavior.Optional;

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(200, httpContext.Response.StatusCode);
            Assert.Equal(1, Interlocked.Read(ref numCallsToNext));
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
            httpContext.Request.Headers["Content-Digest"] = headers;

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

            httpContext.Request.Headers["Content-Digest"] = headers;

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

            httpContext.Request.Headers["Content-Digest"] = headers;

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(999, httpContext.Response.StatusCode);
            Assert.Equal(0, Interlocked.Read(ref numCallsToNext));
        }

        [Theory]
        [InlineData(
            @"{""hello"": ""world""}",
            // Formatted per RFC 3230
            "sha-512=WZDPaVn/7XgHaAy8pmojAkGWoRx2UFChF41A2svX+TaPm+AbwAgBWnrIiYllu7BNNyealdVLvRwEmTHWXvJwew=="
        )]
        [InlineData(
            @"{""hello"": ""world""}",
            // Formatted per RFC 9530
            "sha-512=:WZDPaVn/7XgHaAy8pmojAkGWoRx2UFChF41A2svX+TaPm+AbwAgBWnrIiYllu7BNNyealdVLvRwEmTHWXvJwew==:"
        )]
        [InlineData(
            @"{""busy"": true, ""message"": ""Your call is very important to us""}",
            // Formatted per RFC 3230
            "sha-512=0Y6iCBzGg5rZtoXS95Ijz03mslf6KAMCloESHObfwnHJDbkkWWQz6PhhU9kxsTbARtY2PTBOzq24uJFpHsMuAg=="
        )]
        [InlineData(
            @"{""busy"": true, ""message"": ""Your call is very important to us""}",
            // Formatted per RFC 9530
            "sha-512=:0Y6iCBzGg5rZtoXS95Ijz03mslf6KAMCloESHObfwnHJDbkkWWQz6PhhU9kxsTbARtY2PTBOzq24uJFpHsMuAg==:"
        )]
        [InlineData(
            @"hello world!",
            // Formatted per RFC 3230
            "sha-256=dQnlvaDHYtK6x/kNdYtbImP6Acy8VCq1498WO+CObKk=," +
            "sha-512=25sc0yYt7jd1agm5BklzWJhHyqjlPTGp0ULqJwGxsoq9l4OLuaJwaLowXcjQSkWh/PB53lTWB2ZplrPMVPa2fA=="
        )]
        [InlineData(
            @"hello world!",
            // Formatted per RFC 9530
            "sha-256=:dQnlvaDHYtK6x/kNdYtbImP6Acy8VCq1498WO+CObKk=:," +
            "sha-512=:25sc0yYt7jd1agm5BklzWJhHyqjlPTGp0ULqJwGxsoq9l4OLuaJwaLowXcjQSkWh/PB53lTWB2ZplrPMVPa2fA==:"
        )]
        public async Task TestRfc3230AndRfc9530(string body, string headers)
        {
            options.VerificationFailuresResponseStatus = 999;
            options.Behavior = VerificationBehavior.None;

            using Stream bodyStream = MakeStream(body);
            httpContext.Request.Body = bodyStream;
            httpContext.Request.Headers["Content-Digest"] = headers;

            await middleware.InvokeAsync(httpContext, CountingMiddleware);

            Assert.Equal(200, httpContext.Response.StatusCode);
            Assert.Equal(1, Interlocked.Read(ref numCallsToNext));
        }

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
