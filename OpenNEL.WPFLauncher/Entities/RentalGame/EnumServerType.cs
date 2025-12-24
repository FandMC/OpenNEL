using System.Runtime.Serialization;

namespace OpenNEL.WPFLauncher.Entities.RentalGame;

public enum EnumServerType
{
    [EnumMember(Value = "docker")]
    Docker,
    [EnumMember(Value = "vmware")]
    Vmware
}
