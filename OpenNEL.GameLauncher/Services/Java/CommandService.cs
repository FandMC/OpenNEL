using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using OpenNEL.Core.Utils;
using OpenNEL.GameLauncher.Utils;
using OpenNEL.WPFLauncher.Entities.Minecraft;
using OpenNEL.WPFLauncher.Entities.NetGame;
using OpenNEL.WPFLauncher.Entities.NetGame.Texture;

namespace OpenNEL.GameLauncher.Services.Java;

public class CommandService
{
    private readonly List<string> _jarList = new();
    private readonly List<EnumGameVersion> _newJavaVersionList;

    private string _authToken = "";
    private string _cmd = "";
    private string _gameId = "";
    private EnumGameVersion _gameVersion;
    private bool _isFilter = true;
    private string _protocolVersion = "";
    private string _relLibPath = "";
    private string _relVerPath = "";
    private string _roleName = "";
    private int _rpcPort = 11413;
    private string _serverIp = "";
    private int _serverPort;
    private string _userId = "";
    private string _uuid = "";
    private string _version = "";
    private string _workPath = "";

    public CommandService()
    {
        _newJavaVersionList =
        [
            EnumGameVersion.V_1_13_2,
            EnumGameVersion.V_1_14_3,
            EnumGameVersion.V_1_16,
            EnumGameVersion.V_1_18,
            EnumGameVersion.V_1_20,
            EnumGameVersion.V_1_21
        ];
    }

    public bool Init(EnumGameVersion gameVersion, int maxMemory, string roleName, string serverIp, int serverPort, string userId, string dToken, string gameId, string workPath, string uuid, int socketPort, string protocolVersion = "", bool isFilter = true, int rpcPort = 11413)
    {
        _roleName = roleName;
        _version = GameVersionUtil.GetGameVersionFromEnum(gameVersion);
        _serverIp = serverIp;
        _serverPort = serverPort;
        _gameVersion = gameVersion;
        _rpcPort = rpcPort;
        _userId = userId;
        _uuid = uuid;
        _authToken = dToken;
        _gameId = gameId;
        _isFilter = isFilter;
        _workPath = workPath;
        _relLibPath = "libraries\\";
        _relVerPath = "versions\\" + _version + "\\";
        _protocolVersion = protocolVersion;
        string path = Path.Combine(PathUtil.GameBasePath, ".minecraft", "versions", _version, _version + ".json");
        if (!File.Exists(path))
        {
            throw new Exception("Game version JSON not found, please go to Setting to fix the game file and try again.");
        }
        JsonSerializerOptions options = new()
        {
            AllowTrailingCommas = true
        };
        Dictionary<string, JsonElement> cfg = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(path), options)!;
        BuildJarLists(cfg, _version);
        if (_newJavaVersionList.Contains(gameVersion))
        {
            BuildCommandEx(cfg, _version, maxMemory, socketPort);
        }
        else
        {
            BuildCommand(cfg, _version, maxMemory, socketPort, _jarList);
        }
        return true;
    }

    public Process? StartGame()
    {
        return Process.Start(new ProcessStartInfo(Path.Combine((_gameVersion >= EnumGameVersion.V_1_16) ? PathUtil.Jre17Path : PathUtil.Jre8Path, "bin", "javaw.exe"), _cmd)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _workPath
        });
    }

    private void BuildJarLists(Dictionary<string, JsonElement> cfg, string version)
    {
        _jarList.Clear();
        if (cfg.TryGetValue("libraries", out var value) && value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in value.EnumerateArray())
            {
                if (item.TryGetProperty("name", out var nameValue))
                {
                    string[]? array = nameValue.GetString()?.Split(':');
                    if (array != null && array.Length >= 3 && !array[1].Contains("platform"))
                    {
                        string path = array[0].Replace('.', '\\');
                        string path2 = array[1] + "-" + array[2] + ".jar";
                        _jarList.Add(_relLibPath + Path.Combine(path, array[1], array[2], path2));
                    }
                }
            }
        }
        _jarList.Add(_relVerPath + version + ".jar");
    }

    private void BuildCommand(Dictionary<string, JsonElement> cfg, string version, int mem, int socketPort, List<string> jars)
    {
        StringBuilder sb = new();
        sb.Append(" -XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump");
        sb.Append(" -Xmx").Append(mem).Append("M");
        sb.Append(" -Xmn128M -XX:PermSize=64M -XX:MaxPermSize=128M");
        sb.Append(" -XX:+UseConcMarkSweepGC -XX:+CMSIncrementalMode -XX:-UseAdaptiveSizePolicy");
        AddNativePath(sb);
        sb.Append(" -cp \"");
        foreach (string jar in jars)
        {
            sb.Append(Path.Combine(PathUtil.GameBasePath, ".minecraft", jar)).Append(";");
        }
        sb.Append("\" ");
        sb.Append(cfg["mainClass"].GetString());
        sb.Append(' ');
        string text = cfg.GetValueOrDefault("minecraftArguments").GetString() ?? string.Empty;
        text = text.Replace("${version_name}", version)
            .Replace("${assets_root}", Path.Combine(PathUtil.GameBasePath, ".minecraft", "assets"))
            .Replace("${assets_index_name}", version)
            .Replace("${auth_uuid}", _uuid)
            .Replace("${auth_access_token}", RandomUtil.GetRandomString(32, "ABCDEF1234567890"))
            .Replace("${auth_player_name}", _roleName)
            .Replace("${user_properties}", GetUserProperties(version))
            .Replace("--userType ${user_type}", string.Empty)
            .Replace("--gameDir ${game_directory}", "--gameDir " + _workPath)
            .Replace("--versionType ${version_type}", string.Empty);
        sb.Append(text).Append(" --server ").Append(_serverIp)
            .Append(" --port ").Append(_serverPort)
            .Append(" --userPropertiesEx ").Append(GetUserPropertiesEx());
        sb.Insert(0, $" -DlauncherControlPort={socketPort} -DlauncherGameId={_gameId} -DuserId={_userId} -DToken={_authToken} -DServer=RELEASE ");
        _cmd = sb.ToString();
    }

    private void BuildCommandEx(Dictionary<string, JsonElement> cfg, string version, int mem, int socketPort)
    {
        string? text = cfg.GetValueOrDefault("parameter_arguments").GetString();
        string? text2 = cfg.GetValueOrDefault("jvm_arguments").GetString();
        if (text != null && text2 != null)
        {
            text2 = text2.Replace("-Xmx2G", string.Empty)
                .Replace("-DlibraryDirectory=libraries", "-DlibraryDirectory=" + Path.Combine(PathUtil.GameBasePath, ".minecraft", "libraries"));
            text = text.Replace("--assetsDir assets", "--assetsDir " + Path.Combine(PathUtil.GameBasePath, ".minecraft", "assets"))
                .Replace("--gameDir .", "--gameDir " + _workPath);
            text2 = ReplaceLib(text2, "-cp");
            text2 = ReplaceLib(text2, "-p");
            text = text.Replace("${auth_player_name}", _roleName)
                .Replace("${auth_uuid}", _uuid)
                .Replace("${auth_access_token}", (_gameVersion >= EnumGameVersion.V_1_18) ? "0" : RandomUtil.GetRandomString(32, "ABCDEF0123456789"));
            StringBuilder sb = new StringBuilder().Append(" -Xmx").Append(mem).Append("M -Xmn128M ")
                .Append(text2)
                .Append(' ')
                .Append(text);
            sb.Append(" --userProperties ").Append(GetUserProperties(version));
            sb.Append(" --userPropertiesEx ").Append(GetUserPropertiesEx());
            sb.Append(" --server ").Append(_serverIp);
            sb.Append(" --port ").Append(_serverPort);
            sb.Insert(0, $" -DlauncherControlPort={socketPort} -DlauncherGameId={_gameId} -DuserId={_userId} -DToken={_authToken} -DServer=RELEASE ");
            AddNativePath(sb);
            _cmd = sb.ToString();
        }
    }

    private static string ReplaceLib(string a, string opt)
    {
        string[] array = a.Split(' ');
        for (int i = 0; i < array.Length - 1; i++)
        {
            if (array[i] == opt)
            {
                string[] source = array[i + 1].Split(';');
                string newValue = string.Join(";", source.Select(l => Path.Combine(PathUtil.GameBasePath, ".minecraft", l)));
                a = a.Replace(array[i + 1], newValue);
                break;
            }
        }
        return a;
    }

    private void AddNativePath(StringBuilder sb)
    {
        string text = Path.Combine(PathUtil.GameBasePath, ".minecraft", "versions", _version, "natives");
        string text2 = Path.Combine(text, "runtime");
        sb.Insert(0, $" -Djava.library.path=\"{text.Replace("\\", "\\\\")}\" -Druntime_path=\"{text2.Replace("\\", "\\\\")}\" ");
    }

    private string GetUserPropertiesEx(EnumGType t = EnumGType.NetGame)
    {
        return JsonSerializer.Serialize(new EntityUserPropertiesEx
        {
            GameType = (int)t,
            Channel = "netease",
            TimeDelta = 0,
            IsFilter = _isFilter,
            LauncherVersion = _protocolVersion
        });
    }

    private string GetUserProperties(string version)
    {
        string format = (version == "1.7.10")
            ? "\"uid\":[{0}],\"gameid\":[{1}],\"launcherport\":[{2}],\\\"filterkey\\\":[\\\"{3}\\\",\\\"0\\\"],\\\"filterpath\\\":[\\\"\\\",\\\"0\\\"],\\\"timedelta\\\":[0,0],\\\"launchversion\\\":[\\\"{3}\\\",\\\"0\\\"]"
            : "\\\"uid\\\":[{0},0],\\\"gameid\\\":[{1},0],\\\"launcherport\\\":[{2},0],\\\"filterkey\\\":[\\\"{3}\\\",\\\"0\\\"],\\\"filterpath\\\":[\\\"\\\",\\\"0\\\"],\\\"timedelta\\\":[0,0],\\\"launchversion\\\":[\\\"{4}\\\",\\\"0\\\"]";
        string text = string.Format(format, _userId, 0, _rpcPort, RandomUtil.GetRandomString(32, "abcdefghijklmnopqrstuvwxyz"), _protocolVersion);
        return "\"{" + text + "}\"";
    }
}
