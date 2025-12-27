namespace Codexus.OpenSDK.Http;

public class QueryBuilder
{
    private readonly Dictionary<string, string> _parameters = new();
    private string? _baseUrl;

    public QueryBuilder()
    {
    }

    public QueryBuilder(string urlOrQuery)
    {
        if (IsQueryStringOnly(urlOrQuery))
            ParseQueryString(urlOrQuery.TrimStart('?'));
        else
            ParseUrl(urlOrQuery);
    }

    public int Count => _parameters.Count;

    public static QueryBuilder FromDictionary(Dictionary<string, string> parameters)
    {
        var builder = new QueryBuilder();
        foreach (var param in parameters) builder._parameters[param.Key] = param.Value;

        return builder;
    }

    public static QueryBuilder FromObject(object obj)
    {
        var builder = new QueryBuilder();
        var properties = obj.GetType().GetProperties();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj);
            if (value != null) builder.Add(prop.Name, value.ToString()!);
        }

        return builder;
    }

    public QueryBuilder Add(string key, string value)
    {
        _parameters[key] = value;
        return this;
    }

    public QueryBuilder Add<T>(string key, T value)
    {
        if (value != null) _parameters[key] = value.ToString()!;

        return this;
    }

    public QueryBuilder AddIf(bool condition, string key, string value)
    {
        if (condition) Add(key, value);

        return this;
    }

    public QueryBuilder AddIf(bool condition, string key, Func<string> valueFactory)
    {
        if (condition) Add(key, valueFactory());

        return this;
    }

    public QueryBuilder AddIfNotEmpty(string key, string? value)
    {
        if (!string.IsNullOrEmpty(value)) Add(key, value);

        return this;
    }

    public QueryBuilder AddRange(Dictionary<string, string> parameters)
    {
        foreach (var param in parameters) _parameters[param.Key] = param.Value;

        return this;
    }

    public QueryBuilder Merge(QueryBuilder other)
    {
        return AddRange(other._parameters);
    }

    public QueryBuilder Remove(string key)
    {
        _parameters.Remove(key);
        return this;
    }

    public QueryBuilder RemoveRange(params string[] keys)
    {
        foreach (var key in keys) _parameters.Remove(key);

        return this;
    }

    public QueryBuilder Clear()
    {
        _parameters.Clear();
        return this;
    }


    public string Get(string key)
    {
        return _parameters.GetValueOrDefault(key) ?? throw new Exception("Parameter not found");
    }

    public string GetOrDefault(string key, string defaultValue)
    {
        return _parameters.GetValueOrDefault(key, defaultValue);
    }

    public bool Contains(string key)
    {
        return _parameters.ContainsKey(key);
    }

    public Dictionary<string, string> GetAll()
    {
        return new Dictionary<string, string>(_parameters);
    }


    public QueryBuilder WithBaseUrl(string url)
    {
        _baseUrl = url.Split('?')[0];
        return this;
    }


    public string BuildQueryString()
    {
        if (_parameters.Count == 0)
            return string.Empty;

        var pairs = _parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}");

        return string.Join("&", pairs);
    }


    public string BuildUrl()
    {
        if (string.IsNullOrEmpty(_baseUrl))
            throw new InvalidOperationException(
                "Base URL is not set. Call WithBaseUrl() first or use BuildQueryString()."
            );

        var queryString = BuildQueryString();
        if (string.IsNullOrEmpty(queryString))
            return _baseUrl;

        var separator = _baseUrl.Contains('?') ? "&" : "?";
        return $"{_baseUrl}{separator}{queryString}";
    }

    public static string BuildUrl(string baseUrl, Dictionary<string, string> parameters)
    {
        return new QueryBuilder()
            .WithBaseUrl(baseUrl)
            .AddRange(parameters)
            .BuildUrl();
    }

    private bool IsQueryStringOnly(string input)
    {
        return input.StartsWith('?') ||
               (input.Contains('=') && !input.Contains("://"));
    }

    private void ParseUrl(string url)
    {
        if (!url.Contains('?'))
        {
            _baseUrl = url;
            return;
        }

        var parts = url.Split('?', 2);
        _baseUrl = parts[0];
        ParseQueryString(parts[1]);
    }

    private void ParseQueryString(string queryString)
    {
        if (string.IsNullOrEmpty(queryString))
            return;

        var pairs = queryString.Split('&');
        foreach (var pair in pairs)
        {
            if (string.IsNullOrWhiteSpace(pair))
                continue;

            var kv = pair.Split('=', 2);
            if (kv.Length != 2)
                continue;

            var key = Uri.UnescapeDataString(kv[0]);
            var value = Uri.UnescapeDataString(kv[1]);
            _parameters[key] = value;
        }
    }

    public QueryBuilder Clone()
    {
        var clone = new QueryBuilder
        {
            _baseUrl = _baseUrl
        };

        foreach (var param in _parameters) clone._parameters[param.Key] = param.Value;

        return clone;
    }

    public override string ToString()
    {
        return BuildQueryString();
    }
}