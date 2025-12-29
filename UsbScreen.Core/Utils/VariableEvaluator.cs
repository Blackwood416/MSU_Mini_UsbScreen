using System;
using System.Text.RegularExpressions;
using UsbScreen.Core.Services;

namespace UsbScreen.Core.Utils
{
    public static class VariableEvaluator
    {
        private static readonly Regex VariableRegex = new Regex(@"%([A-Z_]+)%", RegexOptions.Compiled);

        public static string Evaluate(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            return VariableRegex.Replace(input, match =>
            {
                string variableName = match.Groups[1].Value;
                return variableName switch
                {
                    "CPU" => SystemInfoService.Instance.GetCpuUsage(),
                    "RAM" => SystemInfoService.Instance.GetRamUsage(),
                    "TIME" => SystemInfoService.Instance.GetCurrentTime(),
                    "DATE" => SystemInfoService.Instance.GetCurrentDate(),
                    "INET" => SystemInfoService.Instance.GetLocalIPAddress(),
                    _ => match.Value // Keep the original marker if unknown
                };
            });
        }
    }
}
