using System.Text.Json.Serialization;
using OpenNEL.Core.Utils;

namespace OpenNEL.WPFLauncher.Entities.RentalGame;

public class EntityRentalGameServerAddress
{
    [JsonPropertyName("entity_id")]
    [JsonConverter(typeof(JsonConverters.StringFromNumberOrStringConverter))]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("mcserver_host")]
    public string McServerHost { get; set; } = string.Empty;

    [JsonPropertyName("mcserver_port")]
    public ushort McServerPort { get; set; }

    [JsonPropertyName("state")]
    public EnumServerStatus State { get; set; }

    [JsonPropertyName("cmcc_mcserver_host")]
    public string CmccMcServerHost { get; set; } = string.Empty;

    [JsonPropertyName("cmcc_mcserver_port")]
    public int CmccMcServerPort { get; set; }

    [JsonPropertyName("ctcc_mcserver_host")]
    public string CtccMcServerHost { get; set; } = string.Empty;

    [JsonPropertyName("ctcc_mcserver_port")]
    public int CtccMcServerPort { get; set; }

    [JsonPropertyName("cucc_mcserver_host")]
    public string CuccMcServerHost { get; set; } = string.Empty;

    [JsonPropertyName("cucc_mcserver_port")]
    public int CuccMcServerPort { get; set; }

    [JsonPropertyName("isp_enable")]
    public bool IspEnable { get; set; }
}
