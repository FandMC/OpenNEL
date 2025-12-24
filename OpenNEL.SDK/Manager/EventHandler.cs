namespace OpenNEL.SDK.Manager;

public delegate void EventHandler<in T>(T args) where T : IEventArgs;
