using Serilog;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using OpenNEL.type;

namespace OpenNEL.Utils;

internal static class Hwid
{
    public static string Compute()
    {
        try
        {
            var machine = Environment.MachineName;
            var os = Environment.OSVersion.VersionString;
            var cpu = Environment.ProcessorCount.ToString();
            var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var root = Path.GetPathRoot(win) ?? "";
            var serial = GetVolumeSerial(root);
            var macs = string.Join(";", NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.OperationalStatus == OperationalStatus.Up)
                .Select(n => n.GetPhysicalAddress()?.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .OrderBy(s => s));
            var s = string.Join("|", new[] { machine, os, cpu, root, serial, macs });
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            return Convert.ToHexString(hash);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "计算HWID失败");
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName));
            return Convert.ToHexString(hash);
        }
    }

    public static async Task<string?> ReportAsync(string? hwid = null, string? endpoint = null)
    {
        try
        {
            var h = hwid ?? Compute();
            var url = endpoint ?? AppInfo.HwidEndpoint;
            using var client = new HttpClient();
            using var content = new StringContent(h, Encoding.UTF8, "text/plain");
            var resp = await client.PostAsync(url, content);
            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }
            return text;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    static string GetVolumeSerial(string root)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(root)) return "";
            if (!GetVolumeInformation(root, null, 0, out uint serial, out _, out _, null, 0)) return "";
            return serial.ToString("X");
        }
        catch
        {
            return "";
        }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    static extern bool GetVolumeInformation(string lpRootPathName, StringBuilder? lpVolumeNameBuffer, int nVolumeNameSize, out uint lpVolumeSerialNumber, out uint lpMaximumComponentLength, out uint lpFileSystemFlags, StringBuilder? lpFileSystemNameBuffer, int nFileSystemNameSize);
}
