using System.Net.Http;

namespace NSign.Client
{
    public sealed partial class HttpRequestMessageExtensionsTests
    {
        private readonly HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:8080/UnitTests");
    }
}
