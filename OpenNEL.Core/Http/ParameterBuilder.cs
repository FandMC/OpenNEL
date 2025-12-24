namespace OpenNEL.Core.Http;

public class ParameterBuilder
{
    private readonly Dictionary<string, string> _parameters = new();

    public string? Url { get; set; }

    public ParameterBuilder()
    {
    }

    public ParameterBuilder(string parameter)
    {
        if (parameter.Contains('?'))
        {
            Url = parameter[..parameter.IndexOf('?')];
            var startIndex = parameter.IndexOf('?') + 1;
            parameter = parameter[startIndex..];
        }
        
        var pairs = parameter.Split('&');
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=');
            if (keyValue.Length == 2)
            {
                _parameters[keyValue[0]] = keyValue[1];
            }
        }
    }

    public string Get(string parameter)
    {
        return _parameters.TryGetValue(parameter, out var value) ? value : string.Empty;
    }

    public ParameterBuilder Append(string key, string value)
    {
        _parameters[key] = value;
        return this;
    }

    public ParameterBuilder Remove(string key)
    {
        _parameters.Remove(key);
        return this;
    }

    public string FormUrlEncode()
    {
        var values = _parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}");
        return string.Join("&", values);
    }

    public string ToQueryUrl()
    {
        return Url + "?" + FormUrlEncode();
    }

    public override string ToString()
    {
        return _parameters.Aggregate(string.Empty, (current, kv) => 
            (current == string.Empty ? current : current + "&") + kv.Key + "=" + kv.Value);
    }
}
