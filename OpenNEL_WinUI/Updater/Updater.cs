/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Codexus.Game.Launcher.Utils;
using Codexus.Game.Launcher.Utils.Progress;
using OpenNEL_WinUI.type;
using Serilog;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;
using SharpCompress.Common;

namespace OpenNEL_WinUI.Updater;

public static class Updater
{
	private const string LastVersionUrl = AppInfo.ApiBaseURL + "/v1/lastversion";

	private static readonly HttpClient Http = new HttpClient();

	private static int CompareVersion(string a, string b)
	{
		var sa = a.Trim().TrimStart('v', 'V').Split('.');
		var sb = b.Trim().TrimStart('v', 'V').Split('.');
		var n = Math.Max(sa.Length, sb.Length);
		for (int i = 0; i < n; i++)
		{
			var ai = i < sa.Length && int.TryParse(sa[i], out var va) ? va : 0;
			var bi = i < sb.Length && int.TryParse(sb[i], out var vb) ? vb : 0;
			if (ai != bi) return ai > bi ? 1 : -1;
		}
		return 0;
	}

	public static async Task UpdateAsync(string newVersion)
	{
		if (AppState.Pre)
		{
			return;
		}
		Uri uri = new Uri(LastVersionUrl);
		try
		{
			await using Stream responseStream = await Http.GetStreamAsync(uri);
            using JsonDocument jsonDoc = await JsonDocument.ParseAsync(responseStream);
            string latestVersion = newVersion;
            string downloadUrl = null;
            var root = jsonDoc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("version", out var verEl)) latestVersion = verEl.GetString();
                if (root.TryGetProperty("downloadurl", out var urlEl)) downloadUrl = urlEl.GetString();
            }
            else if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                latestVersion = root[0].GetProperty("version").GetString();
            }
			if (!string.IsNullOrWhiteSpace(latestVersion) && !string.IsNullOrWhiteSpace(newVersion))
			{
				var cmp = CompareVersion(newVersion, latestVersion);
				if (cmp >= 0)
				{
					Log.Information("[Update] 当前版本不低于远程版本: {current} >= {latest}", newVersion, latestVersion);
					return;
				}
			}
			// Log.Information("[Update] 新版本下载地址: {downloadUrl}", downloadUrl);
			SyncProgressBarUtil.ProgressBar progress = new SyncProgressBarUtil.ProgressBar(100);
			IProgress<SyncProgressBarUtil.ProgressReport> uiProgress = new SyncCallback<SyncProgressBarUtil.ProgressReport>(delegate(SyncProgressBarUtil.ProgressReport update)
			{
				progress.Update(update.Percent, update.Message);
			});
			string tempDir = PathUtil.UpdaterPath;
			string zipPath = Path.Combine(tempDir, (latestVersion ?? newVersion) + ".zip");
            if (!string.IsNullOrWhiteSpace(downloadUrl))
			{
				await Downloader.CreateDownloadTaskAsync(downloadUrl, zipPath, delegate(int p)
				{
					uiProgress.Report(new SyncProgressBarUtil.ProgressReport
                    {
                        Percent = p,
                        Message = "正在下载更新资源"
                    });
				}, async delegate(bool success, Exception? exception)
				{
					_ = 1;
					try
					{
                        if (!success)
                        {
                            Log.Error<string, Exception>("[Update] 下载更新失败: {downloadUrl}, {exception}", downloadUrl, exception);
                            return;
                        }
                        if (!File.Exists(zipPath))
                        {
                            Log.Error("[Update] 压缩包不存在: {zipPath}", zipPath);
                            return;
                        }
						IProgress<SyncProgressBarUtil.ProgressReport> compressProgress = new SyncCallback<SyncProgressBarUtil.ProgressReport>(delegate(SyncProgressBarUtil.ProgressReport update)
						{
							progress.Update(update.Percent, update.Message);
						});
                        try
                        {
                            Directory.CreateDirectory(tempDir);
                            using var archive = ZipArchive.Open(zipPath, (ReaderOptions)null);
                            var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();
                            int total = entries.Count;
                            int done = 0;
                            foreach (var entry in entries)
                            {
                                entry.WriteToDirectory(tempDir, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                                done++;
                                int percent = total == 0 ? 100 : (int)(done * 100.0 / total);
                                compressProgress.Report(new SyncProgressBarUtil.ProgressReport
                                {
                                    Percent = percent,
                                    Message = "正在解压更新资源"
                                });
                            }
                            Log.Information("[Update] 解压条目数量: {Count}", total);
                        }
                        finally
                        {
                        }
                        Log.Information("[Update] 更新资源已下载到: {tempDir}", tempDir);
                        string extractedExe = Path.Combine(tempDir, "OpenNEL.exe");
                        if (!File.Exists(extractedExe))
                        {
                            Log.Error("[Update] 解压后的文件缺失: {exe}", extractedExe);
                            return;
                        }
                        FileUtil.DeleteFileSafe(zipPath);
                        string fileName = Path.GetFileName(Environment.ProcessPath);
                        string directoryName = Path.GetDirectoryName(Environment.ProcessPath);
                        string scriptPath = PathUtil.ScriptPath;
                        string contents = GenerateUpdateScript(tempDir, directoryName, fileName);
                        await File.WriteAllTextAsync(scriptPath, contents);
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/C \"" + scriptPath + "\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                        Environment.Exit(0);
                    }
                    catch (Exception ex2)
                    {
                        Log.Error("[Update] 解压或脚本生成出错: {exception}", ex2);
                    }
                });
            }
            else
            {
                Log.Error("[Update] 下载地址为空。");
            }
        }
        catch (Exception ex)
        {
            Log.Error("[Update] 更新过程中发生错误: {exception}", ex);
        }
    }

	private static string GenerateUpdateScript(string tempDir, string targetDir, string exeName)
	{
		return $"timeout /t 1 /nobreak\r\nxcopy /e /y /i \"{tempDir}\\*\" \"{targetDir}\\\"\r\nstart \"\" \"{Path.Combine(targetDir, exeName)}\"";
	}
}
