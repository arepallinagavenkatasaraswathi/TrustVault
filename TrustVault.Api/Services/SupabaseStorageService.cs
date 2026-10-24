
using System.Net.Http.Headers;

namespace TrustVault.Api.Services;

public class SupabaseStorageService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SupabaseStorageService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    private string GetBaseUrl()
    {
        return (_configuration["Supabase:Url"]
            ?? throw new InvalidOperationException(
                "Supabase:Url is missing."))
            .TrimEnd('/');
    }

    private string GetServiceKey()
    {
        return _configuration["Supabase:ServiceRoleKey"]
            ?? throw new InvalidOperationException(
                "Supabase:ServiceRoleKey is missing.");
    }

    private string GetBucket()
    {
        return _configuration["Supabase:Bucket"]
            ?? "trustvault-files";
    }

    public async Task UploadAsync(
        string objectPath,
        Stream fileStream,
        string contentType)
    {
        var key = GetServiceKey();
        var url = $"{GetBaseUrl()}/storage/v1/object/" +
                  $"{GetBucket()}/{objectPath}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("apikey", key);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", key);
        request.Headers.Add("x-upsert", "true");

        var content = new StreamContent(fileStream);
        content.Headers.ContentType =
            new MediaTypeHeaderValue(contentType);

        request.Content = content;

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<byte[]> DownloadAsync(string objectPath)
    {
        var key = GetServiceKey();
        var url = $"{GetBaseUrl()}/storage/v1/object/" +
                  $"{GetBucket()}/{objectPath}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("apikey", key);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", key);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync();
    }
}
