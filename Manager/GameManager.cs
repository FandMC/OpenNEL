using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Codexus.Development.SDK.RakNet;
using Codexus.Interceptors;
using OpenNEL.Entities.Web.NEL;

namespace OpenNEL.Manager;

internal class GameManager
{
    private readonly Lock _lock = new Lock();
    static readonly Dictionary<Guid, Codexus.Game.Launcher.Services.Java.LauncherService> Launchers = new();
    static readonly Dictionary<Guid, Codexus.Game.Launcher.Services.Bedrock.LauncherService> PeLaunchers = new();
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
