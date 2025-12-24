using System.Diagnostics;
using OpenNEL.SDK.RakNet;
using OpenNEL.SDK.Utils;
using OpenNEL.GameLauncher.Entities;
using OpenNEL.GameLauncher.Utils;
using OpenNEL.GameLauncher.Utils.Progress;
using OpenNEL.WPFLauncher.Entities.NetGame.Texture;
using Serilog;

namespace OpenNEL.GameLauncher.Services.Bedrock;

public sealed class LauncherService : IDisposable
{
    private readonly IProgress<EntityProgressUpdate> _progress;
    private readonly string _userToken;
    private readonly object _disposeLock = new();
    private Process? _gameProcess;
    private IRakNet? _rakNet;
    private volatile bool _disposed;

    public Guid Identifier { get; } = Guid.NewGuid();
    public EntityLaunchPeGame Entity { get; }
    public EntityProgressUpdate LastProgress { get; private set; }

    public event Action<Guid>? Exited;

    private LauncherService(EntityLaunchPeGame entityLaunchGame, string userToken, IProgress<EntityProgressUpdate> progress)
    {
        Entity = entityLaunchGame ?? throw new ArgumentNullException(nameof(entityLaunchGame));
        _userToken = userToken ?? throw new ArgumentNullException(nameof(userToken));
        _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        LastProgress = new EntityProgressUpdate
        {
            Id = Identifier,
            Percent = 0,
            Message = "Initialized"
        };
    }

    public static LauncherService CreateLauncher(EntityLaunchPeGame entityLaunchGame, string userToken, IProgress<EntityProgressUpdate> progress)
    {
        LauncherService launcherService = new(entityLaunchGame, userToken, progress);
        Task.Run((Func<Task?>)launcherService.LaunchGameAsync);
        return launcherService;
    }

    private async Task LaunchGameAsync()
    {
        try
        {
            if (_disposed)
            {
                return;
            }
            await DownloadGameResourcesAsync().ConfigureAwait(continueOnCapturedContext: false);
            if (!_disposed)
            {
                int port = await LaunchProxyAsync().ConfigureAwait(continueOnCapturedContext: false);
                if (!_disposed)
                {
                    await StartGameProcessAsync(port).ConfigureAwait(continueOnCapturedContext: false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            UpdateProgress(100, "Launch cancelled");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while launching game for {GameId}", Entity.GameId);
            UpdateProgress(100, "Launch failed");
        }
    }

    private async Task DownloadGameResourcesAsync()
    {
        UpdateProgress(5, "Installing game resources");
        if (!await InstallerService.DownloadMinecraftAsync().ConfigureAwait(continueOnCapturedContext: false))
        {
            throw new InvalidOperationException("Failed to download Minecraft resources");
        }
    }

    private Task<int> LaunchProxyAsync()
    {
        UpdateProgress(60, "Launching proxy");
        int availablePort = NetworkUtil.GetAvailablePort();
        int availablePort2 = NetworkUtil.GetAvailablePort();
        string remoteAddress = $"{Entity.ServerIp}:{Entity.ServerPort}";
        bool isRental = Entity.GameType == EnumGType.ServerGame;
        try
        {
            _rakNet = RakNetLoader.ConstructLoader().Create(remoteAddress, Entity.AccessToken, Entity.GameId, Convert.ToUInt32(Entity.UserId), _userToken, Entity.GameName, Entity.RoleName, availablePort, availablePort2, isRental);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Bedrock interceptor failed to launch for {GameId}", Entity.GameId);
            throw new InvalidOperationException("Failed to initialize RakNet proxy", ex);
        }
        return Task.FromResult(availablePort);
    }

    private Task StartGameProcessAsync(int port)
    {
        UpdateProgress(70, "Launching game process");
        string launchPath = GetLaunchPath();
        ValidateLaunchPath(launchPath);
        ConfigService.GenerateLaunchConfig(Entity.SkinPath, Entity.RoleName, Entity.GameId, port);
        string argumentsPath = Path.Combine(PathUtil.CppGamePath, "launch.cppconfig");
        Process? process = CommandService.StartGame(launchPath, argumentsPath);
        if (process == null)
        {
            Log.Error($"Game launch failed for LaunchType: {Entity.LaunchType}, Role: {Entity.RoleName}");
            throw new InvalidOperationException("Failed to start game process");
        }
        SetupGameProcess(process);
        UpdateProgress(100, "Running");
        Log.Information("Game launched successfully. LaunchType: {LaunchType}, ProcessID: {ProcessId}, Role: {Role}", Entity.LaunchType, process.Id, Entity.RoleName);
        return Task.CompletedTask;
    }

    private string GetLaunchPath()
    {
        if (Entity.LaunchType == EnumLaunchType.Custom && !string.IsNullOrEmpty(Entity.LaunchPath))
        {
            return Path.Combine(Entity.LaunchPath, "windowsmc", "Minecraft.Windows.exe");
        }
        return Path.Combine(PathUtil.CppGamePath, "windowsmc", "Minecraft.Windows.exe");
    }

    private static void ValidateLaunchPath(string launchPath)
    {
        if (!File.Exists(launchPath))
        {
            throw new FileNotFoundException("Executable not found at " + launchPath, launchPath);
        }
    }

    private void SetupGameProcess(Process process)
    {
        _gameProcess = process;
        _gameProcess.EnableRaisingEvents = true;
        _gameProcess.Exited += OnGameProcessExited;
    }

    private void OnGameProcessExited(object? sender, EventArgs e)
    {
        try
        {
            Exited?.Invoke(Identifier);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error in game process exit handler for {GameId}", Entity.GameId);
        }
    }

    private void UpdateProgress(int percent, string message)
    {
        if (_disposed)
        {
            return;
        }
        LastProgress = new EntityProgressUpdate
        {
            Id = Identifier,
            Percent = percent,
            Message = message
        };
        try
        {
            _progress.Report(LastProgress);
            if (percent == 100)
            {
                SyncProgressBarUtil.ProgressBar.ClearCurrent();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error reporting progress for {GameId}", Entity.GameId);
        }
    }

    public Process? GetProcess()
    {
        if (!_disposed)
        {
            return _gameProcess;
        }
        return null;
    }

    public void ShutdownAsync()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        lock (_disposeLock)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
        }
        try
        {
            if (_gameProcess != null)
            {
                _gameProcess.Exited -= OnGameProcessExited;
                if (!_gameProcess.HasExited)
                {
                    _gameProcess.CloseMainWindow();
                    if (!_gameProcess.WaitForExit(5000))
                    {
                        _gameProcess.Kill();
                    }
                }
                _gameProcess.Dispose();
                _gameProcess = null;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error disposing game process for {GameId}", Entity.GameId);
        }
        try
        {
            _rakNet?.Shutdown();
            _rakNet = null;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error shutting down RakNet for {GameId}", Entity.GameId);
        }
    }
}
