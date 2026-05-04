using System.Net;

var endpoint = args.Length > 0 && Uri.TryCreate(args[0], UriKind.Absolute, out var parsedEndpoint)
    ? parsedEndpoint
    : new Uri("http://127.0.0.1:8080/api/v1/health");

var timeoutSeconds = 5;
if (args.Length > 1 && int.TryParse(args[1], out var parsedTimeout) && parsedTimeout > 0)
{
    timeoutSeconds = parsedTimeout;
}

using var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(timeoutSeconds)
};

using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
request.Headers.UserAgent.ParseAdd("TendexAI-ContainerHealthcheck/1.0");

try
{
    using var response = await httpClient.SendAsync(request);

    if (response.StatusCode == HttpStatusCode.OK)
    {
        return 0;
    }

    Console.Error.WriteLine($"Healthcheck failed with status code {(int)response.StatusCode} ({response.StatusCode}).");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Healthcheck request failed: {ex.Message}");
    return 1;
}
