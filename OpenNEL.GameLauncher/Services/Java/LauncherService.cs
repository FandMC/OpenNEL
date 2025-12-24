using System.Diagnostics;
using System.Net;
using System.Text.Json;
using OpenNEL.GameLauncher.Connection.Protocols;
using OpenNEL.Core.Cipher;
using OpenNEL.Core.Utils;
using OpenNEL.WPFLauncher;
using OpenNEL.WPFLauncher.Entities.NetGame;
using OpenNEL.WPFLauncher.Entities.NetGame.Mods;
using OpenNEL.WPFLauncher.Entities.NetGame.Texture;
using OpenNEL.GameLauncher.Entities;
using OpenNEL.GameLauncher.Services.Java.RPC;
using OpenNEL.GameLauncher.Utils;
using OpenNEL.GameLauncher.Utils.Progress;
using Serilog;

namespace OpenNEL.GameLauncher.Services.Java;

public sealed class LauncherService : IDisposable
{
    private const string Skip32Key = "SaintSteve";
    private const int DefaultSocketPort = 9876;
    private const int DefaultRpcPort = 11413;
    private const string JavaExeName = "javaw.exe";
    private const string MinecraftDirectory = ".minecraft";
    private const string ModsDirectory = "mods";
    private const string SkinsDirectory = "Skins";
    private const string CoreModPattern = "@3";
    private const string JarExtension = "jar";

    private readonly IProgress<EntityProgressUpdate> _progress;
    private readonly string _protocolVersion;
    private readonly Skip32Cipher _skip32;
    private readonly int _socketPort;
    private readonly string _userToken;
    private readonly WPFLauncherClient _wpf;
    private AuthLibProtocol? _authLibProtocol;
    private GameRpcService? _gameRpcService;
    private EntityModsList? _modList;
    private bool _disposed;

    public EntityLaunchGame Entity { get; }
    public Guid Identifier { get; }
    private Process? GameProcess { get; set; }
    public EntityProgressUpdate LastProgress { get; private set; }
    public event Action<Guid>? Exited;

    private LauncherService(EntityLaunchGame entityLaunchGame, string userToken, WPFLauncherClient wpfLauncher, string protocolVersion, IProgress<EntityProgressUpdate> progress)
    {
        Entity = entityLaunchGame ?? throw new ArgumentNullException(nameof(entityLaunchGame));
        _userToken = userToken ?? throw new ArgumentNullException(nameof(userToken));
        _wpf = wpfLauncher ?? throw new ArgumentNullException(nameof(wpfLauncher));
        _protocolVersion = protocolVersion ?? throw new ArgumentNullException(nameof(protocolVersion));
        _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        _skip32 = new Skip32Cipher(Skip32Key.Select(c => (byte)c).ToArray());
        _socketPort = NetworkUtil.GetAvailablePort(DefaultSocketPort);
        Identifier = Guid.NewGuid();
        LastProgress = new EntityProgressUpdate
        {
            Id = Identifier,
            Percent = 0,
            Message = "Initialized"
        };
    }

    public static LauncherService CreateLauncher(EntityLaunchGame entityLaunchGame, string userToken, WPFLauncherClient wpfLauncher, string protocolVersion, IProgress<EntityProgressUpdate> progress)
    {
        var launcherService = new LauncherService(entityLaunchGame, userToken, wpfLauncher, protocolVersion, progress);
        Task.Run(launcherService.LaunchGameAsync);
        return launcherService;
    }

    public Process? GetProcess() => GameProcess;

    public async Task ShutdownAsync()
    {
        try
        {
            _gameRpcService?.CloseControlConnection();
            if (GameProcess is { HasExited: false })
            {
                GameProcess.Kill();
                await GameProcess.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error occurred during shutdown");
        }
    }

    private async Task LaunchGameAsync()
    {
        var progressHandler = CreateProgressHandler();
        try
        {
            await ExecuteLaunchStepsAsync(progressHandler);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to launch game");
            ReportProgress(progressHandler, 100, "Game launch failed");
            throw;
        }
    }

    private async Task ExecuteLaunchStepsAsync(IProgress<EntityProgressUpdate> progressHandler)
    {
        ReportProgress(progressHandler, 5, "Installing game mods");
        var enumVersion = GameVersionUtil.Convert(Entity.GameVersionId);
        _modList = await InstallGameModsAsync(enumVersion);

        ReportProgress(progressHandler, 15, "Preparing Java runtime");
        await PrepareJavaRuntimeAsync(enumVersion);

        ReportProgress(progressHandler, 30, "Preparing Minecraft client");
        await PrepareMinecraftClientAsync(enumVersion);

        ReportProgress(progressHandler, 45, "Setting up runtime");
        string workingDirectory = SetupGameRuntime();

        ReportProgress(progressHandler, 60, "Applying core mods");
        ApplyCoreMods(workingDirectory);

        ReportProgress(progressHandler, 75, "Initializing launcher");
        var (commandService, rpcPort) = InitializeLauncher(enumVersion, workingDirectory);

        ReportProgress(progressHandler, 80, "Starting RPC service");
        LaunchRpcService(enumVersion, rpcPort);

        ReportProgress(progressHandler, 90, "Starting authentication socket service");
        StartAuthenticationService();

        ReportProgress(progressHandler, 95, "Launching game process");
        await StartGameProcessAsync(commandService, progressHandler);
    }

    private IProgress<EntityProgressUpdate> CreateProgressHandler()
    {
        var progress = new SyncProgressBarUtil.ProgressBar(100);
        var uiProgress = new SyncCallback<SyncProgressBarUtil.ProgressReport>(update =>
        {
            progress.Update(update.Percent, update.Message);
        });

        return new SyncCallback<EntityProgressUpdate>(update =>
        {
            uiProgress.Report(new SyncProgressBarUtil.ProgressReport
            {
                Percent = update.Percent,
                Message = update.Message
            });
            _progress.Report(update);
            LastProgress = update;
        });
    }

    private void ReportProgress(IProgress<EntityProgressUpdate> handler, int percent, string message)
    {
        handler.Report(new EntityProgressUpdate
        {
            Id = Identifier,
            Percent = percent,
            Message = message
        });
    }

    private async Task<EntityModsList?> InstallGameModsAsync(EnumGameVersion enumVersion)
    {
        return await InstallerService.InstallGameMods(Entity.UserId, _userToken, enumVersion, _wpf, Entity.GameId, Entity.GameType == EnumGType.ServerGame);
    }

    private static async Task PrepareJavaRuntimeAsync(EnumGameVersion enumVersion)
    {
        string path = enumVersion > EnumGameVersion.V_1_16 ? "jdk17" : "jre8";
        if (!File.Exists(Path.Combine(Path.Combine(PathUtil.JavaPath, path), "bin", JavaExeName)))
        {
            await JreService.PrepareJavaRuntime();
        }
    }

    private async Task PrepareMinecraftClientAsync(EnumGameVersion enumVersion)
    {
        await InstallerService.PrepareMinecraftClient(Entity.UserId, _userToken, _wpf, enumVersion);
    }

    private string SetupGameRuntime()
    {
        string path = InstallerService.PrepareGameRuntime(Entity.UserId, Entity.GameId, Entity.RoleName, Entity.GameType);
        InstallerService.InstallNativeDll(GameVersionUtil.Convert(Entity.GameVersionId));
        return Path.Combine(path, MinecraftDirectory);
    }

    private void ApplyCoreMods(string workingDirectory)
    {
        string modsPath = Path.Combine(workingDirectory, ModsDirectory);
        if (Entity.LoadCoreMods)
        {
            InstallerService.InstallCoreMods(Entity.GameId, modsPath);
        }
        else
        {
            RemoveCoreModFiles(modsPath);
        }
    }

    private static void RemoveCoreModFiles(string modsPath)
    {
        var files = FileUtil.EnumerateFiles(modsPath, JarExtension);
        foreach (var file in files)
        {
            if (file.Contains(CoreModPattern))
            {
                FileUtil.DeleteFileSafe(file);
            }
        }
    }

    private (CommandService commandService, int rpcPort) InitializeLauncher(EnumGameVersion enumVersion, string workingDirectory)
    {
        var commandService = new CommandService();
        int rpcPort = OpenNEL.Core.Utils.NetworkUtil.GetAvailablePort(DefaultRpcPort);

        commandService.Init(
            uuid: _skip32.GenerateRoleUuid(Entity.RoleName, Convert.ToUInt32(Entity.UserId)),
            gameVersion: enumVersion,
            maxMemory: Entity.MaxGameMemory,
            roleName: Entity.RoleName,
            serverIp: Entity.ServerIp,
            serverPort: Entity.ServerPort,
            userId: Entity.UserId,
            gameId: Entity.GameId,
            workPath: workingDirectory,
            socketPort: _socketPort,
            protocolVersion: _protocolVersion,
            isFilter: true,
            rpcPort: rpcPort,
            dToken: TokenUtil.GenerateEncryptToken(_userToken));

        return (commandService, rpcPort);
    }

    private void LaunchRpcService(EnumGameVersion gameVersion, int rpcPort)
    {
        string skinsPath = Path.Combine(PathUtil.CachePath, SkinsDirectory);
        if (!Directory.Exists(skinsPath))
        {
            Directory.CreateDirectory(skinsPath);
        }

        _gameRpcService = new GameRpcService(rpcPort, Entity.ServerIp, Entity.ServerPort.ToString(), Entity.RoleName, Entity.UserId, _userToken, gameVersion);
        _gameRpcService.Connect(skinsPath, _wpf.GetSkinListInGame, _wpf.GetNetGameComponentDownloadList);
    }

    private void StartAuthenticationService()
    {
        _authLibProtocol = new AuthLibProtocol(IPAddress.Parse("127.0.0.1"), _socketPort, JsonSerializer.Serialize(_modList), Entity.GameVersion, Entity.AccessToken);
        _authLibProtocol.Start();
    }

    private async Task StartGameProcessAsync(CommandService commandService, IProgress<EntityProgressUpdate> progressHandler)
    {
        var process = commandService.StartGame();
        if (process != null)
        {
            await HandleSuccessfulLaunch(process, progressHandler);
        }
        else
        {
            HandleFailedLaunch(progressHandler);
        }
    }

    private Task HandleSuccessfulLaunch(Process process, IProgress<EntityProgressUpdate> progressHandler)
    {
        GameProcess = process;
        GameProcess.EnableRaisingEvents = true;
        GameProcess.Exited += OnGameProcessExited;
        ReportProgress(progressHandler, 100, "Running");
        SyncProgressBarUtil.ProgressBar.ClearCurrent();
        Console.WriteLine();
        Log.Information("Game launched successfully. Game Version: {GameVersion}, Process ID: {ProcessId}, Role: {Role}", Entity.GameVersion, process.Id, Entity.RoleName);
        MemoryOptimizer.GetInstance();
        return Task.CompletedTask;
    }

    private void HandleFailedLaunch(IProgress<EntityProgressUpdate> progressHandler)
    {
        ReportProgress(progressHandler, 100, "Game launch failed");
        SyncProgressBarUtil.ProgressBar.ClearCurrent();
        Log.Error("Game launch failed. Game Version: {GameVersion}, Role: {Role}", Entity.GameVersion, Entity.RoleName);
    }

    private void OnGameProcessExited(object? sender, EventArgs e)
    {
        Exited?.Invoke(Identifier);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            try
            {
                _authLibProtocol?.Dispose();
                _gameRpcService?.CloseControlConnection();
                if (GameProcess is { HasExited: false })
                {
                    GameProcess.Kill();
                    GameProcess.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error occurred during disposal");
            }
        }
        _disposed = true;
    }

    ~LauncherService()
    {
        Dispose(disposing: false);
    }
}
