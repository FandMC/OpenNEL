using System.Text.Json;

namespace OpenNEL.Network;

internal interface IWsMessage
{
    string Type { get; }
    Task<object?> ProcessAsync(JsonElement root);
}
