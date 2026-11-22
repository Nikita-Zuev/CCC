using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Lab08Variant1;

public sealed record ExternalUser(int Id, string Name, string Email);

public sealed class ExternalContactsClient : IDisposable
{
    private readonly HttpClient _client;

    public ExternalContactsClient(string baseUrl)
    {
        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public Task<ExternalUser?> GetUserAsync(int id, CancellationToken token) =>
        _client.GetFromJsonAsync<ExternalUser>($"users/{id}", token);

    public void Dispose() => _client.Dispose();
}

internal static class Program
{
    private static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("LAB 8. Variant 1 - Customer contacts\n");

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
            var baseUrl = configuration["ExternalApi:BaseUrl"]
                ?? throw new InvalidOperationException("ExternalApi:BaseUrl is not configured.");

            using var contactsClient = new ExternalContactsClient(baseUrl);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Console.WriteLine("Loading external customer contacts...");

            var stopwatch = Stopwatch.StartNew();
            var tasks = Enumerable.Range(1, 3)
                .Select(id => contactsClient.GetUserAsync(id, timeout.Token))
                .ToArray();
            var contacts = (await Task.WhenAll(tasks)).Where(x => x is not null).Cast<ExternalUser>().ToArray();
            stopwatch.Stop();

            Console.WriteLine($"Received: {contacts.Length}");
            Console.WriteLine("\nCustomer contacts:");
            foreach (var contact in contacts)
                Console.WriteLine($"{contact.Id} | {contact.Name} | {contact.Email}");
            Console.WriteLine($"\nConcurrent request time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine("No Customer or Order entities were created automatically.");

            await CompareRequestModesAsync(contactsClient, timeout.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Request cancelled or timed out.");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON error: {ex.Message}");
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException)
        {
            Console.WriteLine($"Configuration error: {ex.Message}");
        }
    }

    private static async Task CompareRequestModesAsync(ExternalContactsClient client, CancellationToken token)
    {
        var sequentialTimer = Stopwatch.StartNew();
        foreach (var id in Enumerable.Range(1, 3))
            await client.GetUserAsync(id, token);
        sequentialTimer.Stop();

        var concurrentTimer = Stopwatch.StartNew();
        await Task.WhenAll(Enumerable.Range(1, 3).Select(id => client.GetUserAsync(id, token)));
        concurrentTimer.Stop();

        Console.WriteLine($"Sequential requests: {sequentialTimer.ElapsedMilliseconds} ms");
        Console.WriteLine($"Task.WhenAll requests: {concurrentTimer.ElapsedMilliseconds} ms");
        Console.WriteLine("Measurements may differ because of network latency, server load and caching.");
    }
}
