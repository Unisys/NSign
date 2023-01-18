using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Publisher;

internal sealed class PublisherWorker : BackgroundService
{
    private readonly ILogger<PublisherWorker> logger;
    private readonly WebhooksCaller caller;
    private readonly IHostApplicationLifetime lifetime;

    public PublisherWorker(ILogger<PublisherWorker> logger, WebhooksCaller caller, IHostApplicationLifetime lifetime)
    {
        this.logger = logger;
        this.caller = caller;
        this.lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        string dataToSend = $"The current UTC time is: {DateTimeOffset.UtcNow:O}.";
        logger.LogInformation("Sending '{data}' ...", dataToSend);
        await caller.PostDataAsync("http://localhost:5219/webhooks/SignedRequest", dataToSend);
        await Task.Delay(TimeSpan.FromSeconds(1));
        lifetime.StopApplication();
    }
}
