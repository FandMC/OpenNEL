/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using OpenNEL_WinUI.type;

namespace OpenNEL_WinUI.Manager;

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

    public ElementTheme GetAppTheme()
    {
        var mode = _settings.ThemeMode?.Trim().ToLowerInvariant() ?? "system";
        if (mode == "light") return ElementTheme.Light;
        if (mode == "dark") return ElementTheme.Dark;
        return ElementTheme.Default;
    }

    public void ApplyTheme(ContentDialog dialog) => dialog.RequestedTheme = GetAppTheme();
    public void ApplyTheme(FrameworkElement element) => element.RequestedTheme = GetAppTheme();

    public string? GetCopyIpText(string? ip, int port)
    {
        if (!_settings.AutoCopyIpOnStart || string.IsNullOrWhiteSpace(ip)) return null;
        return port > 0 ? $"{ip}:{port}" : ip;
    }

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
