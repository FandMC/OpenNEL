using System.Text.Json;
using System.Threading;
using Serilog;
using OpenNEL.type;

namespace OpenNEL.Manager;

public class SettingManager
{
    private const string SettingsFilePath = "setting.json";
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };
    private static readonly SemaphoreSlim InstanceLock = new SemaphoreSlim(1, 1);
    private static SettingManager? _instance;
    private readonly SemaphoreSlim _saveSemaphore = new SemaphoreSlim(1, 1);
    private SettingData _settings = new SettingData();

    public static SettingManager Instance
    {
        get
        {
            if (_instance != null) return _instance;
            InstanceLock.Wait();
            try { return _instance ?? (_instance = new SettingManager()); }
            finally { InstanceLock.Release(); }
        }
    }

    private SettingManager()
    {
        ReadFromDisk();
    }

    public SettingData Get() => _settings;

    public void Update(SettingData data)
    {
        _settings = data ?? new SettingData();
        SaveToDisk();
    }

    public void ReadFromDisk()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                Log.Information("未找到设置文件，使用默认设置");
                _settings = new SettingData();
                return;
            }
            var text = File.ReadAllText(SettingsFilePath);
            var obj = JsonSerializer.Deserialize<SettingData>(text) ?? new SettingData();
            _settings = obj;
            Log.Information("设置已从磁盘加载");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载设置失败，使用默认值");
            _settings = new SettingData();
        }
    }

    public void SaveToDisk()
    {
        _saveSemaphore.Wait();
        try
        {
            var text = JsonSerializer.Serialize(_settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, text);
            Log.Debug("设置已保存到磁盘");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存设置失败");
        }
        finally
        {
            _saveSemaphore.Release();
        }
    }
}
