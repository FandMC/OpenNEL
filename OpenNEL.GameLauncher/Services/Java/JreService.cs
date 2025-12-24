using OpenNEL.Core.Utils;
using OpenNEL.GameLauncher.Utils;
using OpenNEL.Core.Progress;
using Serilog;

namespace OpenNEL.GameLauncher.Services.Java;

public static class JreService
{
    public static async Task<bool> PrepareJavaRuntime()
    {
        string jreFile = Path.Combine(PathUtil.JavaPath, "Jre.7z");
        using SyncProgressBarUtil.ProgressBar progress = new(100);
        IProgress<SyncProgressBarUtil.ProgressReport> uiProgress = new SyncCallback<SyncProgressBarUtil.ProgressReport>(update =>
        {
            progress.Update(update.Percent, update.Message);
        });
        await DownloadUtil.DownloadAsync("https://x19.gdl.netease.com/jre-v64-220420.7z", jreFile, p =>
        {
            uiProgress.Report(new SyncProgressBarUtil.ProgressReport
            {
                Percent = (int)p,
                Message = "Downloading JRE"
            });
        });
        try
        {
            CompressionUtil.Extract7Z(jreFile, PathUtil.JavaPath, p =>
            {
                uiProgress.Report(new SyncProgressBarUtil.ProgressReport
                {
                    Percent = p,
                    Message = "Extracting JRE"
                });
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to extract JRE");
            return false;
        }
        File.Delete(jreFile);
        SyncProgressBarUtil.ProgressBar.ClearCurrent();
        return true;
    }
}
