using System.Net.Http.Json;

namespace Publisher;

internal sealed class WebhooksCaller
{
    private readonly HttpClient client;

    public WebhooksCaller(HttpClient client)
    {
        this.client = client;
    }

    public async Task PostDataAsync(string endpoint, string data)
    {
        using JsonContent body = JsonContent.Create(new { data = data });
        using HttpResponseMessage response = await client.PostAsync(endpoint, body);

        response.EnsureSuccessStatusCode();
    }
}
