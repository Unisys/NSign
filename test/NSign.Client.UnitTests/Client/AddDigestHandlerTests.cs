using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static NSign.Client.AddDigestOptions;

namespace NSign.Client
{
    public sealed class AddDigestHandlerTests
    {
        private readonly Mock<HttpMessageHandler> mockInnerHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        private readonly HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:8080/UnitTests/");
        private readonly HttpResponseMessage response = new HttpResponseMessage();
        private readonly AddDigestOptions options = new AddDigestOptions();
        private readonly AddDigestHandler handler;

        public AddDigestHandlerTests()
        {
            mockInnerHandler.Protected().Setup("Dispose", ItExpr.Is<bool>(d => d == true));

            options.WithHash(AddDigestOptions.Hash.Sha256).WithHash(AddDigestOptions.Hash.Sha512);

            handler = new AddDigestHandler(new OptionsWrapper<AddDigestOptions>(options))
            {
                InnerHandler = mockInnerHandler.Object,
            };
        }

        [Fact]
        public async Task SendAsyncDoesNotAddDigestIfRequestDoesNotHaveContent()
        {
            using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

            request.Method = HttpMethod.Get;

            mockInnerHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r == request && !r.Headers.Contains("digest")),
                ItExpr.Is<CancellationToken>(c => c == CancellationToken.None))
                .ReturnsAsync(response);

            Assert.Same(response, await invoker.SendAsync(request, default));

            mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
                "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Theory]
        [InlineData("stream", "hello world", Hash.Sha256, "SHA-256=uU0nuZNNPgilLlLX2n2r+sSE7+N6U4DukIj3rOLvzek=")]
        [InlineData("string", "hello", Hash.Sha256, "SHA-256=LPJNul+wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ=")]
        [InlineData("json", "hello", Hash.Sha256, "SHA-256=Wqdirjg/u3J688ejbUlApbjECpiUUtIwT8lY/z81Tno=")]
        [InlineData("stream", "hello world", Hash.Sha512, "SHA-512=MJ7MSJwS1utMxA9QyQLytNDtd+5RGnx6m808qG1M2G+YndNbxf9JlnDaNCVbRbDP2DDoH2Bdz33FVC6TrpzXbw==")]
        [InlineData("string", "hello", Hash.Sha512, "SHA-512=m3HSJL1i83hdltRq0+o9czGb+8KJDKra4t/3JRlnPKcjI8PZm6XBHXx6zG4UuMXaDEZjR1wuXDre9G9zvN7AQw==")]
        [InlineData("json", "hello", Hash.Sha512, "SHA-512=A8pplr4vsk4xdLkJruCXWp6+i+dy/3pSW5HW5ke1jDWS70Dv6Fstf1jS+XEcLqEVhW3i925IPlf/4tnpnvAQDw==")]
        public async Task SendAsyncAddsOnlyConfiguredHashes(string httpContentType, string content, Hash hash, string expectedValue)
        {
            using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

            request.Method = HttpMethod.Post;
            request.Content = CreateHttpContent(httpContentType, content);
            options.Hashes.Clear();
            options.WithHash(hash);

            mockInnerHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r == request && VerifyDigestHeader(r, expectedValue)),
                ItExpr.Is<CancellationToken>(c => c == CancellationToken.None))
                .ReturnsAsync(response);

            Assert.Same(response, await invoker.SendAsync(request, default));

            mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
                "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Theory]
        [InlineData("stream", "test", "SHA-256=n4bQgYhMfWWaL+qgxVrQFaO/TxsrC4Is0V1sFbDwCgg=", "SHA-512=7iaw3Ur350mqGo7jwQrpkj9hiYB3Lkc/iBml1JQODbJ6wYX4oOHV+E+IvIh/1nsUNzLDBMxfqa2Ob1f1ACio/w==")]
        [InlineData("string", "test", "SHA-256=n4bQgYhMfWWaL+qgxVrQFaO/TxsrC4Is0V1sFbDwCgg=", "SHA-512=7iaw3Ur350mqGo7jwQrpkj9hiYB3Lkc/iBml1JQODbJ6wYX4oOHV+E+IvIh/1nsUNzLDBMxfqa2Ob1f1ACio/w==")]
        [InlineData("json", "test", "SHA-256=TZZ6MBEb8p8OugHESLN1wWKbL+0BzfzDrtkfG1fV3V4=", "SHA-512=ceemix/T1umsPeT9f/DEUNpccmguTGuGcqp+SAhBhz9oTwjX49sBWRNNam2cvhm51qgMV+NsXMm/Fg6JsKjgJQ==")]
        public async Task SendAsyncAddsMultipleHashes(string httpContentType, string content, string expectedValue1, string expectedValue2)
        {
            using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

            request.Method = HttpMethod.Post;
            request.Content = CreateHttpContent(httpContentType, content);

            mockInnerHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r == request && VerifyDigestHeader(r, expectedValue1, expectedValue2)),
                ItExpr.Is<CancellationToken>(c => c == CancellationToken.None))
                .ReturnsAsync(response);

            Assert.Same(response, await invoker.SendAsync(request, default));

            mockInnerHandler.Protected().Verify<Task<HttpResponseMessage>>(
                "SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Theory]
        [InlineData(Hash.Unknown)]
        [InlineData((Hash)999)]
        public async Task SendAsyncThrowsOnUnsupportedHash(Hash hash)
        {
            using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

            request.Method = HttpMethod.Post;
            request.Content = CreateHttpContent("string", "blah");

            options.Hashes.Clear();
            options.WithHash(hash);

            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(() => invoker.SendAsync(request, default));
            Assert.Equal($"Hash algorithm '{hash}' is not supported.", ex.Message);
        }

        private static HttpContent CreateHttpContent(string type, string content)
        {
            switch (type)
            {
                case "stream":
                    return new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content)));

                case "string":
                    return new StringContent(content);

                case "json":
                    return JsonContent.Create(content);

                default:
                    throw new NotSupportedException();
            }
        }

        private static bool VerifyDigestHeader(HttpRequestMessage request, params string[] expectedValues)
        {
            if (!request.Content!.Headers.TryGetValues("digest", out IEnumerable<string>? values))
            {
                return false;
            }

            HashSet<string> actualValues = new HashSet<string>(values);
            if (actualValues.Count != expectedValues.Length)
            {
                return false;
            }

            foreach (string expectedValue in expectedValues)
            {
                if (!actualValues.Contains(expectedValue))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
