/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenNEL.SDK.RakNet;
using OpenNEL.Interceptors;
using OpenNEL.GameLauncher.Services.Java;
using OpenNEL.GameLauncher.Services.Bedrock;
using OpenNEL_WinUI.Entities.Web.NEL;

namespace OpenNEL_WinUI.Manager;

internal class GameManager
{
    private readonly Lock _lock = new Lock();
    static readonly Dictionary<Guid, OpenNEL.GameLauncher.Services.Java.LauncherService> Launchers = new();
    static readonly Dictionary<Guid, OpenNEL.GameLauncher.Services.Bedrock.LauncherService> PeLaunchers = new();
    static readonly Dictionary<Guid, Interceptor> Interceptors = new();
    static readonly Dictionary<Guid, IRakNet> PeInterceptors = new();
    static readonly object Lock = new object();
    public static GameManager Instance { get; } = new GameManager();

    public sealed class LockScope : IDisposable
    {
        readonly object l;
        public LockScope(object o){l=o; Monitor.Enter(l);} 
        public void Dispose(){ Monitor.Exit(l);} 
    }
    public static LockScope EnterScope(object o)=>new LockScope(o);

    public List<EntityQueryInterceptors> GetQueryInterceptors()
    {
        return Interceptors.Values.Select((interceptor, index) => new EntityQueryInterceptors
        {
            Id = index.ToString(),
            Name = interceptor.Identifier,
            Address = $"{interceptor.ForwardAddress}:{interceptor.ForwardPort}",
            Role = interceptor.NickName,
            Server = interceptor.ServerName,
            Version = interceptor.ServerVersion,
            LocalAddress = $"{interceptor.LocalAddress}:{interceptor.LocalPort}"
        }).ToList();
    }
    
    public void ShutdownInterceptor(Guid identifier)
    {
        Interceptor value = null;
        var has = false;
        using (EnterScope(Lock))
        {
            if (Interceptors.TryGetValue(identifier, out value))
            {
                Interceptors.Remove(identifier);
                has = true;
            }
        }
        if (has)
        {
            value.ShutdownAsync();
        }
    }
    public void AddInterceptor(Interceptor interceptor)
    {
        using (_lock.EnterScope())
        {
            Interceptors.Add(interceptor.Identifier, interceptor);
        }
    }
}
