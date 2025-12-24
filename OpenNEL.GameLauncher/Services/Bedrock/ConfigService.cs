using System.Security.Cryptography;
using System.Text.Json;
using OpenNEL.GameLauncher.Utils;

namespace OpenNEL.GameLauncher.Services.Bedrock;

public class ConfigService
{
    public static void GenerateLaunchConfig(string skinPath, string roleName, string entityId, int port)
    {
        string skinHash = Convert.ToHexString(MD5.HashData(File.ReadAllBytes(skinPath)));
        string contents = JsonSerializer.Serialize(new
        {
            room_info = new
            {
                ip = "127.0.0.1",
                port = (uint)port,
                room_name = "OpenNEL Server",
                item_ids = new string[1] { entityId }
            },
            player_info = new
            {
                user_id = 1,
                user_name = roleName,
                urs = "OpenNEL Server"
            },
            skin_info = new
            {
                skin = skinPath.Replace("\\\\", "\\"),
                hash = skinHash.ToLower(),
                slim = false,
                skin_iid = "100"
            },
            misc = new
            {
                multiplayer_game_type = 100,
                auth_server_url = ""
            }
        });
        File.WriteAllTextAsync(Path.Combine(PathUtil.CppGamePath, "launch.cppconfig"), contents).GetAwaiter().GetResult();
    }
}
