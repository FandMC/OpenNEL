using Serilog;
using System.Text;

namespace OpenNEL.Utils;

public static class FileUtil
{
    public static bool OverwriteWithText(string path, string text, Encoding? encoding = null)
    {
        try
        {
            if (!File.Exists(path))
            {
                Log.Error("目标文件不存在: {Path}", path);
                return false;
            }
            encoding ??= Encoding.UTF8;
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
            fs.SetLength(0);
            var bytes = encoding.GetBytes(text);
            fs.Write(bytes, 0, bytes.Length);
            fs.Flush(true);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "复写文件失败: {Path}", path);
            return false;
        }
    }

    public static bool OverwriteWithBytes(string path, ReadOnlySpan<byte> data)
    {
        try
        {
            if (!File.Exists(path))
            {
                Log.Error("目标文件不存在: {Path}", path);
                return false;
            }
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
            fs.SetLength(0);
            fs.Write(data);
            fs.Flush(true);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "复写文件失败: {Path}", path);
            return false;
        }
    }
}
