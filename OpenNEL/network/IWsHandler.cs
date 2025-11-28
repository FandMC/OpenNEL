using System.Text.Json;

namespace OpenNEL.network;

internal interface IWsHandler
{
    string Type { get; }
    Task<object?> ProcessAsync(JsonElement root);
}
