using OpenNEL.SDK.Attributes;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Event;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Plugin;
using OpenNEL.Interceptors.Event;
using OpenNEL.Interceptors.Packet.Login.Server;
using Codexus.Base1200.Plugin.Event;
using Codexus.Base1200.Plugin.Packet.Play.Server.Simulation;
using Codexus.Base1200.Plugin.Utils;
using Codexus.Base1200.Plugin.Utils.Nbt;
using Codexus.Base1200.Plugin.Utils.Patch.Metadata;
using Codexus.Base1200.Plugin.Packet.Play.Server.Patch;
using DotNetty.Common.Concurrency;
using Serilog;
using Heypixel2.Core;
using Heypixel2.Data;

namespace Heypixel2
{
    /// <summary>
    /// Heypixel 协议插件 - 基于 OpenNEL SDK
    /// </summary>
    [Plugin("F110DA9F-F0CB-F926-C72C-FEAC7FCFAAAA", 
            "Heypixel Protocol v2", 
            "A clean implementation of Heypixel protocol for OpenNEL.", 
            "Heypixel2", 
            "2.0.0")]
    public class HeypixelPlugin : IPlugin
    {
        public async Task OnEnable()
        {
            // 初始化映射数据 (从服务器下载)
            await MappingLoader.InitializeAsync();
            Log.Information("[Heypixel2] 插件已启用，映射数据已加载");
        }

        #region 常量

        /// <summary>
        /// 目标游戏ID (布吉岛)
        /// </summary>
        public const string TARGET_GAME_ID = "4661334467366178884";

        /// <summary>
        /// 要求的协议版本 (1.20.6)
        /// </summary>
        public const int REQUIRED_PROTOCOL_VERSION = 766;

        /// <summary>
        /// 协议通道名称
        /// </summary>
        public const string CHANNEL_NAME = "heypixel:s2cevent";

        /// <summary>
        /// 需要注册的频道列表
        /// </summary>
        public static readonly HashSet<string> REGISTERED_CHANNELS = new()
        {
            "worldedit:cui",
            "fml:loginwrapper",
            "forge:tier_sorting",
            "storemod:buy",
            "floodgate:custom",
            "floodgate:packet",
            "heypixel:s2cevent",
            "report:areport",
            "plugin:guild",
            "fml:play",
            "floodgate:netease",
            "floodgate:transfer",
            "fml:handshake",
            "heypixel:onlinestats",
            "forge:split",
            "floodgate:form",
            "geckolib:main",
            "floodgate:skin"
        };

        #endregion

        #region 单例

        private static HeypixelPlugin? _instance;
        public static HeypixelPlugin Instance => _instance ?? throw new InvalidOperationException("Plugin not initialized");

        #endregion

        public HeypixelPlugin()
        {
            _instance = this;
        }

        /// <summary>
        /// 插件初始化 - 注册事件处理器
        /// </summary>
        public void OnInitialize()
        {
            OnEnable().GetAwaiter().GetResult();
            
            // 初始化消息注册表
            MessageRegistry.Initialize();

            // 注册事件处理器
            EventManager.Instance.RegisterHandler("channel_v1206", 
                (OpenNEL.SDK.Manager.EventHandler<EventLoginSuccess>)OnLoginSuccess);
            
            EventManager.Instance.RegisterHandler("base_1200", 
                (OpenNEL.SDK.Manager.EventHandler<EventPluginMessage>)OnPluginMessage);
            
            EventManager.Instance.RegisterHandler("base_1200", 
                (OpenNEL.SDK.Manager.EventHandler<EventSwingArm>)OnSwingArm);
            
            EventManager.Instance.RegisterHandler("base_1200", 
                (OpenNEL.SDK.Manager.EventHandler<EventGameJoin>)OnGameJoin);
            
            EventManager.Instance.RegisterHandler("base_1200", 
                (OpenNEL.SDK.Manager.EventHandler<EventUseItemOn>)OnUseItemOn);
            
            EventManager.Instance.RegisterHandler("base_1200", 
                (OpenNEL.SDK.Manager.EventHandler<EventUseItem>)OnUseItem);

            EventManager.Instance.RegisterHandler("channel_v1206", 
                (OpenNEL.SDK.Manager.EventHandler<EventHandshake>)OnHandshake);

            EventManager.Instance.RegisterHandler("channel_v1206", 
                (OpenNEL.SDK.Manager.EventHandler<EventSetEntityMetadata>)OnSetEntityMetadata);

            Log.Information("[Heypixel2] Done");
        }

        #region 事件处理器

        /// <summary>
        /// 检查是否是目标游戏
        /// </summary>
        private static bool IsTargetGame(EventArgsBase args)
        {
            return args.Connection.GameId == TARGET_GAME_ID;
        }

        /// <summary>
        /// 获取或创建会话管理器
        /// </summary>
        private static SessionManager GetSession(GameConnection connection)
        {
            return SessionStore.GetOrCreate(connection);
        }

        /// <summary>
        /// 握手事件 - 检查版本
        /// </summary>
        private static void OnHandshake(object eventObj)
        {
            var args = (EventHandshake)eventObj;
            if (!IsTargetGame(args)) return;

            if (args.Handshake.ProtocolVersion != REQUIRED_PROTOCOL_VERSION)
            {
                Log.Warning("[Heypixel2] 需要 Minecraft 1.20.6 版本");
                
                var connection = args.Connection;
                if ((int)connection.ProtocolVersion != REQUIRED_PROTOCOL_VERSION)
                {
                    // 发送断开连接消息
                    connection.ClientChannel.WriteAndFlushAsync(new SPacketDisconnect
                    {
                        Reason = new TextComponent
                        {
                            Text = "[OpenNEL] 布吉岛强制要求使用 1.20.6 版本进入服务器",
                            Color = "yellow"
                        }.ToJson()
                    }).ContinueWith(task =>
                    {
                        if (task.IsCompleted) connection.Shutdown();
                    });
                }
            }
        }

        /// <summary>
        /// 登录成功事件 - 启动后台任务
        /// </summary>
        private static void OnLoginSuccess(EventArgsBase args)
        {
            if (!IsTargetGame(args)) return;

            var connection = args.Connection;
            var session = GetSession(connection);

            // 启动心跳线程
            ((AbstractEventExecutorGroup)connection.TaskGroup).ScheduleAsync(() =>
            {
                session.StartHeartbeatLoop(connection);
            }, TimeSpan.Zero);

            // 启动点击同步线程
            ((AbstractEventExecutorGroup)connection.TaskGroup).ScheduleAsync(() =>
            {
                session.StartClickSyncLoop(connection);
            }, TimeSpan.Zero);

            Log.Debug("[Heypixel2] 登录成功，后台任务已启动");
        }

        /// <summary>
        /// 游戏加入事件 - 发送初始信息
        /// </summary>
        private static void OnGameJoin(EventArgsBase args)
        {
            if (!IsTargetGame(args)) return;

            var connection = args.Connection;
            var session = GetSession(connection);

            session.Activate();
            session.SendInitialInfo(connection);
        }

        /// <summary>
        /// Plugin Message 事件
        /// </summary>
        private void OnPluginMessage(EventPluginMessage args)
        {
            if (!IsTargetGame(args)) return;

            var direction = args.Direction;
            
            // 客户端 -> 服务端
            if ((int)direction == 0)
            {
                HandleClientMessage(args);
            }
            // 服务端 -> 客户端
            else if ((int)direction == 1)
            {
                HandleServerMessage(args);
            }
        }

        /// <summary>
        /// 处理客户端消息 (拦截并修改)
        /// </summary>
        private static void HandleClientMessage(EventPluginMessage args)
        {
            if (args.Identifier == "minecraft:brand")
            {
                // 伪造品牌为 forge
                args.Payload = Convert.FromBase64String("BWZvcmdl");
            }
            else if (args.Identifier == "minecraft:register")
            {
                // 取消原始注册，我们会自己注册
                args.IsCancelled = true;
            }
        }

        /// <summary>
        /// 处理服务端消息
        /// </summary>
        private void HandleServerMessage(EventPluginMessage args)
        {
            var connection = args.Connection;
            var session = GetSession(connection);

            // 调试：记录所有S2C消息
            Log.Debug("[Heypixel2] S2C消息: {Id}, Payload长度: {Len}", args.Identifier, args.Payload?.Length ?? 0);

            if (args.Identifier == "minecraft:register")
            {
                session.HandleChannelRegister(connection, args.Payload);
            }
            else if (args.Identifier == CHANNEL_NAME)
            {
                Log.Information("[Heypixel2] 收到 heypixel:s2cevent 消息! 长度: {Len}, 数据: {Data}", 
                    args.Payload.Length, Convert.ToBase64String(args.Payload.Take(Math.Min(64, args.Payload.Length)).ToArray()));
                session.HandleHeypixelMessage(connection, args.Payload);
            }
            else if (args.Identifier == "floodgate:form")
            {
                session.HandleFloodgateForm(connection, args.Payload);
            }
        }

        /// <summary>
        /// 挥手事件 (左键攻击)
        /// </summary>
        private static void OnSwingArm(EventArgsBase args)
        {
            if (!IsTargetGame(args)) return;
            
            var session = GetSession(args.Connection);
            session.ClickTracker.RecordSwingArm();
        }

        /// <summary>
        /// 使用物品事件
        /// </summary>
        private static void OnUseItem(EventArgsBase args)
        {
            if (!IsTargetGame(args)) return;
            
            var session = GetSession(args.Connection);
            session.ClickTracker.RecordUseItem();
        }

        /// <summary>
        /// 方块交互事件
        /// </summary>
        private static void OnUseItemOn(object eventObj)
        {
            var args = (EventUseItemOn)eventObj;
            if (!IsTargetGame(args)) return;

            var connection = args.Connection;
            var session = GetSession(connection);
            var player = Codexus.Base1200.Plugin.Extensions.AttributeExtensions.GetLocalPlayer(connection);

            if (player == null)
            {
                Log.Debug("[Heypixel2] Player not found");
                return;
            }

            // 发送方块交互包
            var packet = new BlockInteractPacket
            {
                PlayerX = (float)((Entity)player).X,
                PlayerY = (float)((Entity)player).Y,
                PlayerZ = (float)((Entity)player).Z,
                Direction = args.Data.Face,
                InteractType = 1,
                ClickX = (float)args.Data.Location.X + args.Data.CursorPositionX,
                ClickY = (float)args.Data.Location.Y + args.Data.CursorPositionY,
                ClickZ = (float)args.Data.Location.Z + args.Data.CursorPositionZ,
                BlockX = args.Data.Location.X,
                BlockY = args.Data.Location.Y,
                BlockZ = args.Data.Location.Z,
                InsideBlock = args.Data.InsideBlock,
                Yaw = ((Entity)player).Yaw,
                Pitch = ((Entity)player).Pitch,
                IsMainHand = false
            };

            session.SendMessage(connection, packet);
        }

        /// <summary>
        /// 实体元数据事件 - 移除本地玩家的Pose数据
        /// </summary>
        private static void OnSetEntityMetadata(object eventObj)
        {
            var args = (EventSetEntityMetadata)eventObj;
            if (!IsTargetGame(args)) return;

            var connection = args.Connection;
            var player = Codexus.Base1200.Plugin.Extensions.AttributeExtensions.GetLocalPlayer(connection);

            if (player == null) return;

            // 只处理本地玩家的元数据
            if (args.Packet.EntityId != player.PlayerId) return;

            // 获取Pose序列化器的ID
            var poseSerializerId = EntityDataSerializers.GetSerializedId(EntityDataSerializers.Pose);

            // 移除所有Pose相关的元数据项 (使用dynamic访问Serializer属性)
            int removedCount = args.Packet.PackedItems.RemoveAll(item =>
            {
                dynamic dynamicItem = item;
                var itemSerializerId = EntityDataSerializers.GetSerializedId(dynamicItem.Serializer);
                return itemSerializerId == poseSerializerId;
            });

            if (removedCount > 0)
            {
                Log.Information("[Heypixel2] 移除了 {Count} 个Pose元数据, EntityId={EntityId}", 
                    removedCount, args.Packet.EntityId);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 发送聊天消息给客户端
        /// </summary>
        public static void SendChatMessage(GameConnection connection, string message, string color = "white")
        {
            var packet = new SSystemChatMessageV1206
            {
                Message = new TextComponent
                {
                    Text = message,
                    Color = color
                },
                Overlay = false
            };

            connection.ClientChannel?.WriteAndFlushAsync(packet);
        }

        #endregion
    }
}
