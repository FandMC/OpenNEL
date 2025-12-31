using System;
using System.Threading.Tasks;
using Codexus.Development.SDK.Connection;
using Codexus.Development.SDK.Entities;
using Codexus.Development.SDK.Enums;
using Codexus.Development.SDK.Packet;
using Codexus.Development.SDK.Utils;
using DotNetty.Buffers;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.type;
using OpenNEL.Core.Utils;
using Serilog;

namespace OpenNEL_WinUI.Packet;

[RegisterPacket(EnumConnectionState.Play, EnumPacketDirection.ClientBound, 0x1D, EnumProtocolVersion.V1206, false)]
public class SPlayDisconnect : IPacket
{
	public TextComponent Reason { get; set; } = new();

	public EnumProtocolVersion ClientProtocolVersion { get; set; }

	private const string BJDGameId = "4661334467366178884";

	public void ReadFromBuffer(IByteBuffer buffer)
	{
		Reason = TextComponentSerializer.Deserialize(buffer);
		Log.Debug("[Disconnect] Parsed: Text={Text}, Extra.Count={Count}", Reason.Text, Reason.Extra.Count);
	}

	public void WriteToBuffer(IByteBuffer buffer)
	{
		var serialized = TextComponentSerializer.Serialize(Reason);
		buffer.WriteBytes(serialized);
	}

	public bool HandlePacket(GameConnection connection)
	{
		var displayText = Reason.DisplayText;
		Log.Information("[Disconnect] GameId={GameId}, Reason={Reason}", connection.GameId, displayText);

		if (IsBanMessage(displayText))
		{
			Log.Warning("检测到ban消息: {Reason}", displayText);

			/*
			var banTypeComponent = new TextComponent
			{
				Text = $"\n\n",
				Color = "yellow"
			};
			var wrapper = new TextComponent { Text = "" };
			wrapper.Extra.Add(Reason);
			wrapper.Extra.Add(banTypeComponent);
			Reason = wrapper;
			*/

			var autoAction = AppState.AutoDisconnectOnBan;
			if (autoAction == "close")
			{
				var interceptorId = connection.InterceptorId;
				_ = Task.Run(async () =>
				{
					await Task.Delay(500);
					Log.Warning("正在关闭 Interceptor...");
					GameManager.Instance.ShutdownInterceptor(interceptorId);
					NotificationHost.ShowGlobal("检测到封禁,已成功关闭通道", ToastLevel.Success);
				});
			}
			else if (autoAction == "switch")
			{
				var interceptorId = connection.InterceptorId;
				var userId = connection.Session.UserId;
				var userToken = connection.Session.UserToken;
				var serverId = connection.GameId;
				var currentRole = connection.NickName;
				
				var interceptor = GameManager.Instance.GetInterceptor(interceptorId);
				var serverName = interceptor?.ServerName ?? string.Empty;
				
				var settings = SettingManager.Instance.Get();
				var socks5 = settings.Socks5Enabled ? new EntitySocks5
				{
					Enabled = true,
					Address = settings.Socks5Address,
					Port = settings.Socks5Port,
					Username = settings.Socks5Username,
					Password = settings.Socks5Password
				} : new EntitySocks5();
				
				_ = Task.Run(async () =>
				{
					await Task.Delay(500);
					Log.Warning("检测到封禁，正在关闭当前通道并切换角色...");
					GameManager.Instance.ShutdownInterceptor(interceptorId);
					
					await BannedRoleTracker.TrySwitchToAnotherRole(
						userId, 
						userToken, 
						serverId, 
						serverName, 
						currentRole, 
						socks5);
				});
			}
		}

		if (connection.GameId == BJDGameId &&
		    displayText.Contains("invalidSession", StringComparison.OrdinalIgnoreCase))
		{
			Log.Warning("[布吉岛] 检测到协议异常");

			Reason = new TextComponent
			{
				Text = "[OpenNEL] 协议未正确工作，请检查插件是否正确安装",
				Color = "red"
			};
		}

		return false;
	}

	private static bool IsBanMessage(string message)
	{
		if (string.IsNullOrEmpty(message)) return false;

		if (message.Contains("封禁", StringComparison.OrdinalIgnoreCase))
			return true;

		return false;
	}
}
