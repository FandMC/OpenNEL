using OpenNEL.Network;
using System.Text.Json;
using Codexus.Development.SDK.Manager;
using Serilog;

namespace OpenNEL.Message.Plugin;

internal class RestartGatewayMessage : IWsMessage
{
    public string Type => "restart";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        Log.Information("重启网关");
        PluginManager.RestartGateway();
        return new { type = "restart_ack" };
    }
}
