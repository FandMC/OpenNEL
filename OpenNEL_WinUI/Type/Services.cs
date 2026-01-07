/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using Codexus.OpenSDK;
using Codexus.OpenSDK.Yggdrasil;
using Codexus.OpenSDK.Entities.Yggdrasil;
using OpenNEL.Core.Utils;

namespace OpenNEL_WinUI.type;

internal class Services(
    C4399 C4399,
    X19 X19,
    StandardYggdrasil Yggdrasil
)
{
    public C4399 C4399 { get; } = C4399;
    public X19 X19 { get; } = X19;
    public StandardYggdrasil Yggdrasil { get; private set; } = Yggdrasil;

    public void RefreshYggdrasil()
    {
        var salt = CrcSalt.GetCached();
        Yggdrasil = new StandardYggdrasil(new YggdrasilData
        {
            LauncherVersion = X19.GameVersion,
            Channel = "netease",
            CrcSalt = salt
        });
    }
}