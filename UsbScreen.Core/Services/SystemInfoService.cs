using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace UsbScreen.Core.Services
{
    public class SystemInfoService
    {
        private static SystemInfoService? _instance;
        public static SystemInfoService Instance => _instance ??= new SystemInfoService();

        private readonly PerformanceCounter? _cpuCounter;
        
        // For Linux CPU calculation
        private long _lastTotalTime;
        private long _lastIdleTime;

        private SystemInfoService()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    _cpuCounter.NextValue(); // First call usually returns 0
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to initialize CPU counter: {ex.Message}");
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    UpdateLinuxCpuUsage(); // Initial reading
                }
                catch { }
            }
        }

        public string GetCpuUsage()
        {
            if (OperatingSystem.IsWindows() && _cpuCounter != null)
            {
                try
                {
                    return $"{(int)_cpuCounter.NextValue()}%";
                }
                catch { return "0%"; }
            }
            else if (OperatingSystem.IsLinux())
            {
                return UpdateLinuxCpuUsage();
            }
            
            return "N/A";
        }

        private string UpdateLinuxCpuUsage()
        {
            try
            {
                // Read /proc/stat
                if (File.Exists("/proc/stat"))
                {
                    var lines = File.ReadAllLines("/proc/stat");
                    if (lines.Length > 0 && lines[0].StartsWith("cpu "))
                    {
                        var parts = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        // cpu  user nice system idle iowait irq softirq steal guest guest_nice
                        if (parts.Length >= 5)
                        {
                            long user = long.Parse(parts[1]);
                            long nice = long.Parse(parts[2]);
                            long system = long.Parse(parts[3]);
                            long idle = long.Parse(parts[4]);
                            long iowait = parts.Length > 5 ? long.Parse(parts[5]) : 0;
                            long irq = parts.Length > 6 ? long.Parse(parts[6]) : 0;
                            long softirq = parts.Length > 7 ? long.Parse(parts[7]) : 0;
                            long steal = parts.Length > 8 ? long.Parse(parts[8]) : 0;

                            long total = user + nice + system + idle + iowait + irq + softirq + steal;
                            
                            long deltaTotal = total - _lastTotalTime;
                            long deltaIdle = idle - _lastIdleTime;

                            _lastTotalTime = total;
                            _lastIdleTime = idle;

                            if (deltaTotal == 0) return "0%";

                            double usage = 1.0 - (double)deltaIdle / deltaTotal;
                            return $"{(int)(usage * 100)}%";
                        }
                    }
                }
            }
            catch { }
            return "0%";
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
                // Better cross-platform way to get the main IP address
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up && 
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        var props = ni.GetIPProperties();
                        var ip = props.UnicastAddresses
                            .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                        
                        if (ip != null)
                            return ip.Address.ToString();
                    }
                }
                
                // Fallback
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
            catch { return "127.0.0.1"; }
        }

        public string GetCurrentTime() => DateTime.Now.ToString("HH:mm:ss");
        public string GetCurrentDate() => DateTime.Now.ToString("yyyy-MM-dd");

        private (long Total, long Available) GetMemoryInfo()
        {
            if (OperatingSystem.IsWindows())
            {
                return GetWindowsMemoryInfo();
            }
            else if (OperatingSystem.IsLinux())
            {
                return GetLinuxMemoryInfo();
            }
            return (0, 0);
        }

        [SupportedOSPlatform("windows")]
        private (long Total, long Available) GetWindowsMemoryInfo()
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                return ((long)memStatus.ullTotalPhys, (long)memStatus.ullAvailPhys);
            }
            return (0, 0);
        }

        private (long Total, long Available) GetLinuxMemoryInfo()
        {
            try
            {
                if (File.Exists("/proc/meminfo"))
                {
                    var lines = File.ReadAllLines("/proc/meminfo");
                    long total = 0;
                    long available = 0;
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("MemTotal:"))
                            total = ParseKb(line);
                        else if (line.StartsWith("MemAvailable:"))
                            available = ParseKb(line);
                    }
                    return (total * 1024, available * 1024);
                }
            }
            catch { }
            return (0, 0);
        }

        private long ParseKb(string line)
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && long.TryParse(parts[1], out long val))
                return val;
            return 0;
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
        [SupportedOSPlatform("windows")]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
    }
}
