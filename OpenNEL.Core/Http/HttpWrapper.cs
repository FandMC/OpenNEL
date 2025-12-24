using System.Net.Http;
using System.Text;

namespace OpenNEL.Core.Http;

public class HttpWrapper : IDisposable
{
    public class HttpWrapperBuilder
    {
        private readonly Dictionary<string, string> _headers = new();

        public string Domain { get; }
        public string Url { get; }
        public string Body { get; }

        public HttpWrapperBuilder(string domain, string url, string body)
        {
            Domain = domain;
            Url = url;
            Body = body;
        }

        public HttpWrapperBuilder AddHeader(Dictionary<string, string> headers)
        {
            foreach (var header in headers)
            {
                _headers.Add(header.Key, header.Value);
            }
            return this;
        }

        public HttpWrapperBuilder AddHeader(string key, string value)
        {
            _headers.Add(key, value);
            return this;
        }

        public HttpWrapperBuilder UserAgent(string userAgent)
        {
            _headers["User-Agent"] = userAgent;
            return this;
        }

        public Dictionary<string, string> GetHeaders() => _headers;
    }

    private readonly HttpClient _httpClient;
    private readonly string _domain;
    private readonly Action<HttpWrapperBuilder>? _extension;
    private readonly Version? _version;

    public HttpWrapper(string domain = "", Action<HttpWrapperBuilder>? extension = null, HttpClientHandler? handler = null, Version? version = null)
    {
        _domain = domain;
        _extension = extension;
        _version = version;
        _httpClient = new HttpClient(handler ?? new HttpClientHandler());
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    public HttpClient GetClient() => _httpClient;

    public async Task<HttpResponseMessage> PostAsync(string url, string body, Action<HttpWrapperBuilder> block)
    {
        return await PostAsync(url, body, "application/json", block);
    }

    public async Task<HttpResponseMessage> PostAsync(string url, string body, string contentType = "application/json", Action<HttpWrapperBuilder>? block = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _domain + url);
        if (_version != null)
        {
            request.Version = _version;
        }
        request.Content = new StringContent(body, Encoding.UTF8, contentType);
        if (block == null)
        {
            return await _httpClient.SendAsync(request);
        }
        var builder = new HttpWrapperBuilder(_domain, url, body);
        _extension?.Invoke(builder);
        block(builder);
        foreach (var header in builder.GetHeaders())
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> PostAsync(string url, byte[] body, Action<HttpWrapperBuilder>? block = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _domain + url);
        if (_version != null)
        {
            request.Version = _version;
        }
        request.Content = new ByteArrayContent(body);
        if (block == null)
        {
            return await _httpClient.SendAsync(request);
        }
        var builder = new HttpWrapperBuilder(_domain, url, Encoding.UTF8.GetString(body));
        _extension?.Invoke(builder);
        block(builder);
        foreach (var header in builder.GetHeaders())
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetAsync(string url, Action<HttpWrapperBuilder>? block = null)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(_domain + url)
        };
        if (_version != null)
        {
            request.Version = _version;
        }
        if (block == null)
        {
            return await _httpClient.SendAsync(request);
        }
        var builder = new HttpWrapperBuilder(_domain, url, "");
        _extension?.Invoke(builder);
        block(builder);
        foreach (var header in builder.GetHeaders())
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return await _httpClient.SendAsync(request);
    }
}
