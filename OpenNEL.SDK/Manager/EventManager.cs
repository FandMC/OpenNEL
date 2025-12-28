using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenNEL.SDK.Manager;

public class EventManager
{
	private static EventManager? _instance;

	private readonly Dictionary<Type, Dictionary<string, SortedDictionary<int, List<Delegate>>>> _eventHandlers = new Dictionary<Type, Dictionary<string, SortedDictionary<int, List<Delegate>>>>();

	public static EventManager Instance => _instance ?? (_instance = new EventManager());

	public void RegisterHandler<T>(string channel, EventHandler<T> handler, int priority = 0) where T : IEventArgs
	{
		ArgumentNullException.ThrowIfNull(handler, "handler");
		if (string.IsNullOrEmpty(channel))
		{
			throw new ArgumentNullException("channel");
		}
		Type typeFromHandle = typeof(T);
		if (!_eventHandlers.TryGetValue(typeFromHandle, out var value))
		{
			value = new Dictionary<string, SortedDictionary<int, List<Delegate>>>();
			_eventHandlers[typeFromHandle] = value;
		}
		if (!value.TryGetValue(channel, out var value2))
		{
			Comparison<int> comparison = (int x, int y) => y.CompareTo(x);
			SortedDictionary<int, List<Delegate>> sortedDictionary = (value[channel] = new SortedDictionary<int, List<Delegate>>(Comparer<int>.Create(comparison)));
			value2 = sortedDictionary;
		}
		if (!value2.TryGetValue(priority, out var value3))
		{
			List<Delegate> list = (value2[priority] = new List<Delegate>());
			value3 = list;
		}
		value3.Add(handler);
	}

	public bool UnregisterHandler<T>(string channel, EventHandler<T>? handler) where T : IEventArgs
	{
		if (string.IsNullOrEmpty(channel) || handler == null || !_eventHandlers.TryGetValue(typeof(T), out var value) || !value.TryGetValue(channel, out var value2))
		{
			return false;
		}
		foreach (int item in value2.Keys.ToList())
		{
			List<Delegate> list = value2[item];
			if (list.Remove(handler))
			{
				if (list.Count != 0)
				{
					return true;
				}
				value2.Remove(item);
				if (value2.Count != 0)
				{
					return true;
				}
				value.Remove(channel);
				if (value.Count == 0)
				{
					_eventHandlers.Remove(typeof(T));
				}
				return true;
			}
		}
		return false;
	}

	public T TriggerEvent<T>(string channel, T args) where T : IEventArgs
	{
		return TriggerEvent(new string[1] { channel }, args);
	}

	public T TriggerEvent<T>(string[] channels, T args) where T : IEventArgs
	{
		if (args == null)
		{
			throw new ArgumentNullException("args");
		}
		foreach (string key in channels)
		{
			if (!_eventHandlers.TryGetValue(typeof(T), out var value) || !value.TryGetValue(key, out var priorityDict))
			{
				continue;
			}
			foreach (EventHandler<T> item in priorityDict.Keys.Select((int priority) => priorityDict[priority]).SelectMany((List<Delegate> handlers) => handlers.ToList()))
			{
				item(args);
			}
		}
		return args;
	}

	public bool HasHandlersForEvent<T>(string channel) where T : IEventArgs
	{
		if (string.IsNullOrEmpty(channel))
		{
			return false;
		}
		if (_eventHandlers.TryGetValue(typeof(T), out var value) && value.TryGetValue(channel, out var value2))
		{
			return value2.Values.Any((List<Delegate> list) => list.Count > 0);
		}
		return false;
	}

	public void ClearAllHandlers()
	{
		_eventHandlers.Clear();
	}

	public void ClearHandlersForEventType<T>() where T : IEventArgs
	{
		_eventHandlers.Remove(typeof(T));
	}

	public void ClearHandlersForEvent<T>(string channel) where T : IEventArgs
	{
		if (!string.IsNullOrEmpty(channel) && _eventHandlers.TryGetValue(typeof(T), out var value) && value.Remove(channel) && value.Count == 0)
		{
			_eventHandlers.Remove(typeof(T));
		}
	}

	public void ClearChannelHandlers(string channel)
	{
		if (string.IsNullOrEmpty(channel)) return;
		
		foreach (var eventType in _eventHandlers.Keys.ToList())
		{
			if (_eventHandlers[eventType].Remove(channel) && _eventHandlers[eventType].Count == 0)
			{
				_eventHandlers.Remove(eventType);
			}
		}
	}
}
