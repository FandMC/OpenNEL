using System.Xml.Serialization;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public enum EnumGameVersion : uint
{
    [XmlEnum(Name = "")]
    NONE = 0u,
    [XmlEnum(Name = "100.0.0")]
    V_CPP = 100000000u,
    [XmlEnum(Name = "300.0.0")]
    V_X64_CPP = 300000000u,
    [XmlEnum(Name = "200.0.0")]
    V_RTX = 200000000u,
    [XmlEnum(Name = "1.7.10")]
    V_1_7_10 = 1007010u,
    [XmlEnum(Name = "1.10.2")]
    V_1_10_2 = 1010002u,
    [XmlEnum(Name = "1.8")]
    V_1_8 = 1008000u,
    [XmlEnum(Name = "1.11.2")]
    V_1_11_2 = 1011002u,
    [XmlEnum(Name = "1.12.2")]
    V_1_12_2 = 1012002u,
    [XmlEnum(Name = "1.8.8")]
    V_1_8_8 = 1008008u,
    [XmlEnum(Name = "1.8.9")]
    V_1_8_9 = 1008009u,
    [XmlEnum(Name = "1.9.4")]
    V_1_9_4 = 1009004u,
    [XmlEnum(Name = "1.6.4")]
    V_1_6_4 = 1006004u,
    [XmlEnum(Name = "1.7.2")]
    V_1_7_2 = 1007002u,
    [XmlEnum(Name = "1.12")]
    V_1_12 = 1012000u,
    [XmlEnum(Name = "1.13.2")]
    V_1_13_2 = 1013002u,
    [XmlEnum(Name = "1.14.3")]
    V_1_14_3 = 1014003u,
    [XmlEnum(Name = "1.15")]
    V_1_15 = 1015000u,
    [XmlEnum(Name = "1.16")]
    V_1_16 = 1016000u,
    [XmlEnum(Name = "1.18")]
    V_1_18 = 1018000u,
    [XmlEnum(Name = "1.19.2")]
    V_1_19_2 = 1019002u,
    [XmlEnum(Name = "1.20")]
    V_1_20 = 1020000u,
    [XmlEnum(Name = "1.20.6")]
    V_1_20_6 = 1020006u,
    [XmlEnum(Name = "1.21")]
    V_1_21 = 1021000u
}
