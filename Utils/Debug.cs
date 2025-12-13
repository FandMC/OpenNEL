using System;
using OpenNEL.Manager;

namespace OpenNEL.Utils;

public class Debug
{
    public static bool Get()
    {
        try
        {
            var s = SettingManager.Instance.Get();
            return s?.Debug ?? false;
        }
        catch{}
        return false;
    }
}
