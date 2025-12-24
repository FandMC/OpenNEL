using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Packet;
using Serilog;

namespace OpenNEL.SDK.Manager;

public class PacketManager
{
	private static PacketManager? _instance;

	private readonly Dictionary<Type, Dictionary<EnumProtocolVersion, int>> _ids = new Dictionary<Type, Dictionary<EnumProtocolVersion, int>>();

	private readonly Dictionary<Type, RegisterPacket> _metadata = new Dictionary<Type, RegisterPacket>();

	private readonly Dictionary<EnumConnectionState, Dictionary<EnumPacketDirection, Dictionary<EnumProtocolVersion, Dictionary<int, Type>>>> _packets = new Dictionary<EnumConnectionState, Dictionary<EnumPacketDirection, Dictionary<EnumProtocolVersion, Dictionary<int, Type>>>>();

	private readonly bool _registered;

	private readonly Dictionary<Type, EnumConnectionState> _states = new Dictionary<Type, EnumConnectionState>();

	public static PacketManager Instance => _instance ?? (_instance = new PacketManager());

	private PacketManager()
	{
		RegisterDefaultPackets();
		_registered = true;
	}

	private void RegisterDefaultPackets()
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		Assembly[] array = assemblies;
		foreach (Assembly assembly in array)
		{
			RegisterPacketFromAssembly(assembly);
		}
	}

	public void RegisterPacketFromAssembly(Assembly assembly)
	{
		foreach (Type item in from type in assembly.GetTypes()
			where typeof(IPacket).IsAssignableFrom(type) && (object)type != null && !type.IsAbstract && !type.IsInterface
			select type)
		{
			List<RegisterPacket> list = item.GetCustomAttributes<RegisterPacket>(inherit: false).ToList();
			if (list.Count == 0)
			{
				continue;
			}
			foreach (RegisterPacket item2 in list)
			{
				RegisterPacket(item2, item);
			}
		}
	}

	public void RegisterPacket(RegisterPacket metadata, Type type)
	{
		if (type.GetConstructor(Type.EmptyTypes) == null)
		{
			throw new InvalidOperationException("Type '" + type.FullName + "' does not have a parameterless constructor.");
		}
		_states[type] = metadata.State;
		_metadata[type] = metadata;
		if (!_ids.TryGetValue(type, out var value))
		{
			value = new Dictionary<EnumProtocolVersion, int>();
			_ids[type] = value;
		}
		if (!_packets.TryGetValue(metadata.State, out var value2))
		{
			value2 = new Dictionary<EnumPacketDirection, Dictionary<EnumProtocolVersion, Dictionary<int, Type>>>();
			_packets[metadata.State] = value2;
		}
		if (!value2.TryGetValue(metadata.Direction, out var value3))
		{
			value3 = new Dictionary<EnumProtocolVersion, Dictionary<int, Type>>();
			value2[metadata.Direction] = value3;
		}
		EnumProtocolVersion[] versions = metadata.Versions;
		for (int i = 0; i < versions.Length; i++)
		{
			EnumProtocolVersion key = versions[i];
			int[] packetIds = metadata.PacketIds;
			int num = Math.Min(packetIds.Length - 1, i);
			int num2 = packetIds[num];
			if (!value3.TryGetValue(key, out var value4))
			{
				Dictionary<int, Type> dictionary = (value3[key] = new Dictionary<int, Type>());
				value4 = dictionary;
			}
			value[key] = num2;
			value4[num2] = type;
		}
	}

	public IPacket? BuildPacket(EnumConnectionState state, EnumPacketDirection direction, EnumProtocolVersion version, int packetId)
	{
		if (!_packets.TryGetValue(state, out var value) || !value.TryGetValue(direction, out var value2) || !value2.TryGetValue(version, out var value3) || !value3.TryGetValue(packetId, out var value4))
		{
			return null;
		}
		try
		{
			return (IPacket)Activator.CreateInstance(value4);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to build packet", Array.Empty<object>());
			return null;
		}
	}

	public int GetPacketId(EnumProtocolVersion version, IPacket packet)
	{
		Type type = packet.GetType();
		if (!_ids.TryGetValue(type, out var value))
		{
			return -1;
		}
		return value.GetValueOrDefault(version, -1);
	}

	public RegisterPacket? GetMetadata(IPacket packet)
	{
		Type type = packet.GetType();
		return _metadata.GetValueOrDefault(type);
	}

	public EnumConnectionState GetState(IPacket packet)
	{
		Type type = packet.GetType();
		return _states.GetValueOrDefault(type);
	}

	public void EnsureRegistered()
	{
		if (_registered)
		{
			return;
		}
		throw new InvalidOperationException("Should never call CheckIsRegistered()");
	}
}
