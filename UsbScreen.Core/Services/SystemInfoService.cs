using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace UsbScreen.Core.Services
{
    public class SystemInfoService
    {
        private static SystemInfoService? _instance;
        public static SystemInfoService Instance => _instance ??= new SystemInfoService();

        private readonly PerformanceCounter? _cpuCounter;

        private SystemInfoService()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to initialize CPU counter: {ex.Message}");
                }
            }
        }

        public string GetCpuUsage()
        {
            if (_cpuCounter != null)
            {
                try
                {
                    return $"{(int)_cpuCounter.NextValue()}%";
                }
                catch { return "0%"; }
            }
            return "N/A";
        }

        public string GetRamUsage()
        {
            try
            {
                var info = GetMemoryInfo();
                if (info.Total == 0) return "N/A";
                double usedGb = (info.Total - info.Available) / (1024.0 * 1024.0 * 1024.0);
                double totalGb = info.Total / (1024.0 * 1024.0 * 1024.0);
                return $"{usedGb:F1}/{totalGb:F1} GB";
            }
            catch { return "N/A"; }
        }

        public string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
            catch { return "N/A"; }
        }

        public string GetCurrentTime() => DateTime.Now.ToString("HH:mm:ss");
        public string GetCurrentDate() => DateTime.Now.ToString("yyyy-MM-dd");

        private (long Total, long Available) GetMemoryInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    return ((long)memStatus.ullTotalPhys, (long)memStatus.ullAvailPhys);
                }
            }
            return (0, 0);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
    }
}
