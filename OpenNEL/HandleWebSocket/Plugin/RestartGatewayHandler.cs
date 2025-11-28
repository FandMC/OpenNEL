using OpenNEL.network;
using System.Text.Json;
using Codexus.Development.SDK.Manager;
using Serilog;

namespace OpenNEL.HandleWebSocket.Plugin;

internal class RestartGatewayHandler : IWsHandler
{
    public string Type => "restart";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        Log.Information("重启网关");
        PluginManager.RestartGateway();
        return new { type = "restart_ack" };
    }
}
