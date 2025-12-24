using System.Security.Cryptography;
using System.Text.Json;
using OpenNEL.Core.Utils;
using OpenNEL.GameLauncher.Utils;
using OpenNEL.Core.Progress;
using OpenNEL.WPFLauncher;
using OpenNEL.WPFLauncher.Entities;
using OpenNEL.WPFLauncher.Entities.Minecraft;
using OpenNEL.WPFLauncher.Entities.NetGame;
using OpenNEL.WPFLauncher.Entities.NetGame.Mods;
using OpenNEL.WPFLauncher.Entities.NetGame.Texture;
using Serilog;

namespace OpenNEL.GameLauncher.Services.Java;

public static class InstallerService
{
    public static async Task<bool> PrepareMinecraftClient(string userId, string userToken, WPFLauncherClient wpfLauncher, EnumGameVersion gameVersion)
    {
        string versionName = Enum.GetName(gameVersion)!;
        string md5Path = Path.Combine(PathUtil.GameBasePath, "GAME_BASE.MD5");
        string zipPath = Path.Combine(PathUtil.CachePath, "GameBase.7z");
        string versionMd5File = Path.Combine(PathUtil.GameBasePath, versionName + ".MD5");
        string versionZip = Path.Combine(PathUtil.CachePath, versionName + ".7z");
        string libMd5File = Path.Combine(PathUtil.GameBasePath, versionName + "_Lib.MD5");
        string libZip = Path.Combine(PathUtil.CachePath, versionName + "_Lib.7z");
        Entity<EntityCoreLibResponse> minecraftClientLibs = wpfLauncher.GetMinecraftClientLibs(userId, userToken);
        if (minecraftClientLibs.Code != 0)
        {
            throw new Exception("Failed to fetch base package: " + minecraftClientLibs.Message);
        }
        await ProcessPackage(minecraftClientLibs.Data!.Url, zipPath, PathUtil.GameBasePath, md5Path, minecraftClientLibs.Data.Md5, "base package");
        Entity<EntityCoreLibResponse> versionResult = wpfLauncher.GetMinecraftClientLibs(userId, userToken, gameVersion);
        if (versionResult.Code != 0)
        {
            throw new Exception("Failed to fetch " + versionName + " package: " + versionResult.Message);
        }
        await ProcessPackage(versionResult.Data!.Url, versionZip, PathUtil.GameBasePath, versionMd5File, versionResult.Data.Md5, versionName + " package");
        await ProcessPackage(versionResult.Data.CoreLibUrl, libZip, PathUtil.CachePath, libMd5File, versionResult.Data.CoreLibMd5, versionName + " libraries");
        InstallCoreLibs(Path.Combine(PathUtil.CachePath, versionName + "_libs"), gameVersion);
        return true;
    }

    private static async Task ProcessPackage(string url, string zipPath, string extractTo, string md5Path, string md5, string label)
    {
        bool valid = File.Exists(md5Path);
        if (valid)
        {
            valid = await File.ReadAllTextAsync(md5Path) == md5;
        }
        if (!valid)
        {
            using SyncProgressBarUtil.ProgressBar progress = new(100);
            IProgress<SyncProgressBarUtil.ProgressReport> uiProgress = new SyncCallback<SyncProgressBarUtil.ProgressReport>(update =>
            {
                progress.Update(update.Percent, update.Message);
            });
            await DownloadUtil.DownloadAsync(url, zipPath, p =>
            {
                uiProgress.Report(new SyncProgressBarUtil.ProgressReport
                {
                    Percent = (int)p,
                    Message = "Downloading " + label
                });
            });
            CompressionUtil.Extract7Z(zipPath, extractTo, p =>
            {
                uiProgress.Report(new SyncProgressBarUtil.ProgressReport
                {
                    Percent = p,
                    Message = "Extracting " + label
                });
            });
            await File.WriteAllTextAsync(md5Path, md5);
            FileUtil.DeleteFileSafe(zipPath);
        }
    }

    private static void InstallCoreLibs(string libPath, EnumGameVersion gameVersion)
    {
        string gameVersionFromEnum = GameVersionUtil.GetGameVersionFromEnum(gameVersion);
        string forgePrefix = "forge-" + gameVersionFromEnum + "-";
        string launchWrapperPrefix = "launchwrapper-";
        string mercuriusPrefix = "MercuriusUpdater-";
        string versionJar = gameVersionFromEnum + ".jar";
        string versionJson = gameVersionFromEnum + ".json";
        if (!Directory.Exists(libPath))
        {
            return;
        }
        string[] files = Directory.GetFiles(libPath, "*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            if (fileName.StartsWith(forgePrefix))
            {
                string baseName = Path.GetFileNameWithoutExtension(file);
                string path = baseName.Replace("forge-", "");
                string targetDir = Path.Combine(PathUtil.GameBasePath, ".minecraft", "libraries\\net\\minecraftforge\\forge", path);
                string targetFile = Path.Combine(targetDir, baseName + ".jar");
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
                else if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                }
                File.Copy(file, targetFile, overwrite: true);
            }
            else if (fileName.StartsWith(launchWrapperPrefix))
            {
                string baseName = Path.GetFileNameWithoutExtension(file);
                string path = baseName.Replace("launchwrapper-", "");
                string targetDir = Path.Combine(PathUtil.GameBasePath, ".minecraft", "libraries\\net\\minecraft\\launchwrapper", path);
                string targetFile = Path.Combine(targetDir, baseName + ".jar");
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
                else if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                }
                File.Copy(file, targetFile, overwrite: true);
            }
            else if (fileName.StartsWith(mercuriusPrefix))
            {
                string baseName = Path.GetFileNameWithoutExtension(file);
                string path = baseName.Replace("MercuriusUpdater-", "");
                string targetDir = Path.Combine(PathUtil.GameBasePath, ".minecraft", "libraries\\net\\minecraftforge\\MercuriusUpdater", path);
                string targetFile = Path.Combine(targetDir, baseName + ".jar");
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
                else if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                }
                File.Copy(file, targetFile, overwrite: true);
            }
            else if (fileName.Equals(versionJar))
            {
                string destFileName = Path.Combine(PathUtil.GameBasePath, ".minecraft", "versions", gameVersionFromEnum, versionJar);
                File.Copy(file, destFileName, overwrite: true);
            }
            else if (fileName.Equals(versionJson))
            {
                string destFileName = Path.Combine(PathUtil.GameBasePath, ".minecraft", "versions", gameVersionFromEnum, versionJson);
                File.Copy(file, destFileName, overwrite: true);
            }
            else if (fileName.StartsWith("modlauncher-") && fileName.Contains("9.1.0"))
            {
                string destFileName = Path.Combine(PathUtil.GameBasePath, ".minecraft", "libraries\\cpw\\mods\\modlauncher\\9.1.0\\modlauncher-9.1.0.jar");
                File.Copy(file, destFileName, overwrite: true);
            }
            else if (fileName.StartsWith("modlauncher-") && fileName.Contains("10.0.9"))
            {
                string destFileName = Path.Combine(PathUtil.GameBasePath, ".minecraft", "libraries\\cpw\\mods\\modlauncher\\10.0.9\\modlauncher-10.0.9.jar");
                File.Copy(file, destFileName, overwrite: true);
            }
            else if (fileName.StartsWith("modlauncher-") && fileName.Contains("10.2.1"))
            {
                string destFileName = Path.Combine(PathUtil.GameBasePath, ".minecraft", "libraries\\net\\minecraftforge\\modlauncher\\10.2.1\\modlauncher-10.2.1.jar");
                File.Copy(file, destFileName, overwrite: true);
            }
        }
        FileUtil.DeleteDirectorySafe(libPath);
    }

    public static async Task<EntityModsList?> InstallGameMods(string userId, string userToken, EnumGameVersion gameVersion, WPFLauncherClient wpfLauncher, string gameId, bool isRental)
    {
        Entity<EntityQuerySearchByGameResponse> entity = await wpfLauncher.GetGameCoreModListAsync(userId, userToken, gameVersion, isRental);
        if (entity.Data?.IidList == null)
        {
            return null;
        }
        Entities<EntityComponentDownloadInfoResponse> entities = await wpfLauncher.GetGameCoreModDetailsListAsync(userId, userToken, entity.Data.IidList);
        EntityModsList modList = new();
        foreach (EntityComponentDownloadInfoResponse entityComponentDownloadInfoResponse in entities.Data)
        {
            foreach (EntityComponentDownloadInfoResponseSub subEntity in entityComponentDownloadInfoResponse.SubEntities)
            {
                modList.Mods.Add(new EntityModsInfo
                {
                    ModPath = $"{entityComponentDownloadInfoResponse.ItemId}@{entityComponentDownloadInfoResponse.MTypeId}@0.jar",
                    Id = $"{entityComponentDownloadInfoResponse.ItemId}@{entityComponentDownloadInfoResponse.MTypeId}@0.jar",
                    Iid = entityComponentDownloadInfoResponse.ItemId,
                    Md5 = subEntity.JarMd5.ToUpper(),
                    Name = "",
                    Version = ""
                });
            }
        }
        using SyncProgressBarUtil.ProgressBar progress = new(100);
        IProgress<SyncProgressBarUtil.ProgressReport> uiProgress = new SyncCallback<SyncProgressBarUtil.ProgressReport>(update =>
        {
            progress.Update(update.Percent, update.Message);
        });
        string corePath = Path.Combine(PathUtil.GameModsPath, gameId);
        if (Directory.Exists(corePath))
        {
            Directory.Delete(corePath, recursive: true);
        }
        int total = int.Parse(entities.Total);
        int idx = 0;
        foreach (EntityComponentDownloadInfoResponse entityComponentDownloadInfoResponse2 in entities.Data)
        {
            idx++;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(entityComponentDownloadInfoResponse2.SubEntities[0].ResName);
            string jar = Path.Combine(corePath, $"{fileNameWithoutExtension}@{entityComponentDownloadInfoResponse2.MTypeId}@{entityComponentDownloadInfoResponse2.EntityId}.jar");
            string archive = Path.Combine(corePath, entityComponentDownloadInfoResponse2.SubEntities[0].ResName);
            string extractDir = Path.Combine(corePath, fileNameWithoutExtension);
            if (!File.Exists(jar) || !FileUtil.ComputeMd5FromFile(jar).Equals(entityComponentDownloadInfoResponse2.SubEntities[0].JarMd5, StringComparison.OrdinalIgnoreCase))
            {
                int currentIdx = idx;
                await DownloadUtil.DownloadAsync(entityComponentDownloadInfoResponse2.SubEntities[0].ResUrl, archive, dp =>
                {
                    uiProgress.Report(new SyncProgressBarUtil.ProgressReport
                    {
                        Percent = (int)dp,
                        Message = $"Downloading core mod {currentIdx}/{total}"
                    });
                });
                CompressionUtil.Extract7Z(archive, extractDir, p =>
                {
                    uiProgress.Report(new SyncProgressBarUtil.ProgressReport
                    {
                        Percent = p * 100 / total,
                        Message = $"Extracting core mod {currentIdx}/{total}"
                    });
                });
                string[] jarFiles = FileUtil.EnumerateFiles(extractDir, "jar");
                foreach (string jarFile in jarFiles)
                {
                    FileUtil.CopyFileSafe(jarFile, jar);
                }
                FileUtil.DeleteDirectorySafe(extractDir);
                FileUtil.DeleteFileSafe(archive);
            }
        }
        uiProgress.Report(new SyncProgressBarUtil.ProgressReport
        {
            Percent = 100,
            Message = "Core mods ready"
        });
        string compDir = Path.Combine(PathUtil.CachePath, "Game", gameId);
        string compArchive = compDir + ".7z";
        FileUtil.CreateDirectorySafe(compDir);
        Entity<EntityComponentDownloadInfoResponse> netGameComponentDownloadList = wpfLauncher.GetNetGameComponentDownloadList(userId, userToken, gameId);
        if (netGameComponentDownloadList?.Data != null && netGameComponentDownloadList.Code == 0)
        {
            EntityComponentDownloadInfoResponseSub? comp = netGameComponentDownloadList.Data.SubEntities.FirstOrDefault();
            string md5File = Path.Combine(compDir, gameId + ".MD5");
            string modListFile = Path.Combine(compDir, gameId + ".json");
            bool needDownload = !File.Exists(md5File);
            if (!needDownload)
            {
                needDownload = await File.ReadAllTextAsync(md5File) != comp?.ResMd5;
            }
            if (!needDownload && File.Exists(modListFile))
            {
                foreach (EntityModsInfo mod in JsonSerializer.Deserialize<EntityModsList>(await File.ReadAllTextAsync(modListFile))!.Mods)
                {
                    modList.Mods.Add(mod);
                }
                uiProgress.Report(new SyncProgressBarUtil.ProgressReport
                {
                    Percent = 100,
                    Message = "Game assets ready"
                });
                SyncProgressBarUtil.ProgressBar.ClearCurrent();
                return modList;
            }
            FileUtil.DeleteFileSafe(compArchive);
            await DownloadUtil.DownloadAsync(comp!.ResUrl, compArchive, p =>
            {
                uiProgress.Report(new SyncProgressBarUtil.ProgressReport
                {
                    Percent = (int)p,
                    Message = "Downloading game assets"
                });
            });
            FileUtil.DeleteDirectorySafe(compDir);
            CompressionUtil.Extract7Z(compArchive, compDir, p =>
            {
                uiProgress.Report(new SyncProgressBarUtil.ProgressReport
                {
                    Percent = p,
                    Message = "Extracting game assets"
                });
            });
            string[] modJars = FileUtil.EnumerateFiles(Path.Combine(compDir, ".minecraft", "mods"), "jar");
            EntityModsList serverModsList = new();
            foreach (string modJar in modJars)
            {
                string jarFileName = Path.GetFileName(modJar);
                string md5Hash = Convert.ToHexString(MD5.HashData(await File.ReadAllBytesAsync(modJar))).ToUpper();
                serverModsList.Mods.Add(new EntityModsInfo
                {
                    Name = "",
                    Version = "",
                    ModPath = jarFileName,
                    Id = jarFileName,
                    Iid = jarFileName.Split('@')[0],
                    Md5 = md5Hash
                });
            }
            modList.Mods.AddRange(serverModsList.Mods);
            await File.WriteAllTextAsync(md5File, comp.ResMd5);
            await File.WriteAllTextAsync(modListFile, JsonSerializer.Serialize(serverModsList));
            FileUtil.DeleteFileSafe(compArchive);
        }
        uiProgress.Report(new SyncProgressBarUtil.ProgressReport
        {
            Percent = 100,
            Message = "Game assets ready"
        });
        SyncProgressBarUtil.ProgressBar.ClearCurrent();
        return modList;
    }

    private static void InstallCustomMods(string mods)
    {
        FileUtil.EnumerateFiles(PathUtil.CustomModsPath, "jar").ToList().ForEach(f =>
        {
            FileUtil.CopyFileSafe(f, Path.Combine(mods, Path.GetFileName(f)));
        });
    }

    public static string PrepareGameRuntime(string userId, string gameId, string roleName, EnumGType gameType)
    {
        string path = HashUtil.GenerateGameRuntimeId(gameId, roleName);
        string runtimeDir = Path.Combine(PathUtil.GamePath, "Runtime", path);
        string minecraftDir = Path.Combine(runtimeDir, ".minecraft");
        if (!Directory.Exists(runtimeDir))
        {
            Directory.CreateDirectory(runtimeDir);
        }
        if (gameType == EnumGType.NetGame)
        {
            string modsDir = Path.Combine(minecraftDir, "mods");
            FileUtil.DeleteDirectorySafe(modsDir);
            FileUtil.CreateDirectorySafe(modsDir);
            FileUtil.CopyDirectory(Path.Combine(PathUtil.CachePath, "Game", gameId, ".minecraft"), minecraftDir, includeRoot: false);
            InstallCustomMods(modsDir);
        }
        string assetsDir = Path.Combine(minecraftDir, "assets");
        string targetPath = Path.Combine(PathUtil.GameBasePath, ".minecraft", "assets");
        if (Directory.Exists(assetsDir))
        {
            FileUtil.DeleteDirectorySafe(assetsDir);
        }
        FileUtil.CreateSymbolicLinkSafe(assetsDir, targetPath);
        return runtimeDir;
    }

    public static void InstallCoreMods(string gameId, string targetModsPath)
    {
        string coreModsDir = Path.Combine(PathUtil.GameModsPath, gameId);
        if (Directory.Exists(coreModsDir))
        {
            FileUtil.CreateDirectorySafe(targetModsPath);
            string[] files = FileUtil.EnumerateFiles(coreModsDir);
            foreach (string file in files)
            {
                string destPath = Path.Combine(targetModsPath, Path.GetRelativePath(coreModsDir, file));
                FileUtil.CreateDirectorySafe(Path.GetDirectoryName(destPath));
                FileUtil.CopyFileSafe(file, destPath);
            }
        }
    }

    public static void InstallNativeDll(EnumGameVersion gameVersion)
    {
        try
        {
            string dllPath = Path.Combine(PathUtil.ResourcePath, "api-ms-win-crt-utility-l1-1-1.dll");
            string nativesDir = Path.Combine(PathUtil.GameBasePath, ".minecraft", "versions", GameVersionUtil.GetGameVersionFromEnum(gameVersion), "natives", "runtime");
            if (!Directory.Exists(nativesDir))
            {
                FileUtil.CreateDirectorySafe(nativesDir);
            }
            if (!File.Exists(dllPath))
            {
                throw new Exception("Native dll not found: " + dllPath);
            }
            string destPath = Path.Combine(nativesDir, "api-ms-win-crt-utility-l1-1-1.dll");
            FileUtil.CopyFileSafe(dllPath, destPath);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to install native dll:" + ex);
        }
    }
}
