using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Codexus.OpenSDK.Http;

public class HttpWrapper : IDisposable
{
    private readonly string _baseUrl;
    private readonly HttpRequestOptions _defaultOptions;

    public HttpWrapper(
        string baseUrl = "",
        Action<HttpRequestOptions>? configureDefaults = null,
        HttpClientHandler? handler = null)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        Client = new HttpClient(handler ?? new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All
        })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        _defaultOptions = new HttpRequestOptions();
        configureDefaults?.Invoke(_defaultOptions);

        ApplyDefaultOptions();
    }

    public HttpClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<string> GetStringAsync(
        string url,
        Action<HttpRequestOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var response = await GetAsync(url, configure, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }


    public async Task<T?> GetAsync<T>(
        string url,
        Action<HttpRequestOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var json = await GetStringAsync(url, configure, cancellationToken);
        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task<HttpResponseMessage> GetAsync(
        string url,
        Action<HttpRequestOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Get, url, configure);
        return await Client.SendAsync(request, cancellationToken);
    }


    public async Task<HttpResponseMessage> PostJsonAsync<T>(
        string url,
        T data,
        Action<HttpRequestOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(data);
        return await PostAsync(url, json, "application/json", configure, cancellationToken);
    }

    public async Task<TResponse?> PostJsonAsync<TRequest, TResponse>(
        string url,
        TRequest data,
        Action<HttpRequestOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var response = await PostJsonAsync(url, data, configure, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(json);
    }

    public async Task<HttpResponseMessage> PostFormAsync(
        string url,
        Dictionary<string, string> formData,
        Action<HttpRequestOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Post, url, configure);
        request.Content = new FormUrlEncodedContent(formData);
        return await Client.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync(
        string url,
        string content,
        string contentType = "application/json",
        Action<HttpRequestOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Post, url, configure);
        request.Content = new StringContent(content, Encoding.UTF8, contentType);
        return await Client.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync(
        string url,
        byte[] content,
        string contentType = "application/octet-stream",
        Action<HttpRequestOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Post, url, configure);
        request.Content = new ByteArrayContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return await Client.SendAsync(request, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string url,
        Action<HttpRequestOptions>? configure)
    {
        var options = _defaultOptions.Clone();
        configure?.Invoke(options);

        var fullUrl = BuildUrl(url, options.QueryParameters);
        var request = new HttpRequestMessage(method, fullUrl);

        if (options.HttpVersion != null) request.Version = options.HttpVersion;

        foreach (var header in options.Headers) request.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return request;
    }

    private string BuildUrl(string url, Dictionary<string, string> queryParams)
    {
        var baseUrl = string.IsNullOrEmpty(_baseUrl) ? url : $"{_baseUrl}/{url.TrimStart('/')}";

        if (queryParams.Count == 0)
            return baseUrl;

        var query = string.Join("&", queryParams.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        var separator = baseUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}{separator}{query}";
    }

    private void ApplyDefaultOptions()
    {
        foreach (var header in _defaultOptions.Headers)
            Client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
    }
}