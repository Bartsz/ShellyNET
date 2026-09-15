using System.ComponentModel;

namespace ShellyNET.Client;

public enum ComponentEvent
{
    Unkown = 0,
    [Description("long_push")]
    LongPush,

    [Description("single_push")]
    SinglePush,

    [Description("double_push")]
    DoublePush,

    [Description("triple_push")]
    TriplePush,

    [Description("btn_down")]
    BtnDown,

    [Description("btn_up")]
    BtnUp,
}
