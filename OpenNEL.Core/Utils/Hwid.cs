using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace OpenNEL.Core.Utils;

public static class Hwid
{
    public static string Compute()
    {
        try
        {
            var os = Environment.OSVersion.VersionString;
            var cpu = Environment.ProcessorCount.ToString();
            var guid = GetMachineGuid();
            var s = string.Join("|", new[] { os, cpu, guid });
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            return Convert.ToHexString(hash);
        }
        catch
        {
            using var sha = SHA256.Create();
            var fallbackGuid = GetMachineGuid();
            var s = string.Join("|", new[] { Environment.OSVersion.VersionString, Environment.ProcessorCount.ToString(), fallbackGuid });
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            return Convert.ToHexString(hash);
        }
    }

    public static string GetLocalIp()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var a in host.AddressList)
            {
                if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return a.ToString();
                }
            }
            return "";
        }
        catch
        {
            return "";
        }
    }

    private static string GetMachineGuid()
    {
        try
        {
            using var lm64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var crypt64 = lm64.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography", false);
            var g64 = crypt64?.GetValue("MachineGuid") as string;
            if (!string.IsNullOrWhiteSpace(g64)) return g64!;

            using var lm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            using var crypt = lm.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography", false);
            var g = crypt?.GetValue("MachineGuid") as string;
            if (!string.IsNullOrWhiteSpace(g)) return g!;
            return "";
        }
        catch
        {
            return "";
        }
    }
}
