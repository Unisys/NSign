using Microsoft.AspNetCore.Mvc;

namespace Subscriber.Controllers;

[ApiController]
[Route("webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly ILogger<WebhooksController> logger;

    public WebhooksController(ILogger<WebhooksController> logger)
    {
        this.logger = logger;
    }

    [HttpPost("SignedRequest")]
    public IActionResult ReceiveEvent(Event evt)
    {
        logger.LogInformation("Received signed data: [{data}].", evt.Data);
        logger.LogInformation("Signature Header: {sigs}", Request.Headers[NSign.Constants.Headers.Signature]);
        logger.LogInformation("Signature-Input Header: {sigInputs}", Request.Headers[NSign.Constants.Headers.SignatureInput]);

        return Ok();
    }

    public readonly struct Event
    {
        public string? Data { get; init; }
    }
}
