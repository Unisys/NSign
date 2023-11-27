using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using System.Text;

namespace RuntimeKeySelection.Controllers;

[ApiController]
[Route("api/test")]
[Authorize]
public sealed class TestController : ControllerBase
{
    private readonly HttpClient client;
    private readonly OutboundCallOptions options;

    public TestController(IHttpClientFactory clientFactory, IOptions<OutboundCallOptions> options)
    {
        client = clientFactory.CreateClient("SignedMessageSender");
        this.options = options.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Test()
    {
        await client.PostAsync(options.TargetEndpoint,
            new StringContent("Hello World", Encoding.UTF8, MediaTypeNames.Text.Plain));

        return Ok(new { now = DateTimeOffset.Now, user = User?.Identity?.Name });
    }
}
