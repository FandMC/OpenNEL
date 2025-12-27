namespace Codexus.OpenSDK.Http;

public class HttpRequestOptions
{
    public Dictionary<string, string> Headers { get; } = new();
    public Dictionary<string, string> QueryParameters { get; } = new();
    public Version? HttpVersion { get; set; }

    public HttpRequestOptions AddHeader(string key, string value)
    {
        Headers[key] = value;
        return this;
    }

    public HttpRequestOptions AddHeaders(Dictionary<string, string> headers)
    {
        foreach (var header in headers) Headers[header.Key] = header.Value;

        return this;
    }

    public HttpRequestOptions WithUserAgent(string userAgent)
    {
        return AddHeader("User-Agent", userAgent);
    }

    public HttpRequestOptions WithBearerToken(string token)
    {
        return AddHeader("Authorization", $"Bearer {token}");
    }

    public HttpRequestOptions AddQuery(string key, string value)
    {
        QueryParameters[key] = value;
        return this;
    }

    public HttpRequestOptions AddQueries(Dictionary<string, string> queries)
    {
        foreach (var query in queries) QueryParameters[query.Key] = query.Value;

        return this;
    }

    public HttpRequestOptions WithVersion(Version version)
    {
        HttpVersion = version;
        return this;
    }

    internal HttpRequestOptions Clone()
    {
        var clone = new HttpRequestOptions
        {
            HttpVersion = HttpVersion
        };

        foreach (var header in Headers) clone.Headers[header.Key] = header.Value;

        foreach (var query in QueryParameters) clone.QueryParameters[query.Key] = query.Value;

        return clone;
    }
}